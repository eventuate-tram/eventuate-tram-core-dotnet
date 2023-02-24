using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.IntegrationTests.TestFixtures;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    public class TestEventConsumer
    {
        private readonly ILogger<TestEventConsumer> _logger;

        public static int MessageType3ProcessingDelay = 5000;

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
                {typeof(TestMessageType4), new EventStatistics()},
                {typeof(TestMessageTypeDelay), new EventStatistics()}
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
            String aggregateType34, String aggregateTypeDelay)
        {
            return DomainEventHandlersBuilder.ForAggregateType(aggregateType12)
                .OnEvent<TestMessageType1>(HandleMessageType1EventAsync)
                .OnEvent<TestMessageType2>(HandleMessageType2EventAsync)
                .AndForAggregateType(aggregateType34)
                .OnEvent<TestMessageType3>(HandleMessageType3EventAsync)
                .OnEvent<TestMessageType4,TestMessage4Handler>()
                .AndForAggregateType(aggregateTypeDelay)
                .OnEvent<TestMessageTypeDelay>(HandleMessageTypeDelayEventAsync)
                .Build();
        }

        private Task HandleMessageType1EventAsync(IDomainEventEnvelope<TestMessageType1> @event)
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

            return Task.CompletedTask;
        }

        private Task HandleMessageType2EventAsync(IDomainEventEnvelope<TestMessageType2> @event)
        {
            _logger.LogDebug("Got message MessageType2Event with id={} and value={}", @event.EventId,
                @event.Event.ToString());
            EventStatistics eventStatistics = GetEventStatistics(typeof(TestMessageType2));
            HandleTestMessageEvent(@event, eventStatistics);
            return Task.CompletedTask;
        }

        private Task HandleMessageType3EventAsync(IDomainEventEnvelope<TestMessageType3> @event,
            IServiceProvider serviceProvider)
        {
            _logger.LogDebug("Got message MessageType3Event with id={} and value={}", @event.EventId,
                @event.Event.ToString());
            EventStatistics eventStatistics = GetEventStatistics(typeof(TestMessageType3));
            HandleTestMessageEvent(@event, eventStatistics);
            return Task.CompletedTask;
        }
        
        private Task HandleMessageTypeDelayEventAsync(IDomainEventEnvelope<TestMessageTypeDelay> @event,
            IServiceProvider serviceProvider)
        {
            _logger.LogDebug("Got message TestMessageTypeDelay with id={} and value={}", @event.EventId,
                @event.Event.ToString());
            File.AppendAllText(IntegrationTestsBase.PingFileName, $"Received event '{@event.Message.Payload}' with value '{@event.Event.Value}'\n");
            EventStatistics eventStatistics = GetEventStatistics(typeof(TestMessageTypeDelay));
            HandleTestMessageEvent(@event, eventStatistics);
            Thread.Sleep(MessageType3ProcessingDelay);
            File.AppendAllText(IntegrationTestsBase.PingFileName, $"Processed event '{@event.Message.Payload}' with value '{@event.Event.Value}'\n");
            return Task.CompletedTask;
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
