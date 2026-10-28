using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureManager
{
	private readonly BackPressureConfig _backPressureConfig;
	private readonly HashSet<TopicPartition> _allTopicPartitions = new();
	
	private IBackPressureManagerState _state = new BackPressureManagerNormalState();

	public BackPressureManager(BackPressureConfig backPressureConfig)
	{
		_backPressureConfig = backPressureConfig;
	}
	
	public BackPressureActions Update(ISet<TopicPartition> topicPartitions, int backlog)
	{
		_allTopicPartitions.UnionWith(topicPartitions);

		BackPressureManagerStateAndActions stateAndActions = _state.Update(_allTopicPartitions, backlog, _backPressureConfig);
		_state = stateAndActions.State;
		return stateAndActions.Actions;
	}

	/// <summary>
	/// Forget partitions the consumer no longer owns (e.g. revoked in a rebalance), so they aren't paused or resumed
	/// </summary>
	public void RemovePartitions(IEnumerable<TopicPartition> topicPartitions)
	{
		var removed = new HashSet<TopicPartition>(topicPartitions);
		_allTopicPartitions.ExceptWith(removed);
		if (_state is BackPressureManagerPausedState pausedState)
		{
			pausedState.RemovePartitions(removed);
		}
	}
}