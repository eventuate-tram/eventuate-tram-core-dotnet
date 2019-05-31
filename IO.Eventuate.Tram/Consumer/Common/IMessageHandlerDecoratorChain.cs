using System;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public interface IMessageHandlerDecoratorChain
	{
		void InvokeNext(SubscriberIdAndMessage subscriberIdAndMessage, IServiceProvider serviceProvider);
	}
}