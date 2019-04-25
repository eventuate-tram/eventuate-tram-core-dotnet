using System.Collections.Generic;

namespace IO.Eventuate.Tram.Messaging.Consumer.Kafka
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