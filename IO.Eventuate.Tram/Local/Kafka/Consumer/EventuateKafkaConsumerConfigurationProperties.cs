/*
 * Ported from:
 * repo:	https://github.com/eventuate-local/eventuate-local
 * module:	eventuate-local-java-kafka
 * package:	io.eventuate.local.java.kafka.consumer
 */

using System.Collections.Generic;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer
{
	public class EventuateKafkaConsumerConfigurationProperties
	{
		public long PollTimeout { get; set; } = 100;

		public BackPressureConfig BackPressure { get; set; } = new();
		
		public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

		public static EventuateKafkaConsumerConfigurationProperties Empty()
		{
			return new EventuateKafkaConsumerConfigurationProperties();
		}
	}
}