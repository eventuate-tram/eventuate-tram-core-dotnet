﻿using System.Collections.Generic;
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
}