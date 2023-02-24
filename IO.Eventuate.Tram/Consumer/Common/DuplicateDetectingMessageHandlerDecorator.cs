using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class DuplicateDetectingMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
	{
		public DuplicateDetectingMessageHandlerDecorator()
		{
		}

		public Func<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain, Task> Accept =>
			async (subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain) =>
			{
				var duplicateMessageDetector =
					serviceProvider.GetRequiredService<IDuplicateMessageDetector>();
				await duplicateMessageDetector.DoWithMessageAsync(subscriberIdAndMessage,
					async () => await messageHandlerDecoratorChain.InvokeNextAsync(subscriberIdAndMessage, serviceProvider));
			};
		
		public int Order => BuiltInMessageHandlerDecoratorOrder.DuplicateDetectingMessageHandlerDecorator;
	}
}