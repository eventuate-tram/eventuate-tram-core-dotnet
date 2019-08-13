/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-consumer-common
 * package:	io.eventuate.tram.consumer.common
 */

namespace IO.Eventuate.Tram.Consumer.Common
{
	public static class BuiltInMessageHandlerDecoratorOrder
	{
		public static int PrePostReceiveMessageHandlerDecorator = 100;
		public static int DuplicateDetectingMessageHandlerDecorator = 200;
		public static int PrePostHandlerMessageHandlerDecorator = 300;
	}
}