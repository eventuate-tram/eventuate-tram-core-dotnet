using System;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public interface IMessageHandlerDecorator
	{
		Action<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain> Accept { get; }
	}
}