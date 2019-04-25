using System;
using System.Reflection;

namespace IO.Eventuate.Tram.Events.Common
{
	/// <summary>
	/// Uses the EventTypeAttribute if present to determine the event type name to use in the message header
	/// for a particular type of domain event. Otherwise, falls back to use the the full name of the type.
	/// </summary>
	public class AttributeEventTypeNamingStrategy : IEventTypeNamingStrategy
	{
		/// <inheritdoc />
		public string GetEventTypeName(Type eventType)
		{
			// Get event type attribute
			var eventTypeAttribute = eventType.GetCustomAttribute<EventTypeAttribute>();

			// Use event type from attribute, if it exists; otherwise, use type full name
			return eventTypeAttribute != null ? eventTypeAttribute.EventType : eventType.FullName;
		}
	}
}