using System;
using IO.Eventuate.Tram.Events.Subscriber;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
	public class TestMessage4Handler : IDomainEventHandler<TestMessageType4>
	{
		public TestMessage4Handler(TestEventConsumer testEventConsumer)
		{
			TestEventConsumer = testEventConsumer;
		}

		public TestEventConsumer TestEventConsumer { get; }

		public void Handle(IDomainEventEnvelope<TestMessageType4> @event)
		{
			DateTime receivedTime = DateTime.Now;
			if (receivedTime < TestEventConsumer.Type4FirstMessage)
				TestEventConsumer.Type4FirstMessage = receivedTime;
			if (receivedTime > TestEventConsumer.Type4LastMessage)
				TestEventConsumer.Type4LastMessage = receivedTime;
			TestEventConsumer.Type4MessageCount++;
			TestEventConsumer.ReceivedType4Messages?.Add(@event);
		}
	}

}
