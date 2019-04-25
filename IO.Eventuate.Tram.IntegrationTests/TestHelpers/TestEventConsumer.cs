using System;
using System.Collections.Generic;
using IO.Eventuate.Tram.Events.Subscriber;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    public class TestEventConsumer
    {
        private readonly ILogger<TestEventConsumer> _logger;

        public int Type1MessageCount { get; set; }
        public int Type2MessageCount { get; set; }
        public DateTime Type1FirstMessage { get; set; }
        public DateTime Type2FirstMessage { get; set; }
        public DateTime Type1LastMessage { get; set; }
        public DateTime Type2LastMessage { get; set; }
        public List<IDomainEventEnvelope<TestMessageType1>> ReceivedType1Messages { get; set; }
        public List<IDomainEventEnvelope<TestMessageType2>> ReceivedType2Messages { get; set; }
        public int ExceptionCount = 0;

        public TestEventConsumer(ILogger<TestEventConsumer> logger)
        {
            _logger = logger;
        }

        public void Reset()
        {
            Type1MessageCount = 0;
            Type2MessageCount = 0;
            Type1FirstMessage = DateTime.MaxValue;
            Type2FirstMessage = DateTime.MaxValue;
            Type1LastMessage = DateTime.MinValue;
            Type2LastMessage = DateTime.MinValue;
            ReceivedType1Messages = new List<IDomainEventEnvelope<TestMessageType1>>();
            ReceivedType2Messages = new List<IDomainEventEnvelope<TestMessageType2>>();
            ExceptionCount = 0;
        }

        public void DontSaveMessages()
        {
            ReceivedType1Messages = null;
            ReceivedType2Messages = null;
        }

        public int TotalMessageCount()
        {
            return Type1MessageCount + Type2MessageCount;
        }
        public TimeSpan Type1Duration()
        {
            return Type1LastMessage > Type1FirstMessage ? Type1LastMessage - Type1FirstMessage : TimeSpan.Zero;
        }
        public TimeSpan Type2Duration()
        {
            return Type2LastMessage > Type2FirstMessage ? Type2LastMessage - Type2FirstMessage : TimeSpan.Zero;
        }
        public TimeSpan TotalDuration()
        {
            return Type1Duration() + Type2Duration();
        }

        public DomainEventHandlers DomainEventHandlers(String aggregateType)
        {
            return DomainEventHandlersBuilder.ForAggregateType(aggregateType)
                .OnEvent<TestMessageType1>(HandleMessageType1Event)
                .OnEvent<TestMessageType2>(HandleMessageType2Event)
                .Build();
        }

        private void HandleMessageType1Event(IDomainEventEnvelope<TestMessageType1> @event)
        {
            _logger.LogDebug("Got MessageType1Event with id={} and value={}", @event.EventId,
                @event.Event.ToString());
            DateTime receivedTime = DateTime.Now;
            if (receivedTime < Type1FirstMessage)
                Type1FirstMessage = receivedTime;
            if (receivedTime > Type1LastMessage)
                Type1LastMessage = receivedTime;
            Type1MessageCount++;
            ReceivedType1Messages?.Add(@event);

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
            DateTime receivedTime = DateTime.Now;
            if (receivedTime < Type2FirstMessage)
                Type2FirstMessage = receivedTime;
            if (receivedTime > Type2LastMessage)
                Type2LastMessage = receivedTime;
            Type2MessageCount++;
            ReceivedType2Messages?.Add(@event);
        }
    }
}
