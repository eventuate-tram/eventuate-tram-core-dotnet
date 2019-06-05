using System;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class DuplicateDetectingMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
	{
		private readonly IServiceScopeFactory _serviceScopeFactory;

		public DuplicateDetectingMessageHandlerDecorator(IServiceScopeFactory serviceScopeFactory)
		{
			_serviceScopeFactory = serviceScopeFactory;
		}

		public Action<SubscriberIdAndMessage, IMessageHandlerDecoratorChain> Accept =>
			(subscriberIdAndMessage, messageHandlerDecoratorChain) =>
			{
				// Creating a service scope to get a duplicate message detector so that
				// the implementation can resolve a DbContext (which is registered as a service with a scoped lifetime)
				using (IServiceScope scope = _serviceScopeFactory.CreateScope())
				{
					var duplicateMessageDetector =
						scope.ServiceProvider.GetRequiredService<IDuplicateMessageDetector>();
					duplicateMessageDetector.DoWithMessage(subscriberIdAndMessage,
						() => messageHandlerDecoratorChain.InvokeNext(subscriberIdAndMessage));
				}
			};
		
		public int Order => BuiltInMessageHandlerDecoratorOrder.DuplicateDetectingMessageHandlerDecorator;
	}
}