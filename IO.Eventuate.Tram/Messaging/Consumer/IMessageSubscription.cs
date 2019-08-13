/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.consumer
 */

namespace IO.Eventuate.Tram.Messaging.Consumer
{
	/// <summary>
	/// Message subscription handle to allow subscriber to unsubscribe
	/// </summary>
	public interface IMessageSubscription
	{
		/// <summary>
		/// Unsubscribe from the subscription
		/// </summary>
		void Unsubscribe();
	}
}