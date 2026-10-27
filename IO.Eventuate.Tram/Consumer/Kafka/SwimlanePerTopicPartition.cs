/*
 * Ported from:
 * repo:	https://github.com/eventuate-foundation/eventuate-messaging-kafka
 * module:	eventuate-messaging-kafka-consumer
 * package:	io.eventuate.messaging.kafka.consumer
 */

using System.Collections.Concurrent;
using System.Threading;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Consumer.Kafka
{
	/// <summary>
	/// One swimlane per topic partition, so a subscriber's messages from different topics are handled concurrently,
	/// and one topic's messages never wait behind another's.
	/// </summary>
	public class SwimlanePerTopicPartition : ITopicPartitionToSwimlaneMapping
	{
		private readonly ConcurrentDictionary<TopicPartition, int> _mapping = new();
		private int _nextSwimlane;

		public int ToSwimlane(TopicPartition topicPartition, string messageKey)
		{
			return _mapping.GetOrAdd(topicPartition, _ => Interlocked.Increment(ref _nextSwimlane) - 1);
		}
	}
}
