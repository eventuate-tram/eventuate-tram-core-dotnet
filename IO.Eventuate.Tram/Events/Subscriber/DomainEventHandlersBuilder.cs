/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-events
 * package:	io.eventuate.tram.events.subscriber
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Events.Common;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public class DomainEventHandlersBuilder
	{
		private string _aggregateType;
		private readonly IList<DomainEventHandler> _handlers = new List<DomainEventHandler>();

		public DomainEventHandlersBuilder(string aggregateType)
		{
			_aggregateType = aggregateType;
		}

		public static DomainEventHandlersBuilder ForAggregateType(string aggregateType)
		{
			return new DomainEventHandlersBuilder(aggregateType);
		}

		public DomainEventHandlersBuilder OnEvent<TEvent>(Func<IDomainEventEnvelope<TEvent>, Task> handler)
			where TEvent : IDomainEvent
		{
			_handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent),
				(e, p, _) => handler((IDomainEventEnvelope<TEvent>) e)));
			return this;
		}

		public DomainEventHandlersBuilder OnEvent<TEvent>(
			Func<IDomainEventEnvelope<TEvent>, IServiceProvider, Task> handler) where TEvent : IDomainEvent
		{
			_handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent),
				(e, p, _) => handler((IDomainEventEnvelope<TEvent>) e, p)));
			return this;
		}
		
		public DomainEventHandlersBuilder OnEvent<TEvent>(Func<IDomainEventEnvelope<TEvent>, CancellationToken, Task> handler)
			where TEvent : IDomainEvent
		{
			_handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent),
				(e, p, cancellationToken) => handler((IDomainEventEnvelope<TEvent>) e, cancellationToken)));
			return this;
		}

		public DomainEventHandlersBuilder OnEvent<TEvent>(
			Func<IDomainEventEnvelope<TEvent>, IServiceProvider, CancellationToken, Task> handler) where TEvent : IDomainEvent
		{
			_handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent),
				(e, p, cancellationToken) => handler((IDomainEventEnvelope<TEvent>) e, p, cancellationToken)));
			return this;
		}

		public DomainEventHandlersBuilder OnEvent<TEvent, TEventHandler>()
			where TEvent : IDomainEvent
			where TEventHandler : IDomainEventHandler<TEvent>
		{
			_handlers.Add(new DomainEventHandler(_aggregateType, typeof(TEvent), async (e, p, cancellationToken) =>
			{
				var eventHandler = p.GetRequiredService<TEventHandler>();
				await eventHandler.HandleAsync((IDomainEventEnvelope<TEvent>) e, cancellationToken);
			}));
			return this;
		}

		public DomainEventHandlersBuilder AndForAggregateType(string aggregateType)
		{
			_aggregateType = aggregateType;
			return this;
		}

		public DomainEventHandlers Build()
		{
			return new DomainEventHandlers(_handlers);
		}
	}
}