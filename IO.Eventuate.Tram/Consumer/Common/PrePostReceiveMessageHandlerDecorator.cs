using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class PrePostReceiveMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
	{
		public Func<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain, Task> Accept =>
			async (subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain) =>
			{
				IMessage message = subscriberIdAndMessage.Message;
				IMessageInterceptor[] messageInterceptors =
					serviceProvider.GetServices<IMessageInterceptor>().ToArray();
				await PreReceiveAsync(message, messageInterceptors);
				try
				{
					await messageHandlerDecoratorChain.InvokeNextAsync(subscriberIdAndMessage, serviceProvider);
				}
				finally
				{
					await PostReceiveAsync(message, messageInterceptors);
				}
			};

		private async Task PreReceiveAsync(IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
		{
			foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
			{
				await messageInterceptor.PreReceiveAsync(message);
			}
		}


		private async Task PostReceiveAsync(IMessage message, IEnumerable<IMessageInterceptor> messageInterceptors)
		{
			foreach (IMessageInterceptor messageInterceptor in messageInterceptors)
			{
				await messageInterceptor.PostReceiveAsync(message);
			}
		}

		public int Order => BuiltInMessageHandlerDecoratorOrder.PrePostReceiveMessageHandlerDecorator;
	}
}