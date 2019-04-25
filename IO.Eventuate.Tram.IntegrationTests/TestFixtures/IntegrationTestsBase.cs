using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using IO.Eventuate.Tram.Database;
using IO.Eventuate.Tram.Events.Publisher;
using IO.Eventuate.Tram.IntegrationTests.TestHelpers;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Consumer.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestFixtures
{
    public class IntegrationTestsBase
    {
        private const string TestSettingsFile = "testsettings.json";
        private string _subscriberId = "xx";
        protected const string AggregateType = "TestMessagesTopic";
        protected string EventuateDatabaseSchemaName = "eventuate";

        protected TestSettings TestSettings;

        // There can only be one of these between all of the tests because there's no good way to tear it down and start again
        private static IHost _host = null;
        private static EventuateTramDbContext _dbContext = null;
        private static IDomainEventPublisher _domainEventPublisher = null;
        private static TestEventConsumer _testEventConsumer = null;
        private static TestMessageInterceptor _interceptor = null;

        public IntegrationTestsBase()
        {
            IConfigurationRoot configuration = null;
            try
            {
                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                ConfigureFromEnvironmentAndSettingsFile(configurationBuilder);
                configuration = configurationBuilder.Build();
            }
            catch
            {
                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                ConfigureFromEnvironment(configurationBuilder);
                configuration = configurationBuilder.Build();
            }

            TestSettings = configuration.Get<TestSettings>();
        }

        protected void TestSetup(string schema, bool withInterceptor, EventuateKafkaConsumerConfigurationProperties consumerConfigProperties)
        {
            EventuateDatabaseSchemaName = schema;
            _subscriberId = Guid.NewGuid().ToString();

            if (_host == null)
            {
                _host = SetupTestHost(withInterceptor, consumerConfigProperties);
                _dbContext = _host.Services.GetService<EventuateTramDbContext>();
                _domainEventPublisher = _host.Services.GetService<IDomainEventPublisher>();
                _testEventConsumer = _host.Services.GetService<TestEventConsumer>();
                _interceptor = (TestMessageInterceptor)_host.Services.GetService<IMessageInterceptor>();
            }
        }

        protected void CleanupTest()
        {
            ClearDb(GetDbContext(), EventuateDatabaseSchemaName);
            GetTestConsumer().Reset();
            GetTestMessageInterceptor()?.Reset();
        }

        protected void CleanupKafka()
        {
            var config = new AdminClientConfig();
            config.BootstrapServers = TestSettings.KafkaBootstrapServers;
            using (var admin = new AdminClientBuilder(config).Build())
            {
                admin.DeleteTopicsAsync(new string[] { AggregateType }).Wait();
            }
        }

        protected void ShowTestResults()
        {
            TestContext.WriteLine("Test Config");
            TestContext.WriteLine("  Connection String: {0}", TestSettings.ConnectionStrings.EventuateTramDbConnection);
            TestContext.WriteLine("  Kafka server:      {0}", TestSettings.KafkaBootstrapServers);
            TestContext.WriteLine("  Schema:            {0}", EventuateDatabaseSchemaName);
            TestContext.WriteLine("  Dispatcher Id:     {0}", _subscriberId);
            TestContext.WriteLine("  Aggregate Type:    {0}", AggregateType);

            TestContext.WriteLine("Test Results");
            TestContext.WriteLine("  N Messages in DB:  {0}", _dbContext.Messages.Count());
            TestContext.WriteLine("  Unpublished Count: {0}", _dbContext.Messages.Count(msg => msg.Published == 0));
            TestContext.WriteLine("  N Received in DB:  {0}", _dbContext.ReceivedMessages.Count(msg => msg.MessageId != null));
            TestContext.WriteLine("  Received Type 1:   {0}", _testEventConsumer.Type1MessageCount);
            TestContext.WriteLine("  Received Type 2:   {0}", _testEventConsumer.Type2MessageCount);
            TestContext.WriteLine("  Exception Count:   {0}", _testEventConsumer.ExceptionCount);

            if (_interceptor != null)
            {
                TestContext.WriteLine("Message Interceptor Counts");
                TestContext.WriteLine("  Pre Send:     {0}", _interceptor.PreSendCount);
                TestContext.WriteLine("  Post Send:    {0}", _interceptor.PostSendCount);
                TestContext.WriteLine("  Pre Receive:  {0}", _interceptor.PreReceiveCount);
                TestContext.WriteLine("  Post Receive: {0}", _interceptor.PostReceiveCount);
                TestContext.WriteLine("  Pre Handle:   {0}", _interceptor.PreHandleCount);
                TestContext.WriteLine("  Post Handle:  {0}", _interceptor.PostHandleCount);
            }
        }

        /// <summary>
        /// Set up the configuration for the HostBuilder
        /// </summary>
        protected void ConfigureFromEnvironmentAndSettingsFile(IConfigurationBuilder config,
            Dictionary<string, string> overrides = null)
        {
            config
                .AddJsonFile(TestSettingsFile, false)
                .AddEnvironmentVariables()
                .AddInMemoryCollection(overrides);
        }

        /// <summary>
        /// Set up the configuration for the HostBuilder
        /// </summary>
        protected void ConfigureFromEnvironment(IConfigurationBuilder config,
            Dictionary<string, string> overrides = null)
        {
            config
                .AddEnvironmentVariables()
                .AddInMemoryCollection(overrides);
        }

        protected IHost SetupTestHost(bool withInterceptor, EventuateKafkaConsumerConfigurationProperties consumerConfigProperties)
        {
            var host = new TestHostBuilder()
                .SetConnectionString(TestSettings.ConnectionStrings.EventuateTramDbConnection)
                .SetEventuateDatabaseSchemaName(EventuateDatabaseSchemaName)
                .SetKafkaBootstrapServers(TestSettings.KafkaBootstrapServers)
                .SetSubscriberId(_subscriberId)
                .SetDomainEventHandlersFactory(
                    provider =>
                    {
                        var consumer = provider.GetRequiredService<TestEventConsumer>();
                        return consumer.DomainEventHandlers(AggregateType);
                    })
                .SetConsumerConfigProperties(consumerConfigProperties)
                .Build<TestEventConsumer>(withInterceptor);
            host.StartAsync().Wait();
            return host;
        }

        protected void DisposeTestHost()
        {
            if (_host == null)
                return;

            _host.StopAsync().Wait();
            _host.Dispose();
            _host = null;
            _dbContext = null;
            _domainEventPublisher = null;
            _testEventConsumer = null;
        }

        protected TestEventConsumer GetTestConsumer()
        {
            return _testEventConsumer;
        }

        protected TestMessageInterceptor GetTestMessageInterceptor()
        {
            return _interceptor;
        }

        protected IDomainEventPublisher GetTestPublisher()
        {
            return _domainEventPublisher;
        }

        protected EventuateTramDbContext GetDbContext()
        {
            return _dbContext;
        }

        protected void ClearDb(EventuateTramDbContext dbContext, String eventuateDatabaseSchemaName)
        {
            dbContext.Database.ExecuteSqlCommand(String.Format("Delete from [{0}].[message]", eventuateDatabaseSchemaName));
            dbContext.Database.ExecuteSqlCommand(String.Format("Delete from [{0}].[received_messages]", eventuateDatabaseSchemaName));
        }
    }
}
