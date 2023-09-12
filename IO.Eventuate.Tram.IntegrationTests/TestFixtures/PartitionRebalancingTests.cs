using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using IO.Eventuate.Tram.Database;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Consumer;
using IO.Eventuate.Tram.Messaging.Producer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestFixtures;

[TestFixture(null)]
[TestFixture("cooperative-sticky")]
public class PartitionRebalancingTests : IntegrationTestsBase
{
	private readonly string _partitionAssignmentStrategyOverride;
	private const string DbSchemaName = "eventuate";
	private ServiceProvider _rootServiceProvider;
	private AsyncServiceScope _serviceScope;
	private IMessageConsumer _consumer;
	private IMessageProducer _producer;

	public PartitionRebalancingTests(string partitionAssignmentStrategyOverride)
	{
		_partitionAssignmentStrategyOverride = partitionAssignmentStrategyOverride;
	}

	[SetUp]
	public async Task SetUp()
	{
		_rootServiceProvider = CreateTestServiceProvider(_partitionAssignmentStrategyOverride);
		_serviceScope = _rootServiceProvider.CreateAsyncScope();

		IServiceProvider serviceProvider = _serviceScope.ServiceProvider;
		
		_consumer = serviceProvider.GetRequiredService<IMessageConsumer>();
		_producer = serviceProvider.GetRequiredService<IMessageProducer>();

		DbContextOptionsBuilder<EventuateTramDbContext> builder = new();
		builder.UseSqlServer(TestSettings.ConnectionStrings.EventuateTramDbConnection);
		await using var dbContext = new EventuateTramDbContext(builder.Options, new EventuateSchema(DbSchemaName));
		await ClearDbAsync(dbContext, DbSchemaName);
		await CleanupKafkaTopics();
	}

	[TearDown]
	public async Task TearDown()
	{
		await _consumer.CloseAsync();
		await _serviceScope.DisposeAsync();
		await _rootServiceProvider.DisposeAsync();
	}

	[Test]
	public async Task PartitionsRebalanced_MessagesInSwimlaneDispatcherQueues_MessagesConsumedOnceEachAndInOrder()
	{
		// Arrange
		var topics = new HashSet<string>
		{
			TestPartitionAssignmentTopic1,
			TestPartitionAssignmentTopic2
		};

		const string partitionId1 = "1";
		const string partitionId2 = "2";
		var partitionIds = new List<string>
		{
			partitionId1,
			partitionId2
		};
		
		var sentMessagesByTopicPartition = new Dictionary<(string Topic, string PartitionId), List<IMessage>>();
		var consumerMessages =
			new Dictionary<(string Topic, string PartitionId),
				ConcurrentQueue<(IMessage Message, string ConsumerName, DateTimeOffset StartTime, DateTimeOffset EndTime)>>();

		// Publish messages that will be written to 2 different partitions on each topic
		foreach (string topic in topics)
		{
			foreach (string partitionId in partitionIds)
			{
				for (var messageNumber = 0; messageNumber < 10; messageNumber++)
				{
					var payload = $"topic:{topic}-partitionId:{partitionId}-message:{messageNumber}";
					IMessage message = MessageBuilder
						.WithPayload(payload)
						.WithHeader(MessageHeaders.PartitionId, partitionId)
						.Build();
					
					if (!sentMessagesByTopicPartition.TryGetValue((topic, partitionId), out List<IMessage> messages))
					{
						messages = new List<IMessage>();
						sentMessagesByTopicPartition.Add((topic, partitionId), messages);
					}
					messages.Add(message);
					
					await _producer.SendAsync(topic, message);
				}

				consumerMessages.Add((topic, partitionId),
					new ConcurrentQueue<(IMessage Message, string consumerName, DateTimeOffset StartTime, DateTimeOffset EndTime)>());
			}
		}

		var consumerGroupId = Guid.NewGuid().ToString();

		var waitToConsume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		// Start a consumer where the message handler is waiting to allow messages to build up in the swimlane queues
		_consumer.Subscribe(consumerGroupId, topics,
			(m, _, ct) => HandleMessageAsync(waitToConsume.Task, m, "consumer1", consumerMessages, ct));

		await WaitForStableConsumerGroupAsync(consumerGroupId);

		// Wait for SwimlaneDispatcher message queues to populate
		await Task.Delay(TimeSpan.FromSeconds(10));
		
		// Act - Start a second consumer in the same consumer group to trigger a rebalance
		_consumer.Subscribe(consumerGroupId, new HashSet<string> { TestPartitionAssignmentTopic1 },
			(m, _, ct) => HandleMessageAsync(waitToConsume.Task, m, "consumer2", consumerMessages, ct));

		await WaitForStableConsumerGroupAsync(consumerGroupId);

		// Wait for SwimlaneDispatcher message queues to populate
		await Task.Delay(TimeSpan.FromSeconds(10));

		// Allow processing to proceed
		waitToConsume.SetResult();

		// Assert
		// Wait for messages to be processed
		await Task.Delay(TimeSpan.FromSeconds(10));
		
		foreach (string topic in topics)
		{
			foreach (string partitionId in partitionIds)
			{
				var context = $"topic={topic} partitionId={partitionId}";
				
				List<(IMessage Message, string ConsumerName, DateTimeOffset StartTime, DateTimeOffset EndTime)> messages =
					consumerMessages[(topic, partitionId)].ToList();
				
				List<IMessage> expectedMessages = sentMessagesByTopicPartition[(topic, partitionId)];
				// Check that message count is correct
				Assert.That(messages, Has.Count.EqualTo(expectedMessages.Count), context);
				
				// Check that messages were processed in the order they were published with no overlap in time
				Assert.Multiple(() =>
				{
					for (var i = 0; i < expectedMessages.Count; i++)
					{
						IMessage expectedMessage = expectedMessages[i];
						IMessage actualMessage = messages[i].Message;
						Assert.That(actualMessage.Id, Is.EqualTo(expectedMessage.Id), $"{context} index={i} Id");
						Assert.That(actualMessage.Payload, Is.EqualTo(expectedMessage.Payload), $"{context} index={i} Payload");
						if (i > 0)
						{
							Assert.That(messages[i].StartTime, Is.GreaterThan(messages[i - 1].EndTime),
								$"{context} index={i} start time less than previous end time");
						}
					}
				});
			}
		}
	}

	private static async Task HandleMessageAsync(Task waitBeforeConsumingTask, IMessage message, string consumerName,
		IDictionary<(string Topic, string PartitionId),
			ConcurrentQueue<(IMessage Message, string ConsumerName, DateTimeOffset StartTime, DateTimeOffset EndTime)>> consumerMessages,
		CancellationToken cancellationToken)
	{
		await waitBeforeConsumingTask;

		cancellationToken.ThrowIfCancellationRequested();

		DateTimeOffset startTime = DateTimeOffset.Now;
		string topic = message.GetRequiredHeader(MessageHeaders.Destination);
		string partitionId = message.GetRequiredHeader(MessageHeaders.PartitionId);
		ConcurrentQueue<(IMessage Message, string ConsumerName, DateTimeOffset StartTime, DateTimeOffset EndTime)> messages =
			consumerMessages[(topic, partitionId)];
		await Task.Delay(50, cancellationToken);
		DateTimeOffset endTime = DateTimeOffset.Now;

		messages.Enqueue((message, consumerName, startTime, endTime));
	}

	private ServiceProvider CreateTestServiceProvider(string partitionAssignmentStrategy = null)
	{
		IServiceCollection services = new ServiceCollection();
		services.AddLogging(b =>
			b.AddSimpleConsole(o =>
				{
					o.ColorBehavior = LoggerColorBehavior.Disabled;
					o.TimestampFormat = "HH:mm:ss ";
				})
				.AddFilter("Default", LogLevel.Information)
				.AddFilter("IO.Eventuate", LogLevel.Debug));
		services.AddOptions();

		EventuateKafkaConsumerConfigurationProperties consumerProperties = EventuateKafkaConsumerConfigurationProperties.Empty();
		if (partitionAssignmentStrategy != null)
		{
			consumerProperties.Properties["partition.assignment.strategy"] = partitionAssignmentStrategy;
		}

		services.AddEventuateTramSqlKafkaTransport("eventuate", TestSettings.KafkaBootstrapServers,
			consumerProperties, (_, options) =>
			{
				options.UseSqlServer(TestSettings.ConnectionStrings.EventuateTramDbConnection);
			});
		
		return services.BuildServiceProvider(new ServiceProviderOptions
			{ ValidateScopes = true, ValidateOnBuild = true });
	}

	private async Task WaitForStableConsumerGroupAsync(string consumerGroupId)
	{
		TimeSpan stableConsumerTimeout = TimeSpan.FromSeconds(30);

		var kafkaAdminConfig = new AdminClientConfig { BootstrapServers = TestSettings.KafkaBootstrapServers };
		using IAdminClient adminClient = new AdminClientBuilder(kafkaAdminConfig).Build();

		var stopwatch = new Stopwatch();
		stopwatch.Start();
		var isConsumerGroupStable = false;
		while (!isConsumerGroupStable)
		{
			DescribeConsumerGroupsResult result =
				await adminClient.DescribeConsumerGroupsAsync(new[] { consumerGroupId });
			if (result.ConsumerGroupDescriptions.Any() &&
			    result.ConsumerGroupDescriptions[0].State == ConsumerGroupState.Stable)
			{
				isConsumerGroupStable = true;
				stopwatch.Stop();
			}
			else
			{
				if (stopwatch.Elapsed > stableConsumerTimeout)
				{
					throw new TimeoutException(
						$"Timed out waiting for consumer group {consumerGroupId} to become stable after {stopwatch.Elapsed}.");
				}
				await Task.Delay(100);
			}
		}
	}
}