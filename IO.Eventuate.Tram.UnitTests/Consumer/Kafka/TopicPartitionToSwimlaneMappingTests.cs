using Confluent.Kafka;
using IO.Eventuate.Tram.Consumer.Kafka;
using NUnit.Framework;

namespace IO.Eventuate.Tram.UnitTests.Consumer.Kafka;

public class TopicPartitionToSwimlaneMappingTests
{
	[Test]
	public void OriginalTopicPartitionToSwimlaneMapping_SamePartitionOnDifferentTopics_SharesSwimlane()
	{
		// Arrange
		var mapping = new OriginalTopicPartitionToSwimlaneMapping();

		// Act
		int swimlane1 = mapping.ToSwimlane(new TopicPartition("topic1", new Partition(3)), "key1");
		int swimlane2 = mapping.ToSwimlane(new TopicPartition("topic2", new Partition(3)), "key2");

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(swimlane1, Is.EqualTo(3), "topic1 swimlane");
			Assert.That(swimlane2, Is.EqualTo(3), "topic2 swimlane");
		});
	}

	[Test]
	public void SwimlanePerTopicPartition_SamePartitionOnDifferentTopics_GetsSwimlanesOfTheirOwn()
	{
		// Arrange
		var mapping = new SwimlanePerTopicPartition();

		// Act
		int swimlane1 = mapping.ToSwimlane(new TopicPartition("topic1", new Partition(3)), "key1");
		int swimlane2 = mapping.ToSwimlane(new TopicPartition("topic2", new Partition(3)), "key2");

		// Assert
		Assert.That(swimlane2, Is.Not.EqualTo(swimlane1));
	}

	[Test]
	public void SwimlanePerTopicPartition_SameTopicPartition_KeepsItsSwimlane()
	{
		// Arrange
		var mapping = new SwimlanePerTopicPartition();
		int swimlane = mapping.ToSwimlane(new TopicPartition("topic1", new Partition(3)), "key1");
		mapping.ToSwimlane(new TopicPartition("topic2", new Partition(3)), "key1");

		// Act
		int sameSwimlane = mapping.ToSwimlane(new TopicPartition("topic1", new Partition(3)), "key2");

		// Assert
		Assert.That(sameSwimlane, Is.EqualTo(swimlane));
	}
}
