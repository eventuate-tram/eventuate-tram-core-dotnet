using IO.Eventuate.Tram.Events.Common;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	public interface IDomainEventHandler<in T> where T : IDomainEvent
	{
		void Handle(IDomainEventEnvelope<T> @event);
	}
}