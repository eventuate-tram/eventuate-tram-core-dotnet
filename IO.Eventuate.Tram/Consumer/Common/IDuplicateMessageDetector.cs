/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-consumer-common
 * package:	io.eventuate.tram.consumer.common
 */

using System;
using System.Threading.Tasks;

namespace IO.Eventuate.Tram.Consumer.Common
{
	public interface IDuplicateMessageDetector
	{
		Task<bool> IsDuplicateAsync(string consumerId, string messageId);
		
		Task DoWithMessageAsync(SubscriberIdAndMessage subscriberIdAndMessage, Func<Task> callback);
	}
}