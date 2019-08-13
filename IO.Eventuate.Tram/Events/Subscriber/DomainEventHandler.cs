/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-events
 * package:	io.eventuate.tram.events.subscriber
 */

using System;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public class DomainEventHandler
	{
		private readonly Action<IDomainEventEnvelope<IDomainEvent>, IServiceProvider> _handler;

		public DomainEventHandler(string aggregateType, Type eventType,
			Action<IDomainEventEnvelope<IDomainEvent>, IServiceProvider> handler)
		{
			AggregateType = aggregateType;
			EventType = eventType;
			_handler = handler;
		}

		public bool Handles(IMessage message, IEventTypeNamingStrategy eventTypeNamingStrategy)
		{
			string eventTypeName = eventTypeNamingStrategy.GetEventTypeName(EventType);
			return AggregateType.Equals(message.GetRequiredHeader(EventMessageHeaders.AggregateType))
			       && String.Equals(eventTypeName, message.GetRequiredHeader(EventMessageHeaders.EventType));
		}

		public void Invoke(IDomainEventEnvelope<IDomainEvent> domainEventEnvelope, IServiceProvider serviceProvider)
		{
			_handler(domainEventEnvelope, serviceProvider);
		}

		public Type EventType { get; }

		public string AggregateType { get; }
	}
}