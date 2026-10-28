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
	/// Maps the topic partition (and key) a message was received on to the swimlane it's dispatched on.
	/// A swimlane's messages are handled one at a time, in order; different swimlanes are handled concurrently.
	/// To use a mapping other than the default, register it in the service collection. A mapping is created for each
	/// subscription, so register one that keeps state (such as <see cref="SwimlanePerTopicPartition"/>) as transient.
	/// The default is <see cref="SwimlanePerTopicPartition"/> when the back pressure scope is <c>Topic</c>, and
	/// <see cref="OriginalTopicPartitionToSwimlaneMapping"/> otherwise.
	/// </summary>
	public interface ITopicPartitionToSwimlaneMapping
	{
		int ToSwimlane(TopicPartition topicPartition, string messageKey);
	}
}
