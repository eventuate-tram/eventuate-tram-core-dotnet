/*
 * Ported from:
 * repo:	https://github.com/eventuate-local/eventuate-local
 * module:	eventuate-local-java-kafka
 * package:	io.eventuate.local.java.kafka.consumer
 */

using System;
using Confluent.Kafka;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer
{
	public delegate void EventuateKafkaConsumerMessageHandler(ConsumeResult<string, string> consumeResult,
		Action<Exception> callback);
}