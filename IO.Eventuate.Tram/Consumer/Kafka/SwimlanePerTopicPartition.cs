/*
 * Ported from:
 * repo:	https://github.com/eventuate-foundation/eventuate-messaging-kafka
 * module:	eventuate-messaging-kafka-consumer
 * package:	io.eventuate.messaging.kafka.consumer
 */

using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Consumer.Kafka
{
	/// <summary>
	/// One swimlane per topic partition, so a subscriber's messages from different topics are handled concurrently,
	/// and one topic's messages never wait behind another's. Swimlanes are numbered from 0 in the order their topic
	/// partitions are first seen, so use one instance per subscription: <c>KafkaMessageConsumer</c> creates one for
	/// each subscription when the back pressure scope is <c>Topic</c> and no mapping is registered; if you register
	/// it, register it as transient.
	/// </summary>
	public class SwimlanePerTopicPartition : ITopicPartitionToSwimlaneMapping
	{
		private readonly Dictionary<TopicPartition, int> _mapping = new();
		private readonly object _lock = new();

		public int ToSwimlane(TopicPartition topicPartition, string messageKey)
		{
			lock (_lock)
			{
				if (!_mapping.TryGetValue(topicPartition, out int swimlane))
				{
					swimlane = _mapping.Count;
					_mapping[topicPartition] = swimlane;
				}

				return swimlane;
			}
		}
	}
}
