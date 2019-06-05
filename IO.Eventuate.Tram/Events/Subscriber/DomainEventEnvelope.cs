/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-events
 * package:	io.eventuate.tram.events.subscriber
 */

using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public class DomainEventEnvelope<T> : IDomainEventEnvelope<T> where T : IDomainEvent
	{
		public DomainEventEnvelope(IMessage message, string aggregateType, string aggregateId, string eventId, T @event)
		{
			Message = message;
			AggregateType = aggregateType;
			AggregateId = aggregateId;
			EventId = eventId;
			Event = @event;			
		}

		public string AggregateId { get; }

		public IMessage Message { get; }

		public T Event { get; }

		public string AggregateType { get; }

		public string EventId { get; }
	}
}