using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureManager
{
	private readonly BackPressureConfig _backPressureConfig;
	private readonly HashSet<TopicPartition> _allTopicPartitions = new();
	
	private BackPressureManagerState _state = new BackPressureManagerNormalState();

	public BackPressureManager(BackPressureConfig backPressureConfig)
	{
		_backPressureConfig = backPressureConfig;
	}
	
	public BackPressureActions Update(ConsumeResult<string, string> record, int backlog)
	{
		_allTopicPartitions.Add(new TopicPartition(record.Topic, record.Partition));
		
		BackPressureManagerStateAndActions stateAndActions = _state.Update(_allTopicPartitions, backlog, _backPressureConfig);
		_state = stateAndActions.State;
		return stateAndActions.Actions;
	}
}