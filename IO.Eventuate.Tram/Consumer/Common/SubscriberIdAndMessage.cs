/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-consumer-common
 * package:	io.eventuate.tram.consumer.common
 */

using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public class SubscriberIdAndMessage
	{
		public string SubscriberId { get; }
		
		public IMessage Message { get; }

		public SubscriberIdAndMessage(string subscriberId, IMessage message) {
			SubscriberId = subscriberId;
			Message = message;
		}
	}
}