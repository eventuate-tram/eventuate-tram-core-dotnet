/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-events
 * package:	io.eventuate.tram.events.subscriber
 */

using System;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Consumer;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public class DomainEventDispatcher
	{
		private readonly ILogger _logger;
		private readonly string _dispatcherContext;
		private readonly string _subscriberId;
		private readonly DomainEventHandlers _domainEventHandlers;
		private readonly IMessageConsumer _messageConsumer;
		private readonly IEventTypeNamingStrategy _eventTypeNamingStrategy;

		private IMessageSubscription _subscription;

		public DomainEventDispatcher(string subscriberId, DomainEventHandlers domainEventHandlers,
			IMessageConsumer messageConsumer, IEventTypeNamingStrategy eventTypeNamingStrategy,
			ILogger<DomainEventDispatcher> logger)
		{
			_subscriberId = subscriberId;
			_domainEventHandlers = domainEventHandlers;
			_messageConsumer = messageConsumer;
			_eventTypeNamingStrategy = eventTypeNamingStrategy;
			_logger = logger;
			_dispatcherContext = $"SubscriberId='{subscriberId}', " +
			                     $"DomainEventHandlers for'{String.Join(",", domainEventHandlers.GetAggregateTypes())}'";
		}

		public void Initialize()
		{
			_subscription = _messageConsumer.Subscribe(_subscriberId, _domainEventHandlers.GetAggregateTypes(),
				MessageHandler);
		}

		public void Stop()
		{
			_subscription?.Unsubscribe();
		}

		public void MessageHandler(IMessage message, IServiceProvider serviceProvider)
		{
			var logContext = $"{nameof(MessageHandler)} on {_dispatcherContext}, MessageId={message.Id}";
			_logger.LogDebug($"+{logContext}");
			string aggregateType = message.GetRequiredHeader(EventMessageHeaders.AggregateType);

			DomainEventHandler handler = _domainEventHandlers.FindTargetMethod(message, _eventTypeNamingStrategy);

			if (handler == null)
			{
				_logger.LogDebug($"{logContext}: No handler found for type='{aggregateType}'");
				return;
			}

			var param = (IDomainEvent)JsonMapper.FromJson(message.Payload, handler.EventType);
			
			Type envelopeType = typeof(DomainEventEnvelope<>).MakeGenericType(handler.EventType);
			var envelope = (IDomainEventEnvelope<IDomainEvent>) Activator.CreateInstance(envelopeType,
				message,
				aggregateType,
				message.GetRequiredHeader(EventMessageHeaders.AggregateId),
				message.GetRequiredHeader(MessageHeaders.Id),
				param);

			handler.Invoke(envelope, serviceProvider);
			_logger.LogDebug($"-{logContext}: Processed message of type='{aggregateType}'");
		}
	}
}