using System;
using System.Threading.Tasks;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public interface IMessageHandlerDecorator
	{
		Func<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain, Task> Accept { get; }
	}
}