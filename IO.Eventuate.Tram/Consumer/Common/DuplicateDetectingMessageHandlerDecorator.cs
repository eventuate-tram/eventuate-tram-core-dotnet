using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class DuplicateDetectingMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
	{
		public DuplicateDetectingMessageHandlerDecorator()
		{
		}

		public Func<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain, CancellationToken, Task> Accept =>
			async (subscriberIdAndMessage, serviceProvider, messageHandlerDecoratorChain, cancellationToken) =>
			{
				var duplicateMessageDetector =
					serviceProvider.GetRequiredService<IDuplicateMessageDetector>();
				await duplicateMessageDetector.DoWithMessageAsync(subscriberIdAndMessage,
					async () => await messageHandlerDecoratorChain.InvokeNextAsync(subscriberIdAndMessage, serviceProvider, cancellationToken));
			};
		
		public int Order => BuiltInMessageHandlerDecoratorOrder.DuplicateDetectingMessageHandlerDecorator;
	}
}