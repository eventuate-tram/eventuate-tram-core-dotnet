using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public interface IDomainEventEnvelope<out T> where T : IDomainEvent
	{
		string AggregateId { get; }
		IMessage Message { get; }
		string AggregateType { get; }
		string EventId { get; }

		T Event { get; }		
	}
}