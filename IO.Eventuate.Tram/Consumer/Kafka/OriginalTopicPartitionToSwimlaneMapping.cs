/*
 * Ported from:
 * repo:	https://github.com/eventuate-foundation/eventuate-messaging-kafka
 * module:	eventuate-messaging-kafka-consumer
 * package:	io.eventuate.messaging.kafka.consumer
 */

using Confluent.Kafka;

namespace IO.Eventuate.Tram.Consumer.Kafka
{
	/// <summary>
	/// The default mapping: one swimlane per partition number. Partitions with the same number on different topics
	/// share a swimlane, so their messages are handled one at a time between them.
	/// </summary>
	public class OriginalTopicPartitionToSwimlaneMapping : ITopicPartitionToSwimlaneMapping
	{
		public int ToSwimlane(TopicPartition topicPartition, string messageKey)
		{
			return topicPartition.Partition.Value;
		}
	}
}
