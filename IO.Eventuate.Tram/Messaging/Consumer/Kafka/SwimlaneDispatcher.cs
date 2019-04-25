using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Messaging.Consumer.Kafka
{
	public class SwimlaneDispatcher
	{
		private readonly object _lockObject = new object();
		
		private readonly ILogger _logger;
		private readonly string _dispatcherContext;

		// The java solution used a blocking queue but none of the methods that use
		// the blocking feature. ConcurrentQueue is simpler.
		private readonly ConcurrentQueue<QueuedMessage> _queue =
			new ConcurrentQueue<QueuedMessage>();

		// The running flag must only be accessed within a lock(_lockObject)
		private bool _running;

		public SwimlaneDispatcher(string subscriberId, int swimlane, ILogger<SwimlaneDispatcher> logger)
		{
			_logger = logger;
			_dispatcherContext = $"SubscriberId='{subscriberId}', SwimLane='{swimlane}'";
		}

		public void Dispatch(IMessage message, Action<IMessage> messageConsumer)
		{
			var logContext = $"{nameof(Dispatch)} for {_dispatcherContext}, MessageId={message.Id}";
			_logger.LogDebug($"+{logContext}");
			lock (_lockObject)
			{
				var queuedMessage = new QueuedMessage(message, messageConsumer);
				_queue.Enqueue(queuedMessage);
				// Start a message processor if one is not running
				if (!_running)
				{
					_logger.LogDebug($"{logContext}: Added message and starting message processor");
					_running = true;
					StartMessageProcessor();
				}
				else
				{
					_logger.LogDebug($"{logContext}: Added message for already running message processor");
				}
			}
			_logger.LogDebug($"-{logContext}");
		}

		private void StartMessageProcessor()
		{
			// Java implementation uses a ThreadPoolExecutor as executor.
			// DOTNET Task.Run queues the work to run on the ThreadPool.
			Task.Run(() => ProcessQueuedMessage());
		}

		private void ProcessQueuedMessage()
		{
			var logContext = $"{nameof(ProcessQueuedMessage)} for {_dispatcherContext}";
			_logger.LogDebug($"+{logContext}");
			while (true)
			{
				if (!_queue.TryDequeue(out QueuedMessage queuedMessage))
				{
					// Queue was empty, check one more time with the lock on to
					// avoid a race and stop the processor if really empty
					lock (_lockObject)
					{
						if (!_queue.TryDequeue(out queuedMessage))
						{
							_logger.LogDebug($"{logContext}: No more messages, stopping message processor");
							_running = false;
							return;
						}
					}
				}
				_logger.LogDebug($"{logContext}: Invoking handler for MessageId='{queuedMessage.Message.Id}'");
				queuedMessage.MessageConsumer(queuedMessage.Message);
			}
		}

		private class QueuedMessage
		{
			public QueuedMessage(IMessage message, Action<IMessage> messageConsumer)
			{
				Message = message;
				MessageConsumer = messageConsumer;
			}
			
			public IMessage Message { get; }

			public Action<IMessage> MessageConsumer { get; }
		}
	}
}