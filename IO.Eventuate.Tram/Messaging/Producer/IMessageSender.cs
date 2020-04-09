/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.producer
 */

using System.Threading.Tasks;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Messaging.Producer
{
	public interface IMessageSender
	{
		void Send(IMessage message);

		Task SendAsync(IMessage message);
	}
}