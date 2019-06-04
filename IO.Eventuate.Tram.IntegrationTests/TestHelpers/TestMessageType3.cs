using System;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Messaging.Common;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    [EventType(EventTypeName)]
    public class TestMessageType3 : TestMessageType2
    {
        public new const string EventTypeName = "testing.TestMessageType3";
        
        public TestMessageType3(String name, int value)
		: base(name, value)
        {
        }

        public void AssertGoodMessageReceived(IDomainEventEnvelope<TestMessageType3> receivedMessage)
        {
            Assert.True(receivedMessage.Message.HasHeader(MessageHeaders.Id), "Message ID is in the header");
            Assert.AreEqual(Name, receivedMessage.Event.Name, "Message Name is the same");
            Assert.AreEqual(Value, receivedMessage.Event.Value, "Message Value is the same");
        }
    }
}
