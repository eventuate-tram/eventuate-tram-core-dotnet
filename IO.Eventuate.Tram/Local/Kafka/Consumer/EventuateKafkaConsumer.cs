/*
 * Ported from:
 * repo:	https://github.com/eventuate-local/eventuate-local
 * module:	eventuate-local-java-kafka
 * package:	io.eventuate.local.java.kafka.consumer
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer
{
	/// <summary>
	/// Kafka consumer listens for a set of topics and triggers a callback when
	/// an event is received.
	/// Disposing of the the consumer shuts down the subscription.
	/// </summary>
	public class EventuateKafkaConsumer : IAsyncDisposable
	{
		private const int AdminClientTimeoutMilliseconds = 10;

		private readonly string _subscriberId;
		private readonly EventuateKafkaConsumerMessageHandler _handler;
		private readonly IList<string> _topics;
		private readonly BackPressureConfig _backPressureConfig;
		private readonly long _pollTimeout;
		private readonly ILoggerFactory _loggerFactory;

		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private readonly IDictionary<string, string> _consumerProperties;
		private readonly ILogger _logger;

		private volatile EventuateKafkaConsumerState _state = EventuateKafkaConsumerState.Created;

		private static readonly ISet<TopicPartition> NoTopicPartitions = new HashSet<TopicPartition>();

		// Back pressure state. Only used on the consume loop's thread: Consume() calls the rebalance handlers there.
		private readonly BackPressureManager _backPressureManager;
		private readonly Dictionary<string, BackPressureManager> _backPressureManagersByTopic = new();
		private readonly Dictionary<string, int> _backlogsByTopic = new();
		private readonly HashSet<TopicPartition> _assignedPartitions = new();
		private readonly Dictionary<string, HashSet<TopicPartition>> _assignedPartitionsByTopic = new();
		private bool _assignmentChanged;

		private Task _consumeTask;

		public EventuateKafkaConsumer(string subscriberId,
			EventuateKafkaConsumerMessageHandler handler,
			IList<string> topics,
			string bootstrapServers,
			EventuateKafkaConsumerConfigurationProperties eventuateKafkaConsumerConfigurationProperties,
			ILoggerFactory loggerFactory)
		{
			_subscriberId = subscriberId;
			_handler = handler;
			_topics = topics;
			_backPressureConfig = eventuateKafkaConsumerConfigurationProperties.BackPressure;
			_pollTimeout = eventuateKafkaConsumerConfigurationProperties.PollTimeout;
			_loggerFactory = loggerFactory;
			_logger = loggerFactory.CreateLogger<EventuateKafkaConsumer>();
			_backPressureManager = new BackPressureManager(_backPressureConfig);

			_consumerProperties =
				ConsumerPropertiesFactory.MakeDefaultConsumerProperties(bootstrapServers, subscriberId)
					.ToDictionary(p => p.Key, p => p.Value);

			foreach (KeyValuePair<string,string> pair in eventuateKafkaConsumerConfigurationProperties.Properties)
			{
				_consumerProperties[pair.Key] = pair.Value;
			}
		}
		
		private void VerifyTopicExistsBeforeSubscribing(IAdminClient adminClient, string topic)
		{
			var logContext = $"{nameof(VerifyTopicExistsBeforeSubscribing)} " +
			                 $"for subscriberId='{_subscriberId}', topic='{topic}'";
			try
			{
				_logger.LogDebug($"+{logContext}");
				Metadata metadata = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(AdminClientTimeoutMilliseconds));

				List<PartitionMetadata> partitions = metadata.Topics[0].Partitions;
				_logger.LogDebug($"-{logContext}: found partitions='{String.Join(",", partitions.Select(p => p.PartitionId))}'");
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"{logContext}: Got exception: {e}");
				throw;
			}
		}

		private void NotePartitionsAssigned(IEnumerable<TopicPartition> partitions)
		{
			foreach (TopicPartition partition in partitions)
			{
				_assignedPartitions.Add(partition);
				if (!_assignedPartitionsByTopic.TryGetValue(partition.Topic, out HashSet<TopicPartition> topicPartitions))
				{
					topicPartitions = [];
					_assignedPartitionsByTopic[partition.Topic] = topicPartitions;
				}

				topicPartitions.Add(partition);
			}

			_assignmentChanged = true;
		}

		private void NotePartitionsRevoked(IEnumerable<TopicPartition> partitions)
		{
			List<TopicPartition> revoked = partitions.ToList();
			_assignedPartitions.ExceptWith(revoked);
			_backPressureManager.RemovePartitions(revoked);
			foreach (IGrouping<string, TopicPartition> topicPartitions in revoked.GroupBy(p => p.Topic))
			{
				if (_assignedPartitionsByTopic.TryGetValue(topicPartitions.Key, out HashSet<TopicPartition> assigned))
				{
					assigned.ExceptWith(topicPartitions);
				}

				if (_backPressureManagersByTopic.TryGetValue(topicPartitions.Key, out BackPressureManager topicBackPressureManager))
				{
					topicBackPressureManager.RemovePartitions(topicPartitions);
				}
			}

			_assignmentChanged = true;
		}

		private void ApplyBackPressure(IConsumer<string, string> consumer, KafkaMessageProcessor processor, string logContext)
		{
			// The managers are given the assigned partitions only when they change, so a partition assigned while
			// its manager is paused gets paused too.
			bool assignmentChanged = _assignmentChanged;
			_assignmentChanged = false;

			if (_backPressureConfig.Scope == BackPressureScope.Topic)
			{
				processor.GetBacklogByTopic(_backlogsByTopic);
				foreach ((string topic, int backlog) in _backlogsByTopic)
				{
					bool newTopic = !_backPressureManagersByTopic.TryGetValue(topic, out BackPressureManager topicBackPressureManager);
					if (newTopic)
					{
						topicBackPressureManager = new BackPressureManager(_backPressureConfig);
						_backPressureManagersByTopic[topic] = topicBackPressureManager;
					}

					ISet<TopicPartition> partitions = (assignmentChanged || newTopic) &&
					                                  _assignedPartitionsByTopic.TryGetValue(topic, out HashSet<TopicPartition> assigned)
						? assigned
						: NoTopicPartitions;
					ApplyBackPressure(consumer, topicBackPressureManager, partitions, backlog, $"topic {topic}'s backlog", logContext);
				}
			}
			else
			{
				ApplyBackPressure(consumer, _backPressureManager, assignmentChanged ? _assignedPartitions : NoTopicPartitions,
					processor.GetBacklog(), "backlog", logContext);
			}
		}

		private void ApplyBackPressure(IConsumer<string, string> consumer, BackPressureManager backPressureManager,
			ISet<TopicPartition> assignedPartitions, int backlog, string backlogDescription, string logContext)
		{
			BackPressureActions actions = backPressureManager.Update(assignedPartitions, backlog);

			if (actions.PartitionsToPause.Any())
			{
				_logger.LogInformation($"{logContext}: subscriber {_subscriberId} pausing " +
				                       $"{string.Join(", ", actions.PartitionsToPause)} due to {backlogDescription} {backlog} > {_backPressureConfig.PauseThreshold}");
				consumer.Pause(actions.PartitionsToPause);
			}

			if (actions.PartitionsToResume.Any())
			{
				_logger.LogInformation($"{logContext}: subscriber {_subscriberId} resuming " +
				                       $"{string.Join(", ", actions.PartitionsToResume)} due to {backlogDescription} {backlog} <= {_backPressureConfig.ResumeThreshold}");
				consumer.Resume(actions.PartitionsToResume);
			}
		}

		private void MaybeCommitOffsets(IConsumer<string, string> consumer, KafkaMessageProcessor processor)
		{
			var logContext = $"{nameof(MaybeCommitOffsets)} for SubscriberId='{_subscriberId}'";
			List<TopicPartitionOffset> offsetsToCommit = processor.OffsetsToCommit().ToList();
			if (offsetsToCommit.Any())
			{
				_logger.LogDebug($"{logContext}: Committing offsets='{String.Join(",", offsetsToCommit)}'");
				consumer.Commit(offsetsToCommit);
				processor.NoteOffsetsCommitted(offsetsToCommit);
				_logger.LogDebug($"-{logContext}");
			}
		}

		public void Start()
		{
			var logContext = $"{nameof(Start)} for SubscriberId={_subscriberId}";
			IConsumer<string, string> consumer = null;
			try
			{
				consumer = new ConsumerBuilder<string, string>(_consumerProperties)
					.SetPartitionsAssignedHandler((_, partitions) => NotePartitionsAssigned(partitions))
					.SetPartitionsRevokedHandler((_, partitions) => NotePartitionsRevoked(partitions.Select(p => p.TopicPartition)))
					.Build();
				var processor = new KafkaMessageProcessor(_subscriberId, _handler,
					_loggerFactory.CreateLogger<KafkaMessageProcessor>());

				using (IAdminClient adminClient = new DependentAdminClientBuilder(consumer.Handle).Build())
				{
					foreach (string topic in _topics)
					{
						VerifyTopicExistsBeforeSubscribing(adminClient, topic);
					}
				}

				List<string> topicsList = new List<string>(_topics);
				_logger.LogInformation($"{logContext}: Subscribing to topics='{String.Join(",", topicsList)}'");

				consumer.Subscribe(topicsList);

				// Set state to started before starting the processing thread instead of after as in the Java code
				// (prevent setting it to it started after it has potentially already been set to stopped)
				_state = EventuateKafkaConsumerState.Started;

				// ReSharper disable AccessToDisposedClosure - consumer is disposed outside of the closure only if the closure doesn't execute
				// No cancellation token: if the consumer was disposed before the task started, the loop wouldn't run,
				// so the consumer would never be closed. The loop checks for cancellation itself.
				_consumeTask = Task.Run(async () =>
				{
					try
					{
						while (!_cancellationTokenSource.IsCancellationRequested)
						{
							try
							{
								ConsumeResult<string, string> record =
									consumer.Consume(TimeSpan.FromMilliseconds(_pollTimeout));

								if (record != null)
								{
									_logger.LogDebug($"{logContext}: process record at offset='{record.Offset}', " +
									                 $"key='{record.Message.Key}', value='{record.Message.Value}'");
									await processor.ProcessAsync(record);
								}
								else
								{
									processor.ThrowExceptionIfHandlerFailed();
								}

								MaybeCommitOffsets(consumer, processor);

								ApplyBackPressure(consumer, processor, logContext);
							}
							catch (ConsumeException e)
							{
								_logger.LogError(e, $"{logContext}: ConsumeException - {e.Error}. Continuing.");
							}
							catch (KafkaException e)
							{
								_logger.LogError(e, $"{logContext}: KafkaException - {e.Error}. Continuing.");
							}
						}

						_state = EventuateKafkaConsumerState.Stopped;
					}
					catch (TaskCanceledException)
					{
						_logger.LogInformation($"{logContext}: Shutdown by cancel");
						_state = EventuateKafkaConsumerState.Stopped;
					}
					catch (KafkaMessageProcessorFailedException e)
					{
						if (e.InnerException is OperationCanceledException)
						{
							_logger.LogDebug($"{logContext}: Terminating due to OperationCanceledException during message handling - {e}");
							_state = EventuateKafkaConsumerState.Stopped;
						}
						else
						{
							_logger.LogError($"{logContext}: Terminating due to KafkaMessageProcessorFailedException - {e}");
							_state = EventuateKafkaConsumerState.MessageHandlingFailed;
						}
					}
					catch (Exception e)
					{
						_logger.LogError($"{logContext}: Exception - {e}");
						_state = EventuateKafkaConsumerState.Failed;
						// Java throws here, but seems like it isn't necessary
					}
					finally
					{
						// Try to put the last of the offsets away. Note that the 
						// callbacks are done asynchronously so there is no guarantee
						// that all the offsets are ready. Worst case is that there
						// are messages processed more than once.
						try
						{
							MaybeCommitOffsets(consumer, processor);
						}
						catch (KafkaException e)
						{
							_logger.LogError(e, $"{logContext}: Error committing last offsets");
						}
						
						consumer.Close();
						consumer.Dispose();
						
						_logger.LogDebug($"{logContext}: Stopped in state {_state.ToString()}");
					}
				});
				// ReSharper restore AccessToDisposedClosure
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"{logContext}: Error subscribing");
				_state = EventuateKafkaConsumerState.FailedToStart;
				consumer?.Close();
				consumer?.Dispose();
				throw;
			}
		}
		
		// Following recommended standard implementation of DisposeAsync for unsealed classes
		public async ValueTask DisposeAsync()
		{
			await DisposeAsyncCore();
			GC.SuppressFinalize(this);
		}
		
		protected virtual async ValueTask DisposeAsyncCore()
		{
			var logContext = $"{nameof(DisposeAsyncCore)} for SubscriberId={_subscriberId}";
			_logger.LogDebug($"+{logContext}");
			if (!_cancellationTokenSource.IsCancellationRequested)
			{
				_logger.LogDebug($"+{logContext}: Sending cancel to consumer thread.");
				_cancellationTokenSource.Cancel();
			}

			if (_consumeTask != null)
			{
				await _consumeTask;
			}

			_cancellationTokenSource.Dispose();
			_logger.LogDebug($"-{logContext}");
		}
	}
}