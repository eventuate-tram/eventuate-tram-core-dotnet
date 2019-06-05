/*
 * Ported from:
 * repo:	https://github.com/eventuate-local/eventuate-local
 * module:	eventuate-local-java-kafka
 * package:	io.eventuate.local.java.kafka.consumer
 */

using System;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer
{
	public class KafkaMessageProcessorFailedException : Exception
	{
		public KafkaMessageProcessorFailedException(string message)
			: base(message)
		{

		}
		
		public KafkaMessageProcessorFailedException(string message, Exception innerException)
			: base(message, innerException)
		{

		}
	}
}