using System;
using System.Collections.Generic;
using System.Linq;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Subscriber;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    public class TestEventConsumer
    {
        private readonly ILogger<TestEventConsumer> _logger;

		/// <summary>
		/// Hold the statistics for a given message type.
		/// </summary>
        public class EventStatistics
        {
            public int MessageCount { get; set; }
            public DateTime FirstMessageTime { get; set; }
            public DateTime LastMessageTime { get; set; }
            public List<IDomainEventEnvelope<IDomainEvent>> ReceivedMessages { get; set; }
            public TimeSpan GetDuration()
            {
                return LastMessageTime > FirstMessageTime
                    ? LastMessageTime - FirstMessageTime
                    : TimeSpan.Zero;
            }

        }

        private readonly Dictionary<Type, EventStatistics> _statisticsForEvent;
        public int ExceptionCount;

        public TestEventConsumer(ILogger<TestEventConsumer> logger)
        {
            _logger = logger;
            _statisticsForEvent = new Dictionary<Type, EventStatistics>
            {
                {typeof(TestMessageType1), new EventStatistics()},
                {typeof(TestMessageType2), new EventStatistics()},
                {typeof(TestMessageType3), new EventStatistics()},
                {typeof(TestMessageType4), new EventStatistics()}
            };
        }

        public EventStatistics GetEventStatistics(Type eventType)
        {
            return _statisticsForEvent[eventType];
        }

        public IEnumerable<Type> GetEventTypes()
        {
            return _statisticsForEvent.Keys;
        }

        public void Reset()
        {
            foreach (EventStatistics eventStatistics in _statisticsForEvent.Values)
            {
                eventStatistics.MessageCount = 0;
                eventStatistics.FirstMessageTime = DateTime.MaxValue;
                eventStatistics.LastMessageTime = DateTime.MinValue;
                eventStatistics.ReceivedMessages = new List<IDomainEventEnvelope<IDomainEvent>>();
            }
            ExceptionCount = 0;
        }

        public int TotalMessageCount()
        {
            return _statisticsForEvent.Aggregate(0, (total, stat) =>
                total + stat.Value.MessageCount);
        }

        public DomainEventHandlers DomainEventHandlers(String aggregateType12,
            String aggregateType34)
        {
            return DomainEventHandlersBuilder.ForAggregateType(aggregateType12)
                .OnEvent<TestMessageType1>(HandleMessageType1Event)
                .OnEvent<TestMessageType2>(HandleMessageType2Event)
                .AndForAggregateType(aggregateType34)
                .OnEvent<TestMessageType3>(HandleMessageType3Event)
                .OnEvent<TestMessageType4,TestMessage4Handler>()
                .Build();
        }

        private void HandleMessageType1Event(IDomainEventEnvelope<TestMessageType1> @event)
        {
            _logger.LogDebug("Got MessageType1Event with id={} and value={}", @event.EventId,
                @event.Event.ToString());
            EventStatistics eventStatistics = GetEventStatistics(typeof(TestMessageType1));
            HandleTestMessageEvent(@event, eventStatistics);
            if (@event.Event.Name.Equals("ThrowException") && ExceptionCount < 5)
            {
                ExceptionCount++;
                throw (new Exception());
            }
        }

        private void HandleMessageType2Event(IDomainEventEnvelope<TestMessageType2> @event)
        {
            _logger.LogDebug("Got message MessageType2Event with id={} and value={}", @event.EventId,
                @event.Event.ToString());
            EventStatistics eventStatistics = GetEventStatistics(typeof(TestMessageType2));
            HandleTestMessageEvent(@event, eventStatistics);
        }

        private void HandleMessageType3Event(IDomainEventEnvelope<TestMessageType3> @event,
            IServiceProvider serviceProvider)
        {
            _logger.LogDebug("Got message MessageType3Event with id={} and value={}", @event.EventId,
                @event.Event.ToString());
            EventStatistics eventStatistics = GetEventStatistics(typeof(TestMessageType3));
            HandleTestMessageEvent(@event, eventStatistics);
        }

        public void HandleTestMessageEvent(IDomainEventEnvelope<IDomainEvent> @event, EventStatistics eventStatistics)
        {
            DateTime receivedTime = DateTime.Now;
            if (receivedTime < eventStatistics.FirstMessageTime)
                eventStatistics.FirstMessageTime = receivedTime;
            if (receivedTime > eventStatistics.LastMessageTime)
                eventStatistics.LastMessageTime = receivedTime;
            eventStatistics.MessageCount++;
            eventStatistics.ReceivedMessages.Add(@event);
        }

    }
}
