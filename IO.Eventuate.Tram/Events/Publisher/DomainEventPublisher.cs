using System.Collections.Generic;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Producer;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Events.Publisher
{
	public class DomainEventPublisher : IDomainEventPublisher
	{
		private readonly ILogger _logger;
		private readonly IMessageProducer _messageProducer;
		private readonly IEventTypeNamingStrategy _eventTypeNamingStrategy;

		public DomainEventPublisher(IMessageProducer messageProducer, IEventTypeNamingStrategy eventTypeNamingStrategy,
			ILogger<DomainEventPublisher> logger)
		{
			_messageProducer = messageProducer;
			_eventTypeNamingStrategy = eventTypeNamingStrategy;
			_logger = logger;
		}
		
		public void Publish(string aggregateType, object aggregateId, IList<IDomainEvent> domainEvents)
		{
			Publish(aggregateType, aggregateId, new Dictionary<string, string>(), domainEvents);
		}

		public void Publish(string aggregateType, object aggregateId, IDictionary<string, string> headers,
			IList<IDomainEvent> domainEvents)
		{
			var logContext = $"{nameof(Publish)}, aggregateType='{aggregateType}', aggregateId='{aggregateId}' " +
			                 $"with {headers.Count} headers and {domainEvents.Count} events";
			_logger.LogDebug($"+{logContext}");
			foreach (IDomainEvent domainEvent in domainEvents)
			{
				_messageProducer.Send(aggregateType,
					MakeMessageForDomainEvent(aggregateType, aggregateId, headers, domainEvent,
						_eventTypeNamingStrategy));
			}
			_logger.LogDebug($"-{logContext}");
		}

		public void Publish<TAggregate>(object aggregateId, IList<IDomainEvent> domainEvents)
		{
			Publish(typeof(TAggregate).FullName, aggregateId, domainEvents);
		}

		public static IMessage MakeMessageForDomainEvent(string aggregateType, object aggregateId,
			IDictionary<string, string> headers, IDomainEvent @event, IEventTypeNamingStrategy eventTypeNamingStrategy)
		{
			string aggregateIdAsString = aggregateId.ToString();
			string eventType = eventTypeNamingStrategy.GetEventTypeName(@event.GetType());
			return MessageBuilder
				.WithPayload(JsonMapper.ToJson(@event))
				.WithExtraHeaders("", headers)
				.WithHeader(MessageHeaders.PartitionId, aggregateIdAsString)
				.WithHeader(EventMessageHeaders.AggregateId, aggregateIdAsString)
				.WithHeader(EventMessageHeaders.AggregateType, aggregateType)
				.WithHeader(EventMessageHeaders.EventType, eventType)
				.Build();
		}
	}
}