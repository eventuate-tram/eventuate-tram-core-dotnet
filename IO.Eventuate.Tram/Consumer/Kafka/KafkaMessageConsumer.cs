/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-consumer-kafka
 * package:	io.eventuate.tram.consumer.kafka
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using IO.Eventuate.Tram.Consumer.Common;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Consumer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Consumer.Kafka
{
	public class KafkaMessageConsumer : IMessageConsumer, IAsyncDisposable
	{
		private readonly ILogger _logger;
		private readonly EventuateKafkaConsumerConfigurationProperties _eventuateKafkaConsumerConfigurationProperties;
		private readonly DecoratedMessageHandlerFactory _decoratedMessageHandlerFactory;
		private readonly ILoggerFactory _loggerFactory;
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly IJsonMapper _jsonMapper;
		private readonly Func<ITopicPartitionToSwimlaneMapping> _swimlaneMappingFactory;

		private readonly string _id = Guid.NewGuid().ToString();
		private readonly string _bootstrapServers;
		private readonly List<EventuateKafkaConsumer> _consumers = new();
		private readonly List<SwimlaneBasedDispatcher> _dispatchers = new();
		private readonly object _lockObject = new();

		public KafkaMessageConsumer(string bootstrapServers,
			EventuateKafkaConsumerConfigurationProperties eventuateKafkaConsumerConfigurationProperties,
			DecoratedMessageHandlerFactory decoratedMessageHandlerFactory, ILoggerFactory loggerFactory,
			IServiceScopeFactory serviceScopeFactory, IJsonMapper jsonMapper)
			: this(bootstrapServers, eventuateKafkaConsumerConfigurationProperties, decoratedMessageHandlerFactory,
				loggerFactory, serviceScopeFactory, jsonMapper, null)
		{
		}

		/// <param name="swimlaneMappingFactory">
		/// Creates the swimlane mapping for each subscription. If it's null or returns null, the mapping is
		/// <see cref="SwimlanePerTopicPartition"/> when the back pressure scope is
		/// <see cref="BackPressureScope.Topic"/>, and <see cref="OriginalTopicPartitionToSwimlaneMapping"/> otherwise.
		/// </param>
		public KafkaMessageConsumer(string bootstrapServers,
			EventuateKafkaConsumerConfigurationProperties eventuateKafkaConsumerConfigurationProperties,
			DecoratedMessageHandlerFactory decoratedMessageHandlerFactory, ILoggerFactory loggerFactory,
			IServiceScopeFactory serviceScopeFactory, IJsonMapper jsonMapper,
			Func<ITopicPartitionToSwimlaneMapping> swimlaneMappingFactory)
		{
			_bootstrapServers = bootstrapServers;
			_eventuateKafkaConsumerConfigurationProperties = eventuateKafkaConsumerConfigurationProperties;
			_decoratedMessageHandlerFactory = decoratedMessageHandlerFactory;
			_loggerFactory = loggerFactory;
			_serviceScopeFactory = serviceScopeFactory;
			_jsonMapper = jsonMapper;
			_swimlaneMappingFactory = swimlaneMappingFactory;
			_logger = _loggerFactory.CreateLogger<KafkaMessageConsumer>();
		}

		private ITopicPartitionToSwimlaneMapping CreateSwimlaneMapping(string logContext)
		{
			bool topicScope = _eventuateKafkaConsumerConfigurationProperties.BackPressure.Scope == BackPressureScope.Topic;
			ITopicPartitionToSwimlaneMapping swimlaneMapping = _swimlaneMappingFactory?.Invoke();
			if (swimlaneMapping == null)
			{
				return topicScope ? new SwimlanePerTopicPartition() : new OriginalTopicPartitionToSwimlaneMapping();
			}

			if (topicScope && swimlaneMapping is OriginalTopicPartitionToSwimlaneMapping)
			{
				_logger.LogWarning($"{logContext}: BackPressure.Scope is Topic, but the swimlane mapping is " +
				                   $"{nameof(OriginalTopicPartitionToSwimlaneMapping)}, so topics share swimlanes and each " +
				                   "topic's backlog includes the other topics' messages on them. " +
				                   $"Use {nameof(SwimlanePerTopicPartition)}, or don't register a mapping.");
			}

			return swimlaneMapping;
		}

		public IMessageSubscription Subscribe(string subscriberId, ISet<string> channels, MessageHandlerAsync handler)
		{
			var logContext = $"{nameof(Subscribe)} for subscriberId='{subscriberId}', " +
			                 $"channels='{String.Join(",", channels)}', " +
			                 $"handler='{handler.Method.Name}'";
			_logger.LogDebug($"+{logContext}");
			
			Func<SubscriberIdAndMessage, IServiceProvider, CancellationToken, Task> decoratedHandler = _decoratedMessageHandlerFactory.Decorate(handler);
			
			var swimLaneBasedDispatcher = new SwimlaneBasedDispatcher(subscriberId, _loggerFactory, CreateSwimlaneMapping(logContext));

			EventuateKafkaConsumerMessageHandler kcHandler =
				async (record, completionCallback) => await swimLaneBasedDispatcher.DispatchAsync(ToMessage(record), record.TopicPartition, record.Message.Key,
					(message, cancellationToken) => HandleAsync(message, completionCallback, subscriberId, decoratedHandler, cancellationToken));
			
			var kc = new EventuateKafkaConsumer(subscriberId,
				kcHandler,
				new List<string>(channels),
				_bootstrapServers,
				_eventuateKafkaConsumerConfigurationProperties,
				_loggerFactory);

			lock (_lockObject)
			{
				_consumers.Add(kc);
				_dispatchers.Add(swimLaneBasedDispatcher);
			}

			kc.Start();
			
			_logger.LogDebug($"-{logContext}");
			return new MessageSubscription(async () =>
			{
				await swimLaneBasedDispatcher.StopAsync();
				await kc.DisposeAsync();
				lock (_lockObject)
				{
					_dispatchers.Remove(swimLaneBasedDispatcher);
					_consumers.Remove(kc);
				}
			});
		}

		private async Task HandleAsync(IMessage message, Action<Exception> completionCallback, string subscriberId,
			Func<SubscriberIdAndMessage, IServiceProvider, CancellationToken, Task> decoratedHandler, CancellationToken cancellationToken)
		{
			try
			{
				// Creating a service scope and passing the scope's service provider to handlers
				// so they can resolve scoped services
				using IServiceScope scope = _serviceScopeFactory.CreateScope();
				await decoratedHandler(new SubscriberIdAndMessage(subscriberId, message), scope.ServiceProvider, cancellationToken);
				completionCallback(null);
			}
			catch (Exception e)
			{
				completionCallback(e);
				throw;
			}
		}

		public string GetId()
		{
			return _id;
		}

		public async Task CloseAsync()
		{
			_logger.LogDebug($"+{nameof(CloseAsync)}");

			List<SwimlaneBasedDispatcher> dispatchers;
			List<EventuateKafkaConsumer> consumers;
			lock (_lockObject)
			{
				dispatchers = _dispatchers.ToList();
				_dispatchers.Clear();
				consumers = _consumers.ToList();
				_consumers.Clear();
			}

			foreach (SwimlaneBasedDispatcher dispatcher in dispatchers)
			{
				await dispatcher.StopAsync();
			}

			foreach (EventuateKafkaConsumer consumer in consumers)
			{
				await consumer.DisposeAsync();
			}
			
			_logger.LogDebug($"-{nameof(CloseAsync)}");
		}

		/// <inheritdoc />
		private class MessageSubscription : IMessageSubscription
		{
			private readonly Func<Task> _unsubscribe;

			public MessageSubscription(Func<Task> unsubscribe)
			{
				_unsubscribe = unsubscribe;
			}

			/// <inheritdoc />
			public async Task UnsubscribeAsync()
			{
				await _unsubscribe();
			}
		}
		
		private Message ToMessage(ConsumeResult<string, string> record)
		{
			return _jsonMapper.FromJson<Message>(record.Message.Value);
		}

		// Following recommended standard implementation of DisposeAsync for unsealed classes
		public async ValueTask DisposeAsync()
		{
			await DisposeAsyncCore();
			GC.SuppressFinalize(this);
		}
		
		protected virtual async ValueTask DisposeAsyncCore()
		{
			_logger.LogDebug($"+{nameof(DisposeAsyncCore)}");
			await CloseAsync();
			_logger.LogDebug($"-{nameof(DisposeAsyncCore)}");
		}
	}
}