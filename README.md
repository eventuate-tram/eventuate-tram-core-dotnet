# Eventuate Tram (Transactional Messaging) framework
A .NET port of version 0.21.0.RELEASE of <https://github.com/eventuate-tram/eventuate-tram-core>.

## Usage

### Register Services
Register the core Eventuate Tram services in Startup.cs. You probably want to configure
Eventuate Tram to use the same DbConnection as your application uses when persisting application 
state. This will allow for events to be published and consumed within the same transaction as 
application state changes are saved. If your application is using Entity Framework, this can be done as
follows:
```c#
services.AddEventuateTramSqlKafkaTransport(dbSchemaName, bootstrapServers,
	eventuateKafkaConsumerConfigurationProperties, (provider, o) =>
	{
		var applicationDbContext = provider.GetRequiredService<ApplicationDbContext>();
		o.UseSqlServer(applicationDbContext.Database.GetDbConnection());
	});
```

To use the default eventuateKafkaConsumerConfigurationProperties, just pass in EventuateKafkaConsumerConfigurationProperties.Empty()

The AddEventuateTramSqlKafkaTransport() method will register the core services for both 
publishing and consuming messages. Alternatively, if your application only needs to publish messages 
you can use the AddEventuateTramSqlProducer() method:
```c#
services.AddEventuateTramSqlProducer(dbSchemaName, (provider, o) =>
	{
		var applicationDbContext = provider.GetRequiredService<ApplicationDbContext>();
		o.UseSqlServer(applicationDbContext.Database.GetDbConnection());
	});
```

### Database Setup
You need to run the [database initialization script](IO.Eventuate.Tram/Database/mssql/initialize-database.sql),
modifying the database schema $(TRAM_SCHEMA) variable to match what you configured. 

### Publishing Events
To enable domain event publishing register the DomainEventPublisher service in Startup.cs:
```c#
services.AddEventuateTramEventsPublisher();
```

To publish a domain event, inject an IDomainEventPublisher into your service and then call IDomainEventPublisher.Publish:
```c#
var @event = new ExampleEvent
{
    ...
};
    
_domainEventPublisher.Publish(aggregateType, aggregateId, new List<IDomainEvent> {@event});
```
The aggregateType will be used as the Kafka topic, and the aggregateId as the partition ID.
Your event type needs to implement IDomainEvent.
If you want to publish events atomically along with persisting application state changes, 
you should call Publish() within a transaction along with saving the changes to you application state.
If your application is using Entity Framework, this can be done as follows:
```c#
using (var scope = new TransactionScope())
{
    _applicationDbContext.SaveChanges();
    _domainEventPublisher.Publish(aggregateType, aggregateId, new List<IDomainEvent> {@event});
    scope.Complete();
}
```
Alternatively, there is also an asynchronous publish method available:
```c#
using (var scope = new TransactionScope(new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled)))
{
    await _applicationDbContext.SaveChangesAsync();
    await _domainEventPublisher.PublishAsync(aggregateType, aggregateId, new List<IDomainEvent> {@event});
    scope.Complete();
}
```

If EnableRetryOnFailure is enabled on your DbContext, you will need to wrap the transaction in
an ExecutionStrategy Execute() method:
```c#
IExecutionStrategy strategy = _applicationDbContext.Database.CreateExecutionStrategy();
strategy.Execute(() =>
{
    using (var scope = new TransactionScope())
    {
        // Defer accepting changes until after transaction is committed so that
        // the changes are saved again on an execution strategy retry
        _applicationDbContext.SaveChanges(false);
        _domainEventPublisher.Publish(aggregateType, aggregateId, new List<IDomainEvent> {@event});
        scope.Complete();
        _applicationDbContext.ChangeTracker.AcceptAllChanges();
    }
});
```

Finally, the [Eventuate CDC Service](https://github.com/eventuate-foundation/eventuate-cdc) will need to be running and monitoring your database
for new events to publish. The [Eventuate Tram Code Basic examples](https://github.com/eventuate-tram/eventuate-tram-core-examples-basic) 
project has an example docker-compose.yml file. See also the Eventuate CDC Service configuration 
[documentation](https://eventuate.io/docs/manual/eventuate-tram/latest/cdc-configuration.html).

#### Message ID Generation

Eventuate Tram generates message IDs using the network interface's MAC address by default when publishing messages. 
This can result in duplicate message IDs being generated if multiple processes using Eventuate Tram 
are running on the same machine. To avoid this, the MAC address used for the purpose of ID generation 
can be overridden by setting the IdGeneratorOptions in Startup.cs. For example, the MAC address can 
be set from a configuration property value:
```c#
services.Configure<IdGeneratorOptions>(o =>
{
    o.MacAddress = _configuration.GetValue<string>("EventuateMacAddress");
});

```
This would allow overriding the MAC address via configuration (e.g. by setting the 
`EventuateMacAddress` property within the appsettings.json file or via an environment variable).

Note that the specified "MAC address" should be a number specified as a base 10 integer value with a 
maximum of 140737488355327.

### Consuming Events (Option 1)

Create one or more event handler services that implements IDomainEventHandler<TEvent>. You could have 
one handler service per event type, or handle multiple event types in the same handler service:
```c#
public class ExampleEventHandler : IDomainEventHandler<Example1Event>, IDomainEventHandler<Example2Event>
{
    private readonly ExampleDbContext _dbContext;
    private readonly ILogger _logger;

    public ExampleEventHandler(ExampleDbContext dbContext, ILogger<ExampleEventHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public void Handle(IDomainEventEnvelope<Example1Event> @event)
    {
        _logger.LogDebug("Got message Example1Event with id={} and value={}", @event.EventId,
            @event.Event.ToString());
        _dbContext.DoSomething(...);
        _dbContext.SaveChanges();
    }

   public void Handle(IDomainEventEnvelope<Example2Event> @event)
    {
        _logger.LogDebug("Got message Example2Event with id={} and value={}", @event.EventId,
            @event.Event.ToString());
        _dbContext.DoSomething(...);
        _dbContext.SaveChanges();
    }
}
```

Register your event handler service(s) as well as a DomainEventDispatcher in Startup.cs:
```c#
services.AddScoped<ExampleEventHandler>();
services.AddEventuateTramDomainEventDispatcher(subscriberId,
    provider => DomainEventHandlersBuilder.ForAggregateType("ExampleAggregate")
        .OnEvent<Example1Event, ExampleEventHandler>()
        .OnEvent<Example2Event, ExampleEventHandler>()
        .Build());
```
The aggregate type will be used as the Kafka topic and the subscriberId will be used as the Kafka
consumer group.

#### Handling with DbContext EnableRetryOnFailure

Note that the Handle method is executed within a transaction scope and a new row is 
inserted into the received_messages table within that scope (used for message deduplication). 
If EnableRetryOnFailure is configured on a DbContext used by your handler, you 
will get the exception:
```Text
System.InvalidOperationException: The configured execution strategy 'SqlServerRetryingExecutionStrategy' does not 
support user initiated transactions. Use the execution strategy returned by 'DbContext.Database.CreateExecutionStrategy()' 
to execute all the operations in the transaction as a retriable unit.
```
To resolve this issue, the transaction scope needs to be wrapped in an ExecutionStrategy Execute() method. 
This can be accomplished by creating a message handler decorator configuring it to run prior to 
the DuplicateDetectingMessageHandlerDecorator:
```c#
public class EventuateExecutionStrategyMessageHandlerDecorator : IMessageHandlerDecorator, IOrdered
{
    public Action<SubscriberIdAndMessage, IServiceProvider, IMessageHandlerDecoratorChain> Accept =>
        (subscriberIdAndMessage, serviceProvider, chain) =>
        {
            var dbContext = serviceProvider.GetService<ApplicationDbContext>();
            IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
            executionStrategy.Execute(() =>
            {
                chain.InvokeNext(subscriberIdAndMessage, serviceProvider);
            });
        };

    public int Order => BuiltInMessageHandlerDecoratorOrder.DuplicateDetectingMessageHandlerDecorator - 1;
}
```
You will also need to register the EventuateExecutionStrategyMessageHandlerDecorator as a singleton:
```c#
services.AddSingleton<EventuateExecutionStrategyMessageHandlerDecorator>();
```

Finally, note that if EnableRetryOnFailure is configured, your handler may get called multiple times if there is a 
transient failure that causes a retry to occur.

### Consuming Events (Option 2)

Create an event consumer service and implement handlers for the different event types:
```c#
public class ExampleEventConsumer
{
    private readonly ILogger<ExampleEventConsumer> _logger;

    public ExampleEventConsumer(ILogger<ExampleEventConsumer> logger)
    {
        _logger = logger;
    }
    
    public DomainEventHandlers DomainEventHandlers()
    {
        return DomainEventHandlersBuilder.ForAggregateType("ExampleAggregate")
            .OnEvent<Example1Event>(HandleExample1Event)
            .OnEvent<Example2Event>(HandleExample2Event)
            .Build();
    }

    private void HandleExample1Event(IDomainEventEnvelope<Example1Event> @event)
    {
        _logger.LogDebug("Got Example1Event with id={} and value={}", @event.EventId,
            @event.Event.ToString());
    }

    private void HandleExample2Event(IDomainEventEnvelope<Example2Event> @event)
    {
        _logger.LogDebug("Got message Example2Event with id={} and value={}", @event.EventId,
            @event.Event.ToString());
    }
}
```
The aggregate type will be used as the Kafka topic.

Register your event consumer service as well as a DomainEventDispatcher in Startup.cs:
```c#
services.AddSingleton<ExampleEventConsumer>();
services.AddEventuateTramDomainEventDispatcher(subscriberId,
    provider =>
    {
        var consumer = provider.GetRequiredService<ExampleEventConsumer>();
        return consumer.DomainEventHandlers();
    });
```
The subscriberId will be used as the Kafka consumer group.

If EnableRetryOnFailure is configured on a DbContext used by your handler, 
see [Handling with DbContext EnableRetryOnFailure](#handling-with-dbcontext-enableretryonfailure).

## Running Integration Tests

### Services Required

The integration tests require a set of support services to be running and accessible.
The IntegrationTests folder contains a docker-compose.yml that can be used to start the
support services and run the tests.

You can use the following steps to run the tests:

```
$ cd IO.Eventuate.Tram.IntegrationTests
$ dotnet build -c Release
$ export CDC_SERVICE_DOCKER_VERSION=<CDC docker tag>
$ ./test.sh
```

Test results will be written to ./bin/Release/netcoreapp3.1/TestResults.

### Environment Variable Configuration

If running the tests from an IDE, set the following environment variables:

> `ConnectionStrings__EventuateTramDbConnection` identifies where to find the database for the outbox messages.  
> `KafkaBootstrapServers` identifies where the Kafka server is running.