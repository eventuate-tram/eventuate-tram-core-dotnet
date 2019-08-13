using System;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Messaging.Common;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    public class TestMessageType1 : IDomainEvent
    {
        public String Name { get; set; }
        public int Value { get; set; }
        public double Number { get; set; }

        public TestMessageType1(String name, int value, double number)
        {
            Name = name;
            Value = value;
            Number = number;
        }

        public void AssertGoodMessageReceived(IDomainEventEnvelope<IDomainEvent> receivedMessage)
        {
            Assert.True(receivedMessage.Message.HasHeader(MessageHeaders.Id), "Message ID is in the header");
            TestMessageType1 @event = (TestMessageType1)receivedMessage.Event;
            Assert.AreEqual(Name, @event.Name, "Message Name is the same");
            Assert.AreEqual(Value, @event.Value, "Message Value is the same");
            Assert.AreEqual(Number, @event.Number, "Message Number is the same");
        }
    }
}
