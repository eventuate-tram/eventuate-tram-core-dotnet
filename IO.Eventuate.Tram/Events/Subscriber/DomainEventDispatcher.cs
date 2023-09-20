/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-events
 * package:	io.eventuate.tram.events.subscriber
 */

using System;
using System.Threading;
using System.Threading.Tasks;
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
		private readonly Func<Task<DomainEventHandlers>> _domainEventHandlersFactory;
		private DomainEventHandlers _domainEventHandlers;
		private readonly IMessageConsumer _messageConsumer;
		private readonly IEventTypeNamingStrategy _eventTypeNamingStrategy;

		private IMessageSubscription _subscription;

		public DomainEventDispatcher(string subscriberId, DomainEventHandlers domainEventHandlers,
			IMessageConsumer messageConsumer, IEventTypeNamingStrategy eventTypeNamingStrategy,
			ILogger<DomainEventDispatcher> logger)
			: this(subscriberId, () => Task.FromResult(domainEventHandlers), messageConsumer,
				eventTypeNamingStrategy, logger)
		{
		}

		public DomainEventDispatcher(string subscriberId, Func<Task<DomainEventHandlers>> domainEventHandlersFactory,
			IMessageConsumer messageConsumer, IEventTypeNamingStrategy eventTypeNamingStrategy,
			ILogger<DomainEventDispatcher> logger)
		{
			_subscriberId = subscriberId;
			_domainEventHandlersFactory = domainEventHandlersFactory;
			_messageConsumer = messageConsumer;
			_eventTypeNamingStrategy = eventTypeNamingStrategy;
			_logger = logger;
			_dispatcherContext = $"SubscriberId='{subscriberId}'";
		}

		public async Task InitializeAsync()
		{
			_domainEventHandlers = await _domainEventHandlersFactory();
			_subscription = _messageConsumer.Subscribe(_subscriberId, _domainEventHandlers.GetAggregateTypes(),
				MessageHandlerAsync);
		}

		public async Task StopAsync()
		{
			if (_subscription == null)
			{
				return;
			}
			
			await _subscription.UnsubscribeAsync();
		}

		public async Task MessageHandlerAsync(IMessage message, IServiceProvider serviceProvider, CancellationToken cancellationToken)
		{
			var logContext = $"{nameof(MessageHandlerAsync)} on {_dispatcherContext}, MessageId={message.Id}";
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

			await handler.InvokeAsync(envelope, serviceProvider, cancellationToken);
			_logger.LogDebug($"-{logContext}: Processed message of type='{aggregateType}'");
		}
	}
}