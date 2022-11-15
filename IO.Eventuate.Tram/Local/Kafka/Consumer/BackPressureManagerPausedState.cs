using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureManagerPausedState : BackPressureManagerState
{
	private readonly HashSet<TopicPartition> _suspendedPartitions;

	public BackPressureManagerPausedState(HashSet<TopicPartition> pausedTopic)
	{
		_suspendedPartitions = pausedTopic;
	}

	public static BackPressureManagerStateAndActions TransitionTo(HashSet<TopicPartition> allTopicPartitions)
	{
		return new BackPressureManagerStateAndActions(BackPressureActions.pause(allTopicPartitions),
			new BackPressureManagerPausedState(allTopicPartitions));
	}

	public BackPressureManagerStateAndActions Update(HashSet<TopicPartition> allTopicPartitions, int backlog,
		BackPressureConfig backPressureConfig)
	{
		if (backlog <= backPressureConfig.Low)
		{
			return BackPressureManagerNormalState.TransitionTo(_suspendedPartitions);
		}
		else
		{
			HashSet<TopicPartition> toSuspend = new HashSet<TopicPartition>(allTopicPartitions);
			toSuspend.RemoveWhere(topic => _suspendedPartitions.Contains(topic));
			return new BackPressureManagerStateAndActions(BackPressureActions.pause(toSuspend), this);
		}
	}
}