using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Messaging.Consumer.Kafka
{
	/// <summary>
	/// Keeps track of message offsets that are (a) being processed and (b) have been processed and can be committed
	/// </summary>
	public class OffsetTracker
	{
		private readonly ILogger _logger;
		private readonly IDictionary<TopicPartition, TopicPartitionOffsets> _state =
			new Dictionary<TopicPartition, TopicPartitionOffsets>();

		public OffsetTracker(ILogger logger)
		{
			_logger = logger;
		}

		public override string ToString()
		{
			// Output the collection of topics and offsets
			StringBuilder stringBuilder = new StringBuilder($"{nameof(_state)}: ");
			foreach (KeyValuePair<TopicPartition, TopicPartitionOffsets> topicOffsetPair in _state)
			{
				stringBuilder.Append($"{topicOffsetPair.Key}={topicOffsetPair.Value},");
			}

			if (_state.Count > 0)
			{
				// Remove the last comma
				stringBuilder.Remove(stringBuilder.Length - 1, 1);
			}

			return stringBuilder.ToString();
		}
		
		TopicPartitionOffsets Fetch(TopicPartition topicPartition)
		{
			var logContext = $"OffsetTracker.Fetch for topicPartition='{topicPartition}'";
			_logger.LogDebug($"+{logContext}");
			_state.TryGetValue(topicPartition, out TopicPartitionOffsets tpo);
			if (tpo == null)
			{
				_logger.LogDebug($"{logContext}: Creating new topic partition offset");
				tpo = new TopicPartitionOffsets(_logger);
				_state[topicPartition] = tpo;
			}

			_logger.LogDebug($"-{logContext}");
			return tpo;
		}

		public void NoteUnprocessed(TopicPartition topicPartition, long offset)
		{
			var logContext = $"OffsetTracker.NoteUnprocessed for topicPartition='{topicPartition}', offset={offset}";
			_logger.LogDebug($"{logContext}");
			Fetch(topicPartition).NoteUnprocessed(offset);
		}

		public void NoteProcessed(TopicPartition topicPartition, long offset)
		{
			var logContext = $"OffsetTracker.NoteProcessed for topicPartition='{topicPartition}', offset={offset}";
			_logger.LogDebug($"{logContext}");
			Fetch(topicPartition).NoteProcessed(offset);
		}

		public IEnumerable<TopicPartitionOffset> OffsetsToCommit()
		{
			var logContext = "OffsetTracker.OffsetsToCommit";
			_logger.LogDebug($"+{logContext}");
			var result = new List<TopicPartitionOffset>();
			foreach (KeyValuePair<TopicPartition, TopicPartitionOffsets> pair in _state)
			{
				long? offset = pair.Value.OffsetToCommit();
				if (offset.HasValue)
				{
					result.Add(new TopicPartitionOffset(pair.Key, new Offset(offset.Value + 1)));
				}
			}

			_logger.LogDebug($"-{logContext}: collected {result.Count} offsets to commit");
			return result;
		}

		public void NoteOffsetsCommitted(IEnumerable<TopicPartitionOffset> offsetsToCommit)
		{
			List<TopicPartitionOffset> offsets = new List<TopicPartitionOffset>(offsetsToCommit);
			var logContext = $"OffsetTracker.NoteOffsetsCommitted with {offsets.Count} offsets";
			_logger.LogDebug($"+{logContext}");
			foreach (TopicPartitionOffset topicPartitionOffset in offsets)
			{
				Fetch(topicPartitionOffset.TopicPartition).NoteOffsetCommitted(topicPartitionOffset.Offset);
			}
			_logger.LogDebug($"-{logContext}");
		}

		public IDictionary<TopicPartition, ISet<long>> GetPending()
		{
			var logContext = "OffsetTracker.GetPending";
			_logger.LogDebug($"+{logContext}");
			IDictionary<TopicPartition, ISet<long>> result = new Dictionary<TopicPartition, ISet<long>>();
			foreach (KeyValuePair<TopicPartition, TopicPartitionOffsets> pair in _state)
			{
				ISet<long> pending = pair.Value.GetPending();
				if (pending.Any())
				{
					result[pair.Key] = pending;
				}
			}

			_logger.LogDebug($"-{logContext}: found {result.Count} pending offsets");
			return result;
		}
	}
}