using System;
using System.Threading.Tasks;
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
			AddEventuateTramDomainEventDispatcher(serviceCollection, subscriberId,
				provider => Task.FromResult(domainEventHandlersFactory(provider)));
		}
		
		public static void AddEventuateTramDomainEventDispatcher(
			this IServiceCollection serviceCollection, string subscriberId,
			Func<IServiceProvider, Task<DomainEventHandlers>> domainEventHandlersFactory)
		{
			serviceCollection.TryAddSingleton<IEventTypeNamingStrategy, AttributeEventTypeNamingStrategy>();
			serviceCollection.AddSingleton(provider =>
			{
				var messageConsumer = provider.GetRequiredService<IMessageConsumer>();
				var logger = provider.GetRequiredService<ILogger<DomainEventDispatcher>>();
				var eventTypeNamingStrategy = provider.GetRequiredService<IEventTypeNamingStrategy>();
				var jsonMapper = provider.GetRequiredService<IJsonMapper>();

				var dispatcher = new DomainEventDispatcher(subscriberId,
					async () => await domainEventHandlersFactory(provider),
					messageConsumer, eventTypeNamingStrategy, jsonMapper, logger);

				return dispatcher;
			});
		}

		public static void AddEventuateTramSqlProducer(this IServiceCollection serviceCollection,
			string eventuateDatabaseSchema, Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
		{
			serviceCollection.AddEventuateTramCommonSqlMessagingServices(eventuateDatabaseSchema, dbContextOptionsAction);
			serviceCollection.TryAddSingleton<IIdGenerator, IdGenerator>();
			serviceCollection.TryAddSingleton<ITimingProvider, TimingProvider>();
			serviceCollection.TryAddScoped<IMessageProducer, DatabaseMessageProducer>();
			serviceCollection.AddEventuateTramJsonMapper();
		}

		private static void AddEventuateTramCommonSqlMessagingServices(
			this IServiceCollection serviceCollection, string eventuateDatabaseSchema,
			Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
		{
			serviceCollection.TryAddSingleton(provider => new EventuateSchema(eventuateDatabaseSchema));
			serviceCollection.AddDbContext<EventuateTramDbContext>(dbContextOptionsAction);
			serviceCollection.TryAddScoped<IEventuateTramDbContextProvider, EventuateTramDbContextProvider>();
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
				var jsonMapper = provider.GetRequiredService<IJsonMapper>();

				IMessageConsumer messageConsumer = new KafkaMessageConsumer(bootstrapServers,
					consumerConfigurationProperties, decoratedMessageHandlerFactory,
					loggerFactory, serviceScopeFactory, jsonMapper);

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
			serviceCollection.AddEventuateTramJsonMapper();
		}

		private static void AddEventuateTramJsonMapper(this IServiceCollection serviceCollection)
		{
			serviceCollection.TryAddSingleton<IJsonMapper, SystemTextJsonMapper>();
		}
	}
}