using System;
using System.Threading.Tasks;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public interface IMessageHandlerDecoratorChain
	{
		Task InvokeNextAsync(SubscriberIdAndMessage subscriberIdAndMessage, IServiceProvider serviceProvider);
	}
}