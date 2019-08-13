using System;
using System.Collections.Generic;
using System.Linq;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class PrePostHandlerMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
	{
		public Action<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain> Accept =>
			(subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain) =>
			{
				IMessage message = subscriberIdAndMessage.Message;
				string subscriberId = subscriberIdAndMessage.SubscriberId;
				IMessageInterceptor[] messageInterceptors =
					serviceProvider.GetServices<IMessageInterceptor>().ToArray();
				PreHandle(subscriberId, message, messageInterceptors);
				try
				{
					messageHandlerDecoratorChain.InvokeNext(subscriberIdAndMessage, serviceProvider);
					PostHandle(subscriberId, message, messageInterceptors, null);
				}
				catch (Exception e)
				{
					PostHandle(subscriberId, message, messageInterceptors, e);
					throw;
				}
			};

		private void PreHandle(string subscriberId, IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
		{
			foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
			{
				messageInterceptor.PreHandle(subscriberId, message);
			}
		}


		private void PostHandle(string subscriberId, IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors, Exception e)
		{
			foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
			{
				messageInterceptor.PostHandle(subscriberId, message, e);
			}
		}

		public int Order => BuiltInMessageHandlerDecoratorOrder.PrePostHandlerMessageHandlerDecorator;
	}
}