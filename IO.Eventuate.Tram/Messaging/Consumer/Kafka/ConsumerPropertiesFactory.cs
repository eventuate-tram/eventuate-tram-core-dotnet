using System.Collections.Generic;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Messaging.Consumer.Kafka
{
	public static class ConsumerPropertiesFactory
	{
		public static ConsumerConfig MakeDefaultConsumerProperties(string bootstrapServers,
			string subscriberId)
		{
			var consumerProperties = new ConsumerConfig
			{
				BootstrapServers = bootstrapServers,
				GroupId = subscriberId,
				EnableAutoCommit = false,
				SessionTimeoutMs = 30000,
				AutoOffsetReset = AutoOffsetReset.Earliest
			};

			return consumerProperties;
		}
	}
}