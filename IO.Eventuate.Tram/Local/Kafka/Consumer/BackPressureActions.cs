using System.Collections.Generic;
using System.Collections.Immutable;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureActions
{
	public IImmutableSet<TopicPartition> PartitionsToPause { get; }
	public IImmutableSet<TopicPartition> PartitionsToResume { get; }

	private BackPressureActions(ISet<TopicPartition> partitionsToPause, ISet<TopicPartition> partitionsToResume)
	{
		PartitionsToPause = partitionsToPause.ToImmutableHashSet();
		PartitionsToResume = partitionsToResume.ToImmutableHashSet();
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