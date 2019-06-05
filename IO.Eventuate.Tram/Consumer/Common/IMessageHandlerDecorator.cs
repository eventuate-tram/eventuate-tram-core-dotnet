using System;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public interface IMessageHandlerDecorator
	{
		Action<SubscriberIdAndMessage, IMessageHandlerDecoratorChain> Accept { get; }
	}
}