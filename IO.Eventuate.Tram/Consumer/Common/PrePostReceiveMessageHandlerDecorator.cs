using System;
using System.Collections.Generic;
using System.Linq;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class PrePostReceiveMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
	{

		private readonly IMessageInterceptor[] _messageInterceptors;

		public PrePostReceiveMessageHandlerDecorator(IEnumerable<IMessageInterceptor> messageInterceptors)
		{
			_messageInterceptors = messageInterceptors.ToArray();
		}

		public Action<SubscriberIdAndMessage, IMessageHandlerDecoratorChain> Accept =>
			(subscriberIdAndMessage, messageHandlerDecoratorChain) =>
			{
				IMessage message = subscriberIdAndMessage.Message;
				PreReceive(message);
				try
				{
					messageHandlerDecoratorChain.InvokeNext(subscriberIdAndMessage);
				}
				finally
				{
					PostReceive(message);
				}
			};

		private void PreReceive(IMessage message)
		{
			foreach (IMessageInterceptor messageInterceptor in _messageInterceptors)
			{
				messageInterceptor.PreReceive(message);
			}
		}


		private void PostReceive(IMessage message)
		{
			foreach (IMessageInterceptor messageInterceptor in _messageInterceptors)
			{
				messageInterceptor.PostReceive(message);
			}
		}

		public int Order => BuiltInMessageHandlerDecoratorOrder.PrePostReceiveMessageHandlerDecorator;
	}
}