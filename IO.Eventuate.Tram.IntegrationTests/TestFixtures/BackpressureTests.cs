using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.IntegrationTests.TestHelpers;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestFixtures;

public class BackpressureTests : IntegrationTestsBase
{
	private const uint PauseThreshold = 20;
	private const uint ResumeThreshold = 5;
	
	[SetUp]
	public async Task Setup()
	{
		await CleanupKafkaTopics();
		// Initialize the backpressure properties
		var properties = new EventuateKafkaConsumerConfigurationProperties
		{
			BackPressure =
			{
				PauseThreshold = PauseThreshold,
				ResumeThreshold = ResumeThreshold
			}
		};
		TestSetup("eventuate", false, properties);
		await CleanupTestAsync();
	}

	[TearDown]
	public void TearDown()
	{
		DisposeTestHost();
	}

	[Test]
	public void PublishWithBackpressure_Send1000Messages_AllMessagesEventuallyProcessed()
	{
		// Arrange
		uint messagesPerType = 250;
		uint totalMessages = messagesPerType * 4;
		TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
		TestMessageType2 msg2 = new TestMessageType2("Msg2", 2);
		TestMessageType3 msg3 = new TestMessageType3("Msg3", 3);
		TestMessageType4 msg4 = new TestMessageType4("Msg4", 4);

		for (int x = 0; x < messagesPerType; x++)
		{
			GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> { msg1 });
			GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> { msg2 });
			GetTestPublisher().Publish(AggregateType34, AggregateType34, new List<IDomainEvent> { msg3 });
			GetTestPublisher().Publish(AggregateType34, AggregateType34, new List<IDomainEvent> { msg4 });
		}

		// Act
		TestEventConsumer consumer = GetTestConsumer();

		// Allow time for messages to process
		int count = 300;
		while (consumer.TotalMessageCount() < totalMessages && count > 0)
		{
			TestContext.WriteLine($"TotalMessageCount: {consumer.TotalMessageCount()} ({count})");
			Thread.Sleep(1000);
			count--;
		}
		ShowTestResults();

		// Assert
		Assert.That(GetDbContext().Messages.Count(),
			Is.EqualTo(totalMessages), "Number of messages produced");
		Assert.That(GetDbContext().Messages.Count(msg => msg.Published == 0),
			Is.EqualTo(0), "Number of unpublished messages");
		foreach (var eventType in new[] { typeof(TestMessageType1), typeof(TestMessageType2), typeof(TestMessageType3), typeof(TestMessageType4) })
		{
			TestEventConsumer.EventStatistics eventStatistics = consumer.GetEventStatistics(eventType);
			Assert.That(eventStatistics.MessageCount,
				Is.EqualTo(messagesPerType), $"Number of {eventType.Name} messages received by consumer");
			Assert.That(eventStatistics.ReceivedMessages.Count,
				Is.EqualTo(messagesPerType), $"Number of received {eventType.Name} messages");
		}

		Assert.That(consumer.TotalMessageCount(),
			Is.EqualTo(totalMessages), "Total number of messages received by consumer");
		Assert.That(GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null),
			Is.EqualTo(totalMessages), "Number of received messages");
	}
}