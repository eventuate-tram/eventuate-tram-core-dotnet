namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

/// <summary>
/// What a consumer's back pressure thresholds apply to.
/// </summary>
public enum BackPressureScope
{
	/// <summary>
	/// The backlog of all of the consumer's partitions together: once it passes the pause threshold, every
	/// partition is paused.
	/// </summary>
	AllPartitions,

	/// <summary>
	/// Each topic's backlog on its own: once a topic's passes the pause threshold, only that topic's partitions are
	/// paused, and the consumer's other topics carry on. Use with <c>SwimlanePerTopicPartition</c>, so that each
	/// topic's messages are on swimlanes of their own; with a shared swimlane, a topic's backlog includes the other
	/// topics' messages on it.
	/// </summary>
	Topic
}
