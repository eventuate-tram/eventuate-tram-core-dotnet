using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace IO.Eventuate.Tram.Events.Subscriber
{
	/// <summary>
	/// Responsible for initializing all the DomainEventDispatcher instances on service startup.
	/// In Java this is done with a @PostConstruct annotation on the initialize() method of DomainEventDispatcher
	/// </summary>
	public class DomainEventDispatcherInitializer : IHostedService
	{
		private readonly IEnumerable<DomainEventDispatcher> _domainEventDispatchers;

		public DomainEventDispatcherInitializer(IEnumerable<DomainEventDispatcher> domainEventDispatchers)
		{
			_domainEventDispatchers = domainEventDispatchers;
		}
		
		public Task StartAsync(CancellationToken cancellationToken)
		{
			foreach (DomainEventDispatcher domainEventDispatcher in _domainEventDispatchers)
			{
				domainEventDispatcher.Initialize();
			}

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			foreach (DomainEventDispatcher domainEventDispatcher in _domainEventDispatchers)
			{
				domainEventDispatcher.Stop();
			}
			
			return Task.CompletedTask;
		}
	}
}