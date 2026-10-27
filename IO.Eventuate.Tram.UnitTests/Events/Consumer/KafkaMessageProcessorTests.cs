using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace IO.Eventuate.Tram.UnitTests.Events.Consumer;

public class KafkaMessageProcessorTests
{
	private static IMessageConsumerBacklog Backlog(int size)
	{
		var backlog = Substitute.For<IMessageConsumerBacklog>();
		backlog.Size().Returns(size);
		return backlog;
	}

	private static ConsumeResult<string, string> Record(string topic, int partition, long offset) => new()
	{
		Topic = topic,
		Partition = new Partition(partition),
		Offset = new Offset(offset),
		Message = new Message<string, string> { Key = "key", Value = "value" }
	};

	[Test]
	public async Task GetBacklog_ForTopic_CountsOnlyThatTopicsSwimlanes()
	{
		// Arrange
		IMessageConsumerBacklog topic1Partition0 = Backlog(3);
		IMessageConsumerBacklog topic1Partition1 = Backlog(4);
		IMessageConsumerBacklog topic2Partition0 = Backlog(20);
		var backlogs = new Dictionary<TopicPartition, IMessageConsumerBacklog>
		{
			[new TopicPartition("topic1", new Partition(0))] = topic1Partition0,
			[new TopicPartition("topic1", new Partition(1))] = topic1Partition1,
			[new TopicPartition("topic2", new Partition(0))] = topic2Partition0
		};
		var processor = new KafkaMessageProcessor("subscriber",
			(record, _) => Task.FromResult(backlogs[record.TopicPartition]),
			NullLogger<KafkaMessageProcessor>.Instance);

		// Act
		await processor.ProcessAsync(Record("topic1", 0, 1));
		await processor.ProcessAsync(Record("topic1", 0, 2));
		await processor.ProcessAsync(Record("topic1", 1, 1));
		await processor.ProcessAsync(Record("topic2", 0, 1));

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(processor.GetBacklog("topic1"), Is.EqualTo(7), "topic1 backlog");
			Assert.That(processor.GetBacklog("topic2"), Is.EqualTo(20), "topic2 backlog");
			Assert.That(processor.GetBacklog("topic3"), Is.EqualTo(0), "topic3 backlog");
			Assert.That(processor.GetBacklog(), Is.EqualTo(27), "total backlog");
			Assert.That(processor.GetTopics(), Is.EquivalentTo(new[] { "topic1", "topic2" }), "topics");
		});
	}
}
