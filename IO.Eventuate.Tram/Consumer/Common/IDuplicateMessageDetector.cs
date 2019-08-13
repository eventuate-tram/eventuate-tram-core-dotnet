/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-consumer-common
 * package:	io.eventuate.tram.consumer.common
 */

using System;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public interface IDuplicateMessageDetector
	{
		bool IsDuplicate(string consumerId, string messageId);
		
		void DoWithMessage(SubscriberIdAndMessage subscriberIdAndMessage, Action callback);
	}
}