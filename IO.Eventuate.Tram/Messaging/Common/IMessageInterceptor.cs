/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.common
 */

using System;
using System.Threading.Tasks;

namespace IO.Eventuate.Tram.Messaging.Common
{
	public interface IMessageInterceptor
	{
		void PreSend(IMessage message);

		Task PreSendAsync(IMessage message);
		
		void PostSend(IMessage message, Exception e);

		Task PostSendAsync(IMessage message, Exception e);

		Task PreReceiveAsync(IMessage message);

		Task PreHandleAsync(string subscriberId, IMessage message);

		Task PostHandleAsync(string subscriberId, IMessage message, Exception e);

		Task PostReceiveAsync(IMessage message);
	}
}