using System;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public interface IMessageHandlerDecorator
	{
		Func<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain, CancellationToken, Task> Accept { get; }
	}
}