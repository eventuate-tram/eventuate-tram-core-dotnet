using System;
using System.Threading;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Consumer.Kafka;

/// <summary>
/// Wraps a <see cref="Message"/> queued by a <see cref="SwimlaneDispatcher"/> to be handled by the specified <see cref="MessageConsumerAsync"/> delegate.
/// </summary>
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