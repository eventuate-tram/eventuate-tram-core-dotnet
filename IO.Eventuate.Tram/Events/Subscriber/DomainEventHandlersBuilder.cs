using System;
using System.Collections.Generic;
using IO.Eventuate.Tram.Events.Common;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public class DomainEventHandlersBuilder
	{
		private readonly string _aggregateType;
		private readonly IList<DomainEventHandler> _handlers = new List<DomainEventHandler>();

		public DomainEventHandlersBuilder(string aggregateType)
		{
			_aggregateType = aggregateType;
		}

		public static DomainEventHandlersBuilder ForAggregateType(string aggregateType)
		{
			return new DomainEventHandlersBuilder(aggregateType);
		}

		public DomainEventHandlersBuilder OnEvent<TEvent>(Action<IDomainEventEnvelope<TEvent>> handler) where TEvent : IDomainEvent
		{
			_handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent), (e, p) => handler((IDomainEventEnvelope<TEvent>) e)));
			return this;
		}

		public DomainEventHandlersBuilder OnEvent<TEvent, TEventHandler>()
			where TEvent : IDomainEvent
			where TEventHandler : IDomainEventHandler<TEvent>
		{
			_handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent), (e, p) =>
			{
				var eventHandler = p.GetRequiredService<TEventHandler>();
				eventHandler.Handle((IDomainEventEnvelope<TEvent>) e);
			}));
			return this;
		}

		public DomainEventHandlers Build()
		{
			return new DomainEventHandlers(_handlers);
		}
	}
}