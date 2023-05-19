/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-consumer-common
 * package:	io.eventuate.tram.consumer.common
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class MessageHandlerDecoratorChainBuilder
	{
		private readonly LinkedList<IMessageHandlerDecorator> _handlers = new LinkedList<IMessageHandlerDecorator>();

		public static MessageHandlerDecoratorChainBuilder StartingWith(IMessageHandlerDecorator smh)
		{
			var b = new MessageHandlerDecoratorChainBuilder();
			b.Add(smh);
			return b;
		}

		private void Add(IMessageHandlerDecorator smh)
		{
			_handlers.AddLast(smh);
		}

		public MessageHandlerDecoratorChainBuilder AndThen(IMessageHandlerDecorator smh)
		{
			Add(smh);
			return this;
		}

		public IMessageHandlerDecoratorChain AndFinally(Func<SubscriberIdAndMessage, IServiceProvider, CancellationToken, Task> consumer)
		{
			return BuildChain(_handlers.First, consumer);
		}

		private static IMessageHandlerDecoratorChain BuildChain(LinkedListNode<IMessageHandlerDecorator> handlersHead,
			Func<SubscriberIdAndMessage, IServiceProvider, CancellationToken, Task> consumer)
		{
			if (handlersHead == null)
			{
				return new MessageHandlerDecoratorChain(consumer);
			}
			else
			{
				LinkedListNode<IMessageHandlerDecorator> tail = handlersHead.Next;
				return new MessageHandlerDecoratorChain((subscriberIdAndMessage, serviceProvider, cancellationToken) =>
					handlersHead.Value.Accept(subscriberIdAndMessage, serviceProvider, BuildChain(tail, consumer), cancellationToken));
			}
		}

		private class MessageHandlerDecoratorChain : IMessageHandlerDecoratorChain
		{
			private readonly Func<SubscriberIdAndMessage, IServiceProvider, CancellationToken, Task> _action;

			public MessageHandlerDecoratorChain(Func<SubscriberIdAndMessage, IServiceProvider, CancellationToken, Task> action)
			{
				_action = action;
			}
			
			public async Task InvokeNextAsync(SubscriberIdAndMessage subscriberIdAndMessage, IServiceProvider serviceProvider, CancellationToken cancellationToken)
			{
				await _action(subscriberIdAndMessage, serviceProvider, cancellationToken);
			}
		}
	}
}