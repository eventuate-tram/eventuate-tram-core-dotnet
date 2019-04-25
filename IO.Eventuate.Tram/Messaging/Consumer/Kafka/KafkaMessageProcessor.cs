using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Messaging.Consumer.Kafka
{
	/// <summary>
	/// Processes a Kafka message and tracks the message offsets that have been successfully processed and can be committed
	/// </summary>
	public class KafkaMessageProcessor
	{
		private readonly ILogger _logger;
		private readonly string _loggingObjectContext;

		private readonly Action<ConsumeResult<string, string>, Action<Exception>> _handler;
		private readonly OffsetTracker _offsetTracker;

		// The java solution used a blocking queue but none of the methods that use
		// the blocking feature. ConcurrentQueue is simpler.
		private readonly ConcurrentQueue<ConsumeResult<string, string>> _processedRecords =
			new ConcurrentQueue<ConsumeResult<string, string>>();

		public KafkaMessageProcessor(string subscriberId,
			Action<ConsumeResult<string, string>, Action<Exception>> handler,
			ILogger<KafkaMessageProcessor> logger)
		{
			_handler = handler;
			_logger = logger;
			_loggingObjectContext = $"SubscriberId='{subscriberId}', handler='{handler.Method.Name}'";
			_offsetTracker = new OffsetTracker(_logger);
		}

		public void Process(ConsumeResult<string, string> record)
		{
			var logContext = $"{nameof(Process)} for {_loggingObjectContext}, " +
			                 $"record.Key='{record.Key}', record.Topic='{record.Topic}'";
			_logger.LogDebug($"+{logContext}");
			_offsetTracker.NoteUnprocessed(new TopicPartition(record.Topic, record.Partition), record.Offset);
			_handler(record, e =>
			{
				if (e != null)
				{
					_logger.LogError(e, $"{logContext}: Exception processing record");
				}
				else
				{
					_logger.LogDebug($"{logContext}: Adding process record to queue");
					_processedRecords.Enqueue(record);
				}
			});
			_logger.LogDebug($"-{logContext}");
		}

		public IEnumerable<TopicPartitionOffset> OffsetsToCommit()
		{
			var logContext = $"{nameof(OffsetsToCommit)} for {_loggingObjectContext}";
			_logger.LogDebug($"+{logContext}");
			int count = 0;
			while (true)
			{
				if (!_processedRecords.TryDequeue(out ConsumeResult<string, string> record))
				{
					break;
				}

				count++;
				_offsetTracker.NoteProcessed(new TopicPartition(record.Topic, record.Partition), record.Offset);
			}

			List<TopicPartitionOffset> offsetsToCommit =
				new List<TopicPartitionOffset>(_offsetTracker.OffsetsToCommit());
			_logger.LogDebug($"-{logContext}: Marked {count} records as processed, returning {offsetsToCommit.Count} offsets to commit");
			return offsetsToCommit;
		}

		public void NoteOffsetsCommitted(IEnumerable<TopicPartitionOffset> offsetsToCommit)
		{
			var logContext = $"{nameof(NoteOffsetsCommitted)} for {_loggingObjectContext}";
			_logger.LogDebug($"{logContext}");
			_offsetTracker.NoteOffsetsCommitted(offsetsToCommit);
		}

		public OffsetTracker GetPending()
		{
			var logContext = $"{nameof(GetPending)} for {_loggingObjectContext}";
			_logger.LogDebug($"{logContext}");
			return _offsetTracker;
		}
	}
}