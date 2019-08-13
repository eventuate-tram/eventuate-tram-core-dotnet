/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-events
 * package:	io.eventuate.tram.events.publisher
 */

using System.Collections.Generic;
using IO.Eventuate.Tram.Events.Common;

namespace IO.Eventuate.Tram.Events.Publisher
{
	public interface IDomainEventPublisher
	{
		void Publish(string aggregateType, object aggregateId, IList<IDomainEvent> domainEvents);
		void Publish(string aggregateType, object aggregateId, IDictionary<string, string> headers, IList<IDomainEvent> domainEvents);
		void Publish<TAggregate>(object aggregateId, IList<IDomainEvent> domainEvents);
	}
}