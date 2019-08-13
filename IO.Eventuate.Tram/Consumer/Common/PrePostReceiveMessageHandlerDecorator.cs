using System;
using System.Collections.Generic;
using System.Linq;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class PrePostReceiveMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
	{
		public Action<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain> Accept =>
			(subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain) =>
			{
				IMessage message = subscriberIdAndMessage.Message;
				IMessageInterceptor[] messageInterceptors =
					serviceProvider.GetServices<IMessageInterceptor>().ToArray();
				PreReceive(message, messageInterceptors);
				try
				{
					messageHandlerDecoratorChain.InvokeNext(subscriberIdAndMessage, serviceProvider);
				}
				finally
				{
					PostReceive(message, messageInterceptors);
				}
			};

		private void PreReceive(IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
		{
			foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
			{
				messageInterceptor.PreReceive(message);
			}
		}


		private void PostReceive(IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
		{
			foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
			{
				messageInterceptor.PostReceive(message);
			}
		}

		public int Order => BuiltInMessageHandlerDecoratorOrder.PrePostReceiveMessageHandlerDecorator;
	}
}