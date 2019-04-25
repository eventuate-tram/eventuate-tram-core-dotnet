using System;

namespace IO.Eventuate.Tram.Events.Common
{
	/// <summary>
	/// Allows overriding the event type used in the header of published domain event messages
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class EventTypeAttribute : Attribute
	{
		/// <summary>
		/// Event type name to use
		/// </summary>
		public string EventType { get; }

		/// <summary>
		/// Overrides the event type name used in the header of published domain event messages
		/// </summary>
		/// <param name="eventType">Event type name to use</param>
		public EventTypeAttribute(string eventType)
		{
			EventType = eventType;
		}
	}
}