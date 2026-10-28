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
	public async Task GetBacklogByTopic_CountsOnlyEachTopicsSwimlanes()
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
		var backlogByTopic = new Dictionary<string, int> { ["stale"] = 99 };
		processor.GetBacklogByTopic(backlogByTopic);
		Assert.Multiple(() =>
		{
			Assert.That(backlogByTopic, Is.EquivalentTo(new Dictionary<string, int> { ["topic1"] = 7, ["topic2"] = 20 }), "backlog by topic");
			Assert.That(processor.GetBacklog(), Is.EqualTo(27), "total backlog");
		});
	}
}
