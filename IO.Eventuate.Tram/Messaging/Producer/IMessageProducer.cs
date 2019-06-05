/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.producer
 */

using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Messaging.Producer
{
	/// <summary>
	/// Supports sending basic messages
	/// </summary>
	public interface IMessageProducer
	{
		/// <summary>
		/// Send a message
		/// </summary>
		/// <param name="destination">The destination channel</param>
		/// <param name="message">The message to send</param>
		void Send(string destination, IMessage message);
	}
}