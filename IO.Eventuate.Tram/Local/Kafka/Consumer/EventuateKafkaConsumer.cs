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
	public class EventuateKafkaConsumer : IDisposable
	{
		private const int ConsumePollMilliseconds = 100;
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
		public EventuateKafkaConsumerState State => _state;

		private Task _consumeTask = null;

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
			_pollTimeout = eventuateKafkaConsumerConfigurationProperties.Timeout;
			_loggerFactory = loggerFactory;
			_logger = loggerFactory.CreateLogger<EventuateKafkaConsumer>();

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
			try
			{
				IConsumer<string, string> consumer = new ConsumerBuilder<string, string>(_consumerProperties).Build();
				var processor = new KafkaMessageProcessor(_subscriberId, _handler,
					_loggerFactory.CreateLogger<KafkaMessageProcessor>());
				var backPressureManager = new BackPressureManager(_backPressureConfig);

				using (IAdminClient adminClient = new DependentAdminClientBuilder(consumer.Handle).Build())
				{
					foreach (string topic in _topics)
					{
						VerifyTopicExistsBeforeSubscribing(adminClient, topic);
					}
				}

				List<string> topicsList = new List<string>(_topics);
				_logger.LogDebug($"{logContext}: Subscribing to topics='{String.Join(",", topicsList)}'");

				consumer.Subscribe(topicsList);

				// Set state to started before starting the processing thread instead of after as in the Java code
				// (prevent setting it to it started after it has potentially already been set to stopped)
				_state = EventuateKafkaConsumerState.Started;

				_consumeTask = Task.Run(() =>
				{
					try
					{
						while (!_cancellationTokenSource.IsCancellationRequested)
						{
							try
							{
								ConsumeResult<string, string> record =
									consumer.Consume(TimeSpan.FromMilliseconds(ConsumePollMilliseconds));

								if (record != null)
								{
									_logger.LogDebug(
										$"{logContext}: process record at offset='{record.Offset}', key='{record.Message.Key}', value='{record.Message.Value}'");

									processor.Process(record);
								}
								else
								{
									processor.ThrowExceptionIfHandlerFailed();
								}

								MaybeCommitOffsets(consumer, processor);

								int backlog = processor.GetBacklog();
								BackPressureActions actions = backPressureManager.Update(record, backlog);

								if (actions.Pause.Any())
								{
									_logger.LogInformation(
										$"{logContext}: subscriber {_subscriberId} pausing due to backlog {backlog} > {_backPressureConfig.PauseThreshold}");
									consumer.Pause(actions.Pause);
								}
								if (actions.Resume.Any())
								{
									_logger.LogInformation(
										$"{logContext}: subscriber {_subscriberId} resuming due to backlog {backlog} <= {_backPressureConfig.ResumeThreshold}");
									consumer.Resume(actions.Resume);
								}
							}
							catch (ConsumeException e)
							{
								_logger.LogError($"{logContext}: ConsumeException - {e.Error}. Continuing.");
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
						_logger.LogError($"{logContext}: Terminating due to KafkaMessageProcessorFailedException - {e}");
						_state = EventuateKafkaConsumerState.MessageHandlingFailed;
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
						MaybeCommitOffsets(consumer, processor);
						consumer.Close();
						consumer.Dispose();
						
						_logger.LogDebug($"{logContext}: Stopped in state {_state.ToString()}");
					}
				}, _cancellationTokenSource.Token);
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"{logContext}: Error subscribing");
				_state = EventuateKafkaConsumerState.FailedToStart;
				throw;
			}
		}

		public void Dispose()
		{
			var logContext = $"{nameof(Dispose)} for SubscriberId={_subscriberId}";
			_logger.LogDebug($"+{logContext}");
			if (!_cancellationTokenSource.IsCancellationRequested)
			{
				_logger.LogDebug($"+{logContext}: Sending cancel to consumer thread.");
				_cancellationTokenSource.Cancel();
			}

			_consumeTask?.Wait();

			_cancellationTokenSource.Dispose();
			_logger.LogDebug($"-{logContext}");
		}
	}
}