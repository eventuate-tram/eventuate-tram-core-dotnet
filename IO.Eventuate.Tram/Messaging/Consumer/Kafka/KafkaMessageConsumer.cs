using System;
using System.Collections.Generic;
using System.Transactions;
using Confluent.Kafka;
using IO.Eventuate.Tram.Database;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Messaging.Consumer.Kafka
{
	public class KafkaMessageConsumer : IMessageConsumer, IDisposable
	{
		private readonly ILogger _logger;
		private readonly EventuateKafkaConsumerConfigurationProperties _eventuateKafkaConsumerConfigurationProperties;
		private readonly IEnumerable<IMessageInterceptor> _messageInterceptors;
		private readonly ILoggerFactory _loggerFactory;
		private readonly IServiceScopeFactory _serviceScopeFactory;

		private readonly string _bootstrapServers;
		private readonly List<EventuateKafkaConsumer> _consumers = new List<EventuateKafkaConsumer>();
		
		public KafkaMessageConsumer(string bootstrapServers,
			EventuateKafkaConsumerConfigurationProperties eventuateKafkaConsumerConfigurationProperties,
			IEnumerable<IMessageInterceptor> messageInterceptors, ILoggerFactory loggerFactory,
			IServiceScopeFactory serviceScopeFactory)
		{
			_bootstrapServers = bootstrapServers;
			_eventuateKafkaConsumerConfigurationProperties = eventuateKafkaConsumerConfigurationProperties;
			_messageInterceptors = messageInterceptors;
			_loggerFactory = loggerFactory;
			_serviceScopeFactory = serviceScopeFactory;
			_logger = _loggerFactory.CreateLogger<KafkaMessageConsumer>();
		}

		public void Subscribe(string subscriberId, ISet<string> channels, MessageHandler handler)
		{
			var logContext = $"{nameof(Subscribe)} for subscriberId='{subscriberId}', " +
			                 $"channels='{String.Join(",", channels)}', " +
			                 $"handler='{handler.Method.Name}'";
			_logger.LogDebug($"+{logContext}");
			
			var swimLaneBasedDispatcher = new SwimlaneBasedDispatcher(subscriberId, _loggerFactory);

			Action<ConsumeResult<string, string>, Action<Exception>> kcHandler =
				(record, completionCallback) =>
					HandleKafkaConsumeResult(subscriberId, handler, swimLaneBasedDispatcher, logContext, record, completionCallback);
			
			var kc = new EventuateKafkaConsumer(subscriberId,
				kcHandler,
				new List<string>(channels),
				_bootstrapServers,
				_eventuateKafkaConsumerConfigurationProperties,
				_loggerFactory);

			_consumers.Add(kc);

			kc.Start();
			_logger.LogDebug($"-{logContext}");
		}

		/// <summary>
		/// Handles a Kafka ConsumeResult by converting it to an IMessage and dispatching it using the specified dispatcher
		/// </summary>
		/// <param name="subscriberId">The subscriber ID (Kafka consumer group)</param>
		/// <param name="handler">External message handler</param>
		/// <param name="swimLaneBasedDispatcher">Swim lane dispatcher that routes messages by partition ID</param>
		/// <param name="logContext">Log context</param>
		/// <param name="record">Kafka consume result</param>
		/// <param name="completionCallback">Called after the message has been handled with the exception if applicable</param>
		private void HandleKafkaConsumeResult(string subscriberId, MessageHandler handler,
			SwimlaneBasedDispatcher swimLaneBasedDispatcher, string logContext, ConsumeResult<string, string> record, Action<Exception> completionCallback)
		{
			swimLaneBasedDispatcher.Dispatch(ToMessage(record), record.Partition,
				message => { HandleDispatchedMessage(subscriberId, handler, logContext, message, completionCallback); }
			);
		}

		/// <summary>
		/// Handle a dispatched message. Calls message interceptor hooks and the external message handler.
		/// </summary>
		/// <param name="subscriberId">The subscriber ID (Kafka consumer group)</param>
		/// <param name="handler">External message handler</param>
		/// <param name="logContext">Log context</param>
		/// <param name="message">Dispatched message</param>
		/// <param name="completionCallback">Called after the message has been handled with the exception if applicable</param>
		private void HandleDispatchedMessage(string subscriberId, MessageHandler handler, string logContext, IMessage message,
			Action<Exception> completionCallback)
		{
			_logger.LogDebug($"{logContext}: Dispatcher called for messageId='{message.Id}'");
			using (IServiceScope serviceScope = _serviceScopeFactory.CreateScope())
			{
				IServiceProvider scopedServiceProvider = serviceScope.ServiceProvider;
				PreReceive(message);
				try
				{
					var dbContext = scopedServiceProvider.GetRequiredService<EventuateTramDbContext>();
					IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
					strategy.Execute(() =>
					{
						using (var transactionScope = new TransactionScope())
						{
							var duplicateMessageDetector = scopedServiceProvider
								.GetRequiredService<IDuplicateMessageDetector>();
							if (duplicateMessageDetector.IsDuplicate(subscriberId, message.Id))
							{
								_logger.LogDebug($"{logContext}: messageId='{message.Id}' is a duplicate");
								completionCallback(null);
								transactionScope.Complete();
								return;
							}

							try
							{
								_logger.LogDebug($"{logContext}: Invoking handlers for messageId='{message.Id}'");
								PreHandle(subscriberId, message);
								handler(message, scopedServiceProvider);
								PostHandle(subscriberId, message, null);
							}
							catch (Exception e)
							{
								PostHandle(subscriberId, message, e);
								_logger.LogError(e, $"{logContext}: Exception processing messageId='{message.Id}'");
								completionCallback(e);
								transactionScope.Complete();
								return;
							}

							completionCallback(null);
							transactionScope.Complete();
							_logger.LogDebug($"{logContext}: Processed messageId='{message.Id}'");
						}
					});
				}
				finally
				{
					PostReceive(message);
				}
			}

			_logger.LogDebug($"{logContext}: Dispatcher done with messageId='{message.Id}'");
		}

		private void PreReceive(IMessage message)
		{
			foreach (IMessageInterceptor interceptor in _messageInterceptors)
			{
				interceptor.PreReceive(message);
			}
		}

		private void PreHandle(string subscriberId, IMessage message)
		{
			foreach (IMessageInterceptor interceptor in _messageInterceptors)
			{
				interceptor.PreHandle(subscriberId, message);
			}
		}

		private void PostHandle(string subscriberId, IMessage message, Exception e)
		{
			foreach (IMessageInterceptor interceptor in _messageInterceptors)
			{
				interceptor.PostHandle(subscriberId, message, e);
			}
		}

		private void PostReceive(IMessage message)
		{
			foreach (IMessageInterceptor interceptor in _messageInterceptors)
			{
				interceptor.PostReceive(message);
			}
		}

		private IMessage ToMessage(ConsumeResult<string, string> record)
		{
			return JsonMapper.FromJson<Message>(record.Value);
		}

		public void Dispose()
		{
			_logger.LogDebug($"+{nameof(Dispose)}");
			foreach (EventuateKafkaConsumer consumer in _consumers)
			{
				consumer.Dispose();
			}
			_logger.LogDebug($"-{nameof(Dispose)}");
		}
	}
}