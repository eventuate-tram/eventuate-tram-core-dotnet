/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.consumer
 */

using System;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Messaging.Consumer
{
	public delegate void MessageHandler(IMessage message, IServiceProvider serviceProvider);
}