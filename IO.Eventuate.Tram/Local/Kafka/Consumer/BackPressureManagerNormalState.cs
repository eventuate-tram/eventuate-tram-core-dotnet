using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureManagerNormalState : IBackPressureManagerState
{
	public static BackPressureManagerStateAndActions TransitionTo(ISet<TopicPartition> suspendedPartitions) {
		return new BackPressureManagerStateAndActions(BackPressureActions.Resume(suspendedPartitions), new BackPressureManagerNormalState());
	}
	
	public BackPressureManagerStateAndActions Update(ISet<TopicPartition> allTopicPartitions, int backlog, BackPressureConfig backPressureConfig)
	{
		return backlog > backPressureConfig.PauseThreshold ? BackPressureManagerPausedState.TransitionTo(allTopicPartitions) : new BackPressureManagerStateAndActions(this);
	}
}