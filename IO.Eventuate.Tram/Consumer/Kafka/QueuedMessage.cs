using System;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Consumer.Kafka;

internal class QueuedMessage
{
	public QueuedMessage(IMessage message, Action<IMessage> messageConsumer)
	{
		Message = message;
		MessageConsumer = messageConsumer;
	}
			
	public IMessage Message { get; }

	public Action<IMessage> MessageConsumer { get; }
}