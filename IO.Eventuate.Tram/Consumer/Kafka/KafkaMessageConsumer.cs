/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-consumer-kafka
 * package:	io.eventuate.tram.consumer.kafka
 */

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using IO.Eventuate.Tram.Consumer.Common;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Consumer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Consumer.Kafka
{
	public class KafkaMessageConsumer : IMessageConsumer, IDisposable
	{
		private readonly ILogger _logger;
		private readonly EventuateKafkaConsumerConfigurationProperties _eventuateKafkaConsumerConfigurationProperties;
		private readonly IEnumerable<IMessageInterceptor> _messageInterceptors;
		private readonly DecoratedMessageHandlerFactory _decoratedMessageHandlerFactory;
		private readonly ILoggerFactory _loggerFactory;
		private readonly IServiceScopeFactory _serviceScopeFactory;

		private readonly string _id = Guid.NewGuid().ToString();
		private readonly string _bootstrapServers;
		private readonly List<EventuateKafkaConsumer> _consumers = new List<EventuateKafkaConsumer>();

		public KafkaMessageConsumer(string bootstrapServers,
			EventuateKafkaConsumerConfigurationProperties eventuateKafkaConsumerConfigurationProperties,
			IEnumerable<IMessageInterceptor> messageInterceptors,
			DecoratedMessageHandlerFactory decoratedMessageHandlerFactory, ILoggerFactory loggerFactory,
			IServiceScopeFactory serviceScopeFactory)
		{
			_bootstrapServers = bootstrapServers;
			_eventuateKafkaConsumerConfigurationProperties = eventuateKafkaConsumerConfigurationProperties;
			_messageInterceptors = messageInterceptors;
			_decoratedMessageHandlerFactory = decoratedMessageHandlerFactory;
			_loggerFactory = loggerFactory;
			_serviceScopeFactory = serviceScopeFactory;
			_logger = _loggerFactory.CreateLogger<KafkaMessageConsumer>();
		}

		public IMessageSubscription Subscribe(string subscriberId, ISet<string> channels, MessageHandler handler)
		{
			var logContext = $"{nameof(Subscribe)} for subscriberId='{subscriberId}', " +
			                 $"channels='{String.Join(",", channels)}', " +
			                 $"handler='{handler.Method.Name}'";
			_logger.LogDebug($"+{logContext}");
			
			Action<SubscriberIdAndMessage> decoratedHandler = _decoratedMessageHandlerFactory.Decorate(handler);
			
			var swimLaneBasedDispatcher = new SwimlaneBasedDispatcher(subscriberId, _loggerFactory);

			EventuateKafkaConsumerMessageHandler kcHandler =
				(record, completionCallback) => swimLaneBasedDispatcher.Dispatch(ToMessage(record), record.Partition,
					message => Handle(message, completionCallback, subscriberId, decoratedHandler));
			
			var kc = new EventuateKafkaConsumer(subscriberId,
				kcHandler,
				new List<string>(channels),
				_bootstrapServers,
				_eventuateKafkaConsumerConfigurationProperties,
				_loggerFactory);

			_consumers.Add(kc);

			kc.Start();
			
			_logger.LogDebug($"-{logContext}");
			return new MessageSubscription(() =>
			{
				kc.Dispose();
				_consumers.Remove(kc);
			});
		}

		public static void Handle(IMessage message, Action<Exception> completionCallback, string subscriberId,
			Action<SubscriberIdAndMessage> decoratedHandler)
		{
			try
			{
				decoratedHandler(new SubscriberIdAndMessage(subscriberId, message));
				completionCallback(null);
			}
			catch (Exception e)
			{
				completionCallback(e);
				throw;
			}
		}

		public string GetId()
		{
			return _id;
		}

		// TODO: Java added this Close method. Do we still need Dispose?
		public void Close()
		{
			foreach (EventuateKafkaConsumer consumer in _consumers)
			{
				// TODO: Java calls consumer.stop() here
				consumer.Dispose();
			}
		}

		/// <inheritdoc />
		private class MessageSubscription : IMessageSubscription
		{
			private readonly Action _unsubscribe;

			public MessageSubscription(Action unsubscribe)
			{
				_unsubscribe = unsubscribe;
			}

			/// <inheritdoc />
			public void Unsubscribe()
			{
				_unsubscribe();
			}
		}
		
		private IMessage ToMessage(ConsumeResult<string, string> record)
		{
			return JsonMapper.FromJson<Message>(record.Value);
		}

		public void Dispose()
		{
			_logger.LogDebug($"+{nameof(Dispose)}");
			foreach (EventuateKafkaConsumer consumer in _consumers)
			{
				consumer.Dispose();
			}
			_logger.LogDebug($"-{nameof(Dispose)}");
		}
	}
}