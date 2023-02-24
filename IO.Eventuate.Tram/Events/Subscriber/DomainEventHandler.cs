/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-events
 * package:	io.eventuate.tram.events.subscriber
 */

using System;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public class DomainEventHandler
	{
		private readonly Func<IDomainEventEnvelope<IDomainEvent>, IServiceProvider, Task> _handler;

		public DomainEventHandler(string aggregateType, Type eventType,
			Func<IDomainEventEnvelope<IDomainEvent>, IServiceProvider, Task> handler)
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

		public async Task InvokeAsync(IDomainEventEnvelope<IDomainEvent> domainEventEnvelope, IServiceProvider serviceProvider)
		{
			await _handler(domainEventEnvelope, serviceProvider);
		}

		public Type EventType { get; }

		public string AggregateType { get; }
	}
}