using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureManagerPausedState : IBackPressureManagerState
{
	private readonly ISet<TopicPartition> _suspendedPartitions;

	public BackPressureManagerPausedState(ISet<TopicPartition> pausedTopic)
	{
		_suspendedPartitions = pausedTopic;
	}

	public static BackPressureManagerStateAndActions TransitionTo(ISet<TopicPartition> allTopicPartitions)
	{
		return new BackPressureManagerStateAndActions(BackPressureActions.Pause(allTopicPartitions),
			new BackPressureManagerPausedState(allTopicPartitions));
	}

	public BackPressureManagerStateAndActions Update(ISet<TopicPartition> allTopicPartitions, int backlog,
		BackPressureConfig backPressureConfig)
	{
		if (backlog <= backPressureConfig.ResumeThreshold)
		{
			return BackPressureManagerNormalState.TransitionTo(_suspendedPartitions);
		}

		HashSet<TopicPartition> toSuspend = new HashSet<TopicPartition>(allTopicPartitions);
		toSuspend.ExceptWith(_suspendedPartitions);
		return new BackPressureManagerStateAndActions(BackPressureActions.Pause(toSuspend), this);
	}
}