using System;
using System.Collections.Concurrent;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Consumer.Kafka
{
	public class SwimlaneBasedDispatcher
	{
		private readonly object _lockObject = new object();
		private bool _dispatcherStopped;

		private readonly ConcurrentDictionary<int, SwimlaneDispatcher> _map = new ConcurrentDictionary<int, SwimlaneDispatcher>();
		private readonly string _subscriberId;
		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger _logger;
		private readonly string _dispatcherContext;

		public SwimlaneBasedDispatcher(string subscriberId, ILoggerFactory loggerFactory)
		{
			_subscriberId = subscriberId;
			_loggerFactory = loggerFactory;
			_logger = _loggerFactory.CreateLogger<SwimlaneBasedDispatcher>();
			_dispatcherContext = $"SubscriberId='{subscriberId}'";
		}

		public SwimlaneDispatcherBacklog Dispatch(IMessage message, int swimlane, Action<IMessage> target)
		{
			var logContext = $"{nameof(Dispatch)} for {_dispatcherContext}, swimlane='{swimlane}', MessageId={message.Id}";
			_logger.LogDebug($"+{logContext}");
			if (!_map.TryGetValue(swimlane, out SwimlaneDispatcher swimlaneDispatcher))
			{
				_logger.LogDebug($"{logContext}: No dispatcher found, attempting to create");
				swimlaneDispatcher = new SwimlaneDispatcher(_subscriberId, swimlane, _loggerFactory.CreateLogger<SwimlaneDispatcher>());
				SwimlaneDispatcher r = _map.GetOrAdd(swimlane, swimlaneDispatcher);
				if (r != swimlaneDispatcher)
				{
					_logger.LogDebug($"{logContext}: Using concurrently created SwimlaneDispatcher");
					swimlaneDispatcher = r;
				}
				else
				{
					_logger.LogDebug($"{logContext}: Using newly created SwimlaneDispatcher");

					// If the dispatcher is stopped, make sure we stop the new swim lane dispatcher
					lock (_lockObject)
					{
						if (_dispatcherStopped)
						{
							_logger.LogDebug($"{logContext}: Stopping newly created SwimlaneDispatcher");
							r.Stop();
						}
					}
				}
			}

			SwimlaneDispatcherBacklog backlog = swimlaneDispatcher.Dispatch(message, target);
			_logger.LogDebug($"-{logContext}");

			return backlog;
		}

		/// <summary>
		/// Stop the dispatcher from processing any further messages
		/// </summary>
		public void Stop()
		{
			var logContext = $"{nameof(Stop)} for {_dispatcherContext}";
			_logger.LogDebug($"+{logContext}");
			lock (_lockObject)
			{
				_dispatcherStopped = true;
				foreach (SwimlaneDispatcher dispatcher in _map.Values)
				{
					dispatcher.Stop();
				}
			}

			_logger.LogDebug($"-{logContext}");
		}
	}
}