using System;

namespace IO.Eventuate.Tram.Events.Common
{
	/// <summary>
	/// Strategy for determining the event type name to use in the message header for a particular type of domain event
	/// </summary>
	public interface IEventTypeNamingStrategy
	{
		/// <summary>
		/// Determine the event type to use in the message header for a particular type of domain event.
		/// </summary>
		/// <param name="eventType">Type of domain event.</param>
		/// <returns>Event type name for specified domain event type.</returns>
		string GetEventTypeName(Type eventType);
	}
}