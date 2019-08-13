using System;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Messaging.Common;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    [EventType(EventTypeName)]
    public class TestMessageType2 : IDomainEvent
    {
        public const string EventTypeName = "testing.TestMessageType2";
        
        public String Name { get; set; }
        public int Value { get; set; }
        public TestMessageType2(String name, int value)
        {
            Name = name;
            Value = value;
        }

        public void AssertGoodMessageReceived(IDomainEventEnvelope<IDomainEvent> receivedMessage)
        {
            Assert.True(receivedMessage.Message.HasHeader(MessageHeaders.Id), "Message ID is in the header");
            TestMessageType2 @event = (TestMessageType2) receivedMessage.Event;
            Assert.AreEqual(Name, @event.Name, "Message Name is the same");
            Assert.AreEqual(Value, @event.Value, "Message Value is the same");
        }
    }
}
