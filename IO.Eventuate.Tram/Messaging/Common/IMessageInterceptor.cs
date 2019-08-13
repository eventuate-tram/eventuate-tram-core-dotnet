/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.common
 */

using System;

namespace IO.Eventuate.Tram.Messaging.Common
{
	public interface IMessageInterceptor
	{
		void PreSend(IMessage message);

		void PostSend(IMessage message, Exception e);

		void PreReceive(IMessage message);

		void PreHandle(string subscriberId, IMessage message);

		void PostHandle(string subscriberId, IMessage message, Exception e);

		void PostReceive(IMessage message);
	}
}