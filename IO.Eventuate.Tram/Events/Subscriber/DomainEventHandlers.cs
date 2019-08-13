/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-events
 * package:	io.eventuate.tram.events.subscriber
 */

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public class DomainEventHandlers
	{
		public IList<DomainEventHandler> Handlers { get; }

		public DomainEventHandlers(IList<DomainEventHandler> handlers)
		{
			Handlers = handlers;
		}

		public ISet<string> GetAggregateTypes()
		{
			return Handlers.Select(h => h.AggregateType).ToImmutableHashSet();
		}

		public DomainEventHandler FindTargetMethod(IMessage message, IEventTypeNamingStrategy eventTypeNamingStrategy)
		{
			return Handlers.FirstOrDefault(h => h.Handles(message, eventTypeNamingStrategy));
		}
	}
}