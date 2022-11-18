using System;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Consumer.Kafka;

internal class QueuedMessage
{
	internal QueuedMessage(IMessage message, Action<IMessage> messageConsumer)
	{
		Message = message;
		MessageConsumer = messageConsumer;
	}
			
	internal IMessage Message { get; }

	internal Action<IMessage> MessageConsumer { get; }
}