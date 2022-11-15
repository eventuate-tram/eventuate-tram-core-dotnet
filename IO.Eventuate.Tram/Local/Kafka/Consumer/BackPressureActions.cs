using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureActions
{
	public HashSet<TopicPartition> Pause { get; }
	public HashSet<TopicPartition> Resume { get; }

	public BackPressureActions(HashSet<TopicPartition> pause, HashSet<TopicPartition> resume)
	{
		Pause = pause;
		Resume = resume;
	}

	public static readonly BackPressureActions NONE =
		new BackPressureActions(new HashSet<TopicPartition>(), new HashSet<TopicPartition>());
	
	public static BackPressureActions pause(HashSet<TopicPartition> topicPartitions) {
		return new BackPressureActions(topicPartitions, new HashSet<TopicPartition>());
	}

	public static BackPressureActions resume(HashSet<TopicPartition> topicPartitions) {
		return new BackPressureActions(new HashSet<TopicPartition>(), topicPartitions);
	}
}