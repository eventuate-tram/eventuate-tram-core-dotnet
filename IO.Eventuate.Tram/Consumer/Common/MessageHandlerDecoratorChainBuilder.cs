/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-consumer-common
 * package:	io.eventuate.tram.consumer.common
 */

using System;
using System.Collections.Generic;

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

		public IMessageHandlerDecoratorChain AndFinally(Action<SubscriberIdAndMessage> consumer)
		{
			return BuildChain(_handlers.First, consumer);
		}

		private static IMessageHandlerDecoratorChain BuildChain(LinkedListNode<IMessageHandlerDecorator> handlersHead,
			Action<SubscriberIdAndMessage> consumer)
		{
			if (handlersHead == null)
			{
				return new MessageHandlerDecoratorChain(consumer);
			}
			else
			{
				LinkedListNode<IMessageHandlerDecorator> tail = handlersHead.Next;
				return new MessageHandlerDecoratorChain(subscriberIdAndMessage =>
					handlersHead.Value.Accept(subscriberIdAndMessage, BuildChain(tail, consumer)));
			}
		}

		private class MessageHandlerDecoratorChain : IMessageHandlerDecoratorChain
		{
			private readonly Action<SubscriberIdAndMessage> _action;

			public MessageHandlerDecoratorChain(Action<SubscriberIdAndMessage> action)
			{
				_action = action;
			}
			
			public void InvokeNext(SubscriberIdAndMessage subscriberIdAndMessage)
			{
				_action(subscriberIdAndMessage);
			}
		}
	}
}