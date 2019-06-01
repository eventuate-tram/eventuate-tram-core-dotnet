using System;
using IO.Eventuate.Tram.Consumer.Common;
using IO.Eventuate.Tram.Consumer.Database;
using IO.Eventuate.Tram.Consumer.Kafka;
using IO.Eventuate.Tram.Database;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Publisher;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using IO.Eventuate.Tram.Messaging.Consumer;
using IO.Eventuate.Tram.Messaging.Producer;
using IO.Eventuate.Tram.Messaging.Producer.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram
{
	public static class EventuateTramServiceCollectionExtensions
	{
		public static void AddEventuateTramSqlKafkaTransport(this IServiceCollection serviceCollection,
			string eventuateDatabaseSchema, string bootstrapServers, EventuateKafkaConsumerConfigurationProperties consumerConfigurationProperties,
			Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
		{
			AddEventuateTramSqlProducer(serviceCollection, eventuateDatabaseSchema, dbContextOptionsAction);
			AddEventuateTramKafkaConsumer(serviceCollection, eventuateDatabaseSchema, bootstrapServers,
				consumerConfigurationProperties, dbContextOptionsAction);
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
			string eventuateDatabaseSchema, Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
		{
			AddEventuateTramCommonSqlMessagingServices(serviceCollection, eventuateDatabaseSchema, dbContextOptionsAction);
			serviceCollection.TryAddSingleton<IIdGenerator, IdGenerator>();
			serviceCollection.TryAddScoped<IMessageProducer, DatabaseMessageProducer>();
		}

		private static void AddEventuateTramCommonSqlMessagingServices(
			this IServiceCollection serviceCollection, string eventuateDatabaseSchema,
			Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
		{
			serviceCollection.TryAddSingleton(provider => new EventuateSchema(eventuateDatabaseSchema));
			serviceCollection.TryAddScoped<IEventuateTramDbContextProvider>(provider =>
			{
				var eventuateSchema = provider.GetRequiredService<EventuateSchema>();
				var builder = new DbContextOptionsBuilder<EventuateTramDbContext>();
				dbContextOptionsAction(provider, builder);
				return new EventuateTramDbContextProvider(builder.Options, eventuateSchema);
			});
		}

		public static void AddEventuateTramKafkaConsumer(this IServiceCollection serviceCollection,
			string eventuateDatabaseSchema, string bootstrapServers,
			EventuateKafkaConsumerConfigurationProperties consumerConfigurationProperties,
			Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
		{
			AddEventuateTramCommonSqlMessagingServices(serviceCollection, eventuateDatabaseSchema, dbContextOptionsAction);
			AddEventuateTramCommonConsumer(serviceCollection);
			serviceCollection.TryAddScoped<IDuplicateMessageDetector, SqlTableBasedDuplicateMessageDetector>();
			serviceCollection.TryAddSingleton(provider =>
			{
				var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
				var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
				var decoratedMessageHandlerFactory = provider.GetRequiredService<DecoratedMessageHandlerFactory>(); 

				IMessageConsumer messageConsumer = new KafkaMessageConsumer(bootstrapServers,
					consumerConfigurationProperties, decoratedMessageHandlerFactory,
					loggerFactory, serviceScopeFactory);

				return messageConsumer;
			});
			serviceCollection.AddHostedService<DomainEventDispatcherInitializer>();
		}

		private static void AddEventuateTramCommonConsumer(this IServiceCollection serviceCollection)
		{
			serviceCollection.TryAddSingleton<DecoratedMessageHandlerFactory>();
			serviceCollection.AddSingleton<IMessageHandlerDecorator, PrePostReceiveMessageHandlerDecorator>();
			serviceCollection.AddSingleton<IMessageHandlerDecorator, DuplicateDetectingMessageHandlerDecorator>();
			serviceCollection.AddSingleton<IMessageHandlerDecorator, PrePostHandlerMessageHandlerDecorator>();
		}
	}
}