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
		public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

		public static EventuateKafkaConsumerConfigurationProperties Empty()
		{
			return new EventuateKafkaConsumerConfigurationProperties();
		}
	}
}