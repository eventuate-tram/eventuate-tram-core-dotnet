/*
 * Ported from:
 * repo:	https://github.com/eventuate-local/eventuate-local
 * module:	eventuate-local-java-kafka
 * package:	io.eventuate.local.java.kafka.consumer
 */

namespace IO.Eventuate.Tram.Local.Kafka.Consumer
{
	public enum EventuateKafkaConsumerState
	{
		MessageHandlingFailed,
		Started,
		FailedToStart,
		Stopped,
		Failed,
		Created
	}
}