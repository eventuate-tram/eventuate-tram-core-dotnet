/*
 * Ported from:
 * repo:	https://github.com/eventuate-local/eventuate-local
 * module:	eventuate-local-java-kafka
 * package:	io.eventuate.local.java.kafka.consumer
 */

using System;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer
{
	public delegate Task<IMessageConsumerBacklog> EventuateKafkaConsumerMessageHandler(ConsumeResult<string, string> consumeResult,
		Action<Exception> callback);
}