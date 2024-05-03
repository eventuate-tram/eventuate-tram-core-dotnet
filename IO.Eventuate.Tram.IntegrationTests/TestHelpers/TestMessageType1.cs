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
			Assert.That(receivedMessage.Message.HasHeader(MessageHeaders.Id), "Message ID is in the header");
			TestMessageType1 @event = (TestMessageType1)receivedMessage.Event;
			Assert.That(@event.Name, Is.EqualTo(Name), "Message Name is the same");
			Assert.That(@event.Value, Is.EqualTo(Value), "Message Value is the same");
			Assert.That(@event.Number, Is.EqualTo(Number), "Message Number is the same");
		}
	}
}