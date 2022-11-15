using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public interface BackPressureManagerState
{
	BackPressureManagerStateAndActions Update(HashSet<TopicPartition> allTopicPartitions, int backlog, BackPressureConfig backPressureConfig);
}