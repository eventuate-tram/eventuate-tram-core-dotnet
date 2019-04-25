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
            this.Name = name;
            this.Value = value;
            this.Number = number;
        }

        public void AssertGoodMessageReceived(IDomainEventEnvelope<TestMessageType1> receivedMessage)
        {
            Assert.True(receivedMessage.Message.HasHeader(MessageHeaders.Id), "Message ID is in the header");
            Assert.AreEqual(Name, receivedMessage.Event.Name, "Message Name is the same");
            Assert.AreEqual(Value, receivedMessage.Event.Value, "Message Value is the same");
            Assert.AreEqual(Number, receivedMessage.Event.Number, "Message Number is the same");
        }
    }
}
