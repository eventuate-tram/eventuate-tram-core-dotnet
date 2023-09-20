using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public interface IBackPressureManagerState
{
	BackPressureManagerStateAndActions Update(ISet<TopicPartition> allTopicPartitions, int backlog, BackPressureConfig backPressureConfig);
}