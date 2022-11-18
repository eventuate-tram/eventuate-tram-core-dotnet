using System.Collections.Generic;
using System.Collections.Immutable;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureActions
{
	public ISet<TopicPartition> PartitionsToPause { get; }
	public ISet<TopicPartition> PartitionsToResume { get; }

	public BackPressureActions(ISet<TopicPartition> partitionsToPause, ISet<TopicPartition> partitionsToResume)
	{
		PartitionsToPause = partitionsToPause;
		PartitionsToResume = partitionsToResume;
	}

	public static readonly BackPressureActions None =
		new(ImmutableHashSet<TopicPartition>.Empty, ImmutableHashSet<TopicPartition>.Empty);
	
	public static BackPressureActions Pause(ISet<TopicPartition> topicPartitions) {
		return new BackPressureActions(topicPartitions, ImmutableHashSet<TopicPartition>.Empty);
	}

	public static BackPressureActions Resume(ISet<TopicPartition> topicPartitions) {
		return new BackPressureActions(ImmutableHashSet<TopicPartition>.Empty, topicPartitions);
	}
}