using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public class DomainEventHandlers
	{
		private readonly IList<DomainEventHandler> _handlers;

		public DomainEventHandlers(IList<DomainEventHandler> handlers)
		{
			_handlers = handlers;
		}

		public ISet<string> GetAggregateTypes()
		{
			return _handlers.Select(h => h.AggregateType).ToImmutableHashSet();
		}

		public DomainEventHandler FindTargetMethod(IMessage message, IEventTypeNamingStrategy eventTypeNamingStrategy)
		{
			return _handlers.FirstOrDefault(h => h.Handles(message, eventTypeNamingStrategy));
		}
	}
}