using System;
using System.Collections.Generic;
using System.Linq;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Consumer;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class DecoratedMessageHandlerFactory
	{
		private readonly ILogger _logger;
		private readonly IList<IMessageHandlerDecorator> _decorators;

		public DecoratedMessageHandlerFactory(IEnumerable<IMessageHandlerDecorator> decorators,
			ILogger<DecoratedMessageHandlerFactory> logger)
		{
			_decorators = decorators.OrderBy(d => d is IOrdered ordered ? ordered.Order : Int32.MaxValue).ToList();
			_logger = logger;
		}

		public Action<SubscriberIdAndMessage, IServiceProvider> Decorate(MessageHandler mh) {
			MessageHandlerDecoratorChainBuilder builder = MessageHandlerDecoratorChainBuilder.StartingWith(_decorators[0]);

			foreach (IMessageHandlerDecorator mhd in _decorators.Skip(1))
			{
				builder = builder.AndThen(mhd);
			}

			IMessageHandlerDecoratorChain chain = builder.AndFinally((smh, serviceProvider) => {
				String subscriberId = smh.SubscriberId;
				IMessage message = smh.Message;
				try {
					_logger.LogTrace($"Invoking handler {subscriberId} {message.Id}");
					mh(smh.Message, serviceProvider);
					_logger.LogTrace($"handled message {subscriberId} {message.Id}");
				} catch (Exception e) {
					_logger.LogTrace($"Got exception {subscriberId} {message.Id}: {e}");
					throw;
				}
			});
			return (subscriberIdAndMessage, serviceProvider) => chain.InvokeNext(subscriberIdAndMessage, serviceProvider);
		}
	}
}