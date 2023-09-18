using System;
using IO.Eventuate.Tram.Database;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    public class TestHostBuilder
    {
        private String _sqlConnectionString;
        private String _eventuateDatabaseSchemaName;
        private String _kafkaBootstrapServers;
        private String _subscriberId;
        private Func<IServiceProvider, DomainEventHandlers> _domainEventHandlersFactory;
        private EventuateKafkaConsumerConfigurationProperties _consumerConfigProperties = EventuateKafkaConsumerConfigurationProperties.Empty();

        private IHost _host;

        public TestHostBuilder SetConnectionString(String sqlConnectionString)
        {
            _sqlConnectionString = sqlConnectionString;
            return this;
        }

        public TestHostBuilder SetEventuateDatabaseSchemaName(String eventuateDatabaseSchemaName)
        {
            _eventuateDatabaseSchemaName = eventuateDatabaseSchemaName;
            return this;
        }

        public TestHostBuilder SetKafkaBootstrapServers(String kafkaBootstrapServers)
        {
            _kafkaBootstrapServers = kafkaBootstrapServers;
            return this;
        }

        public TestHostBuilder SetSubscriberId(String subscriberId)
        {
            _subscriberId = subscriberId;
            return this;
        }

        public TestHostBuilder SetDomainEventHandlersFactory(
            Func<IServiceProvider, DomainEventHandlers> domainEventHandlersFactory)
        {
            _domainEventHandlersFactory = domainEventHandlersFactory;
            return this;
        }

        public TestHostBuilder SetConsumerConfigProperties(EventuateKafkaConsumerConfigurationProperties consumerConfigProperties)
        {
            _consumerConfigProperties = consumerConfigProperties;
            return this;
        }

 
        public IHost Build<TConsumerType>(bool withInterceptor) where TConsumerType : class
        {
            _host = new HostBuilder()
                .UseDefaultServiceProvider(options => options.ValidateScopes = true)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<EventuateTramDbContext>((provider, o) =>
                    {
                        o.UseSqlServer(_sqlConnectionString)
                            // Use a model cache key factory that ensures a new model is created if EventuateSchema is changed
                            .ReplaceService<IModelCacheKeyFactory, DynamicEventuateSchemaModelCacheKeyFactory>();
                    });
                    services.AddEventuateTramSqlKafkaTransport(_eventuateDatabaseSchemaName, _kafkaBootstrapServers, _consumerConfigProperties,
                        (provider, o) =>
                        {
                            o.UseSqlServer(_sqlConnectionString);
                        });
                    if (withInterceptor)
                    {
                        services.AddSingleton<IMessageInterceptor, TestMessageInterceptor>();
                    }

                    // Publisher Setup
                    services.AddEventuateTramEventsPublisher();

                    // Consumer Setup
                    services.AddSingleton<TConsumerType>();
                    services.AddEventuateTramDomainEventDispatcher(_subscriberId, _domainEventHandlersFactory);
                    services.AddSingleton<TestMessage4Handler>();

                    services.Configure<HostOptions>((options) => options.ShutdownTimeout = TimeSpan.FromSeconds(30));
                })
                .Build();
            return _host;
        }
    }
}
