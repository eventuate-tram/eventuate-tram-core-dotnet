using System;
using IO.Eventuate.Tram.Database;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    public class TestHostBuilder
    {
        private String _sqlConnectionString = null;
        private String _eventuateDatabaseSchemaName = null;
        private String _kafkaBootstrapServers = null;
        private String _subscriberId = null;
        private Func<IServiceProvider, DomainEventHandlers> _domainEventHandlersFactory = null;
        private EventuateKafkaConsumerConfigurationProperties _consumerConfigProperties = EventuateKafkaConsumerConfigurationProperties.Empty();

        private IHost _host;

        public TestHostBuilder SetConnectionString(String sqlConnectionString)
        {
            this._sqlConnectionString = sqlConnectionString;
            return this;
        }

        public TestHostBuilder SetEventuateDatabaseSchemaName(String eventuateDatabaseSchemaName)
        {
            this._eventuateDatabaseSchemaName = eventuateDatabaseSchemaName;
            return this;
        }

        public TestHostBuilder SetKafkaBootstrapServers(String kafkaBootstrapServers)
        {
            this._kafkaBootstrapServers = kafkaBootstrapServers;
            return this;
        }

        public TestHostBuilder SetSubscriberId(String subscriberId)
        {
            this._subscriberId = subscriberId;
            return this;
        }

        public TestHostBuilder SetDomainEventHandlersFactory(
            Func<IServiceProvider, DomainEventHandlers> domainEventHandlersFactory)
        {
            this._domainEventHandlersFactory = domainEventHandlersFactory;
            return this;
        }

        public TestHostBuilder SetConsumerConfigProperties(EventuateKafkaConsumerConfigurationProperties consumerConfigProperties)
        {
            this._consumerConfigProperties = consumerConfigProperties;
            return this;
        }

 
        public IHost Build<TConsumerType>(bool withInterceptor) where TConsumerType : class
        {
            _host = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<EventuateTramDbContext>((provider, o) =>
                    {
                        o.UseSqlServer(_sqlConnectionString);
                    });
                    services.AddEventuateTramSqlKafkaTransport(_eventuateDatabaseSchemaName, _kafkaBootstrapServers, EventuateKafkaConsumerConfigurationProperties.Empty());
                    if (withInterceptor)
                    {
                        services.AddSingleton<IMessageInterceptor>(new TestMessageInterceptor());
                    }

                    // Publisher Setup
                    services.AddEventuateTramEventsPublisher();

                    // Consumer Setup
                    services.AddSingleton<TConsumerType>();
                    services.AddEventuateTramDomainEventDispatcher(_subscriberId, _domainEventHandlersFactory);
                })
                .Build();
            return _host;
        }
    }
}
