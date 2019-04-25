using System.Collections.Generic;

namespace IO.Eventuate.Tram.Messaging.Consumer
{
	/// <summary>
	/// Supports basic message consumption
	/// </summary>
	public interface IMessageConsumer
	{
		/// <summary>
		/// Subscribe to and register a message handler for messages published to the specified set of channels.
		/// </summary>
		/// <param name="subscriberId">The subscriber ID to use for this subscription. Multiple subscriptions
		/// using the same subscriber ID will result in a particular message being sent to only one of the subscribers.</param>
		/// <param name="channels">The set of channels to subscribe to.</param>
		/// <param name="handler">A message handler method to call when a message is received.</param>
		void Subscribe(string subscriberId, ISet<string> channels, MessageHandler handler);
	}
}