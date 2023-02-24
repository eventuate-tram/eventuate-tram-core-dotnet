using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class PrePostHandlerMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
	{
		public Func<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain, Task> Accept =>
			async (subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain) =>
			{
				IMessage message = subscriberIdAndMessage.Message;
				string subscriberId = subscriberIdAndMessage.SubscriberId;
				IMessageInterceptor[] messageInterceptors =
					serviceProvider.GetServices<IMessageInterceptor>().ToArray();
				await PreHandleAsync(subscriberId, message, messageInterceptors);
				try
				{
					await messageHandlerDecoratorChain.InvokeNextAsync(subscriberIdAndMessage, serviceProvider);
					await PostHandleAsync(subscriberId, message, messageInterceptors, null);
				}
				catch (Exception e)
				{
					await PostHandleAsync(subscriberId, message, messageInterceptors, e);
					throw;
				}
			};

		private async Task PreHandleAsync(string subscriberId, IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
		{
			foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
			{
				await messageInterceptor.PreHandleAsync(subscriberId, message);
			}
		}


		private async Task PostHandleAsync(string subscriberId, IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors, Exception e)
		{
			foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
			{
				await messageInterceptor.PostHandleAsync(subscriberId, message, e);
			}
		}

		public int Order => BuiltInMessageHandlerDecoratorOrder.PrePostHandlerMessageHandlerDecorator;
	}
}