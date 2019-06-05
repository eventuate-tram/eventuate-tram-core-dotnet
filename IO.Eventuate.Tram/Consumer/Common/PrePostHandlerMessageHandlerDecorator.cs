using System;
using System.Collections.Generic;
using System.Linq;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class PrePostHandlerMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
	{
		private readonly IMessageInterceptor[] _messageInterceptors;

		public PrePostHandlerMessageHandlerDecorator(IEnumerable<IMessageInterceptor> messageInterceptors)
		{
			_messageInterceptors = messageInterceptors.ToArray();
		}

		public Action<SubscriberIdAndMessage, IMessageHandlerDecoratorChain> Accept =>
			(subscriberIdAndMessage, messageHandlerDecoratorChain) =>
			{
				IMessage message = subscriberIdAndMessage.Message;
				string subscriberId = subscriberIdAndMessage.SubscriberId;
				PreHandle(subscriberId, message);
				try
				{
					messageHandlerDecoratorChain.InvokeNext(subscriberIdAndMessage);
					PostHandle(subscriberId, message, null);
				}
				catch (Exception e)
				{
					PostHandle(subscriberId, message, e);
				}
			};

		private void PreHandle(string subscriberId, IMessage message)
		{
			foreach (IMessageInterceptor messageInterceptor in _messageInterceptors)
			{
				messageInterceptor.PreHandle(subscriberId, message);
			}
		}


		private void PostHandle(string subscriberId, IMessage message, Exception e)
		{
			foreach (IMessageInterceptor messageInterceptor in _messageInterceptors)
			{
				messageInterceptor.PostHandle(subscriberId, message, e);
			}
		}

		public int Order => BuiltInMessageHandlerDecoratorOrder.PrePostHandlerMessageHandlerDecorator;
	}
}