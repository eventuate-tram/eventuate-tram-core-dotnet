using System;
using System.Threading;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Consumer.Kafka;

internal class QueuedMessage
{
	public QueuedMessage(IMessage message, Func<IMessage, CancellationToken, Task> messageConsumerAsync)
	{
		Message = message;
		MessageConsumerAsync = messageConsumerAsync;
	}
			
	public IMessage Message { get; }

	public Func<IMessage, CancellationToken, Task> MessageConsumerAsync { get; }
}