/*
 * Ported from:
 * repo:	https://github.com/eventuate-local/eventuate-local
 * module:	eventuate-local-java-kafka
 * package:	io.eventuate.local.java.kafka.consumer
 */

using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer
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