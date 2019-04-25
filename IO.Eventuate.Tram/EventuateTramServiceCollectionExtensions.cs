using System;
using System.Collections.Generic;
using IO.Eventuate.Tram.Database;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Publisher;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Consumer;
using IO.Eventuate.Tram.Messaging.Consumer.Kafka;
using IO.Eventuate.Tram.Messaging.Producer;
using IO.Eventuate.Tram.Messaging.Producer.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram
{
	public static class EventuateTramServiceCollectionExtensions
	{
		public static void AddEventuateTramSqlKafkaTransport(this IServiceCollection serviceCollection,
			string eventuateDatabaseSchema, string bootstrapServers, EventuateKafkaConsumerConfigurationProperties consumerConfigurationProperties)
		{
			AddEventuateTramSqlProducer(serviceCollection, eventuateDatabaseSchema);
			AddEventuateTramKafkaConsumer(serviceCollection, eventuateDatabaseSchema, bootstrapServers,
				consumerConfigurationProperties);
		}

		public static void AddEventuateTramEventsPublisher(this IServiceCollection serviceCollection)
		{
			serviceCollection.TryAddSingleton<IEventTypeNamingStrategy, AttributeEventTypeNamingStrategy>();
			serviceCollection.TryAddScoped<IDomainEventPublisher, DomainEventPublisher>();
		}

		public static void AddEventuateTramDomainEventDispatcher(
			this IServiceCollection serviceCollection, string subscriberId,
			Func<IServiceProvider, DomainEventHandlers> domainEventHandlersFactory)
		{
			serviceCollection.TryAddSingleton<IEventTypeNamingStrategy, AttributeEventTypeNamingStrategy>();
			serviceCollection.AddSingleton(provider =>
			{
				var messageConsumer = provider.GetRequiredService<IMessageConsumer>();
				var logger = provider.GetRequiredService<ILogger<DomainEventDispatcher>>();
				var eventTypeNamingStrategy = provider.GetRequiredService<IEventTypeNamingStrategy>();

				var dispatcher = new DomainEventDispatcher(subscriberId, domainEventHandlersFactory(provider),
					messageConsumer, eventTypeNamingStrategy, logger);

				return dispatcher;
			});
		}

		public static void AddEventuateTramSqlProducer(this IServiceCollection serviceCollection,
			string eventuateDatabaseSchema)
		{
			AddEventuateTramCommonSqlMessagingServices(serviceCollection, eventuateDatabaseSchema);
			serviceCollection.TryAddSingleton<IIdGenerator, IdGenerator>();
			serviceCollection.TryAddScoped<IMessageProducer, OutboxMessageProducer>();
		}

		private static void AddEventuateTramCommonSqlMessagingServices(
			this IServiceCollection serviceCollection, string eventuateDatabaseSchema)
		{
			serviceCollection.TryAddSingleton(provider => new EventuateSchema(eventuateDatabaseSchema));
		}

		public static void AddEventuateTramKafkaConsumer(this IServiceCollection serviceCollection,
			string eventuateDatabaseSchema, string bootstrapServers,
			EventuateKafkaConsumerConfigurationProperties consumerConfigurationProperties)
		{
			AddEventuateTramCommonSqlMessagingServices(serviceCollection, eventuateDatabaseSchema);
			serviceCollection.TryAddScoped<IDuplicateMessageDetector, SqlTableBasedDuplicateMessageDetector>();
			serviceCollection.TryAddSingleton(provider =>
			{
				var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
				var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
				IEnumerable<IMessageInterceptor> messageInterceptors = provider.GetServices<IMessageInterceptor>();

				IMessageConsumer messageConsumer = new KafkaMessageConsumer(bootstrapServers,
					consumerConfigurationProperties, messageInterceptors,
					loggerFactory, serviceScopeFactory);

				return messageConsumer;
			});
			serviceCollection.AddHostedService<DomainEventDispatcherInitializer>();
		}
	}
}