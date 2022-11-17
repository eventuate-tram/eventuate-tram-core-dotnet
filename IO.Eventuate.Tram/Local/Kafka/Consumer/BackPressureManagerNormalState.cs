using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureManagerNormalState : BackPressureManagerState
{
	public static BackPressureManagerStateAndActions TransitionTo(HashSet<TopicPartition> suspendedPartitions) {
		return new BackPressureManagerStateAndActions(BackPressureActions.resume(suspendedPartitions), new BackPressureManagerNormalState());
	}
	
	public BackPressureManagerStateAndActions Update(HashSet<TopicPartition> allTopicPartitions, int backlog, BackPressureConfig backPressureConfig)
	{
		return backlog > backPressureConfig.PauseThreshold ? BackPressureManagerPausedState.TransitionTo(allTopicPartitions) : new BackPressureManagerStateAndActions(this);
	}
}