/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-events
 * package:	io.eventuate.tram.events.common
 */

namespace IO.Eventuate.Tram.Events.Common
{
	public static class EventMessageHeaders
	{
		public const string EventType = "event-type";
		public const string AggregateType = "event-aggregate-type";
		public const string AggregateId = "event-aggregate-id";		
	}
}