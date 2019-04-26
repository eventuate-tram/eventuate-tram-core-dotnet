# Eventuate Tram (Transactional Messaging) framework
A .NET port of https://github.com/eventuate-tram/eventuate-tram-core

## Usage

First you need to register the EventuateTramDbContext in Startup.cs. You probably want use the same
DbConnection from another DbContext (e.g. AppDbContext) to allow for events to be published
and consumed within the same transaction as other state changes are saved. This can be done as
follows:
```c#
services.AddDbContext<EventuateTramDbContext>((provider, o) =>
{
    // Share the same connection as AppDbContext
    var appDbContext = provider.GetRequiredService<AppDbContext>();
    o.UseSqlServer(appDbContext.Database.GetDbConnection());
});
```

Next you need to register the core Eventuate Tram services in Startup.cs:
```c#
services.AddEventuateTramSqlKafkaTransport(dbSchemaName, bootstrapServers,
    eventuateKafkaConsumerConfigurationProperties);
```
To use the default eventuateKafkaConsumerConfigurationProperties, just pass in EventuateKafkaConsumerConfigurationProperties.Empty()

Next you need to run the database initialization script (https://github.build.ge.com/plantapps/eventuate-tram-core/blob/ms-sql-support/mssql/initialize-database.sql),
modifying the database schema to match what you configured in the previous step. 

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
You also need to save the changes to the EventuateTramDbContext (probably within the same
transaction as other changes in another DbContext, e.g. AppDbContext):
```c#
using (IDbContextTransaction transaction = await _AppDbContext.Database.BeginTransactionAsync(stoppingToken))
{
    await _AppDbContext.SaveChangesAsync(stoppingToken);
    _eventuateTramDbContext.Database.UseTransaction(transaction.GetDbTransaction());
    await _eventuateTramDbContext.SaveChangesAsync(stoppingToken);
    transaction.Commit();
}
```
Alternatively, you could use a TransactionScope instead of the explicit BeginTransactionAsync.

If EnableRetryOnFailure is enabled on your DbContext, you will need to wrap the transaction in
an ExecutionStrategy ExecuteAsync():
```c#
IExecutionStrategy strategy = _AppDbContext.Database.CreateExecutionStrategy();
rowCount = await strategy.ExecuteAsync(async (stoppingToken) =>
{
    using (IDbContextTransaction transaction = await _AppDbContext.Database.BeginTransactionAsync(stoppingToken))
    {
        int count = await _AppDbContext.SaveChangesAsync(stoppingToken);
        _eventuateTramDbContext.Database.UseTransaction(transaction.GetDbTransaction());
        await _eventuateTramDbContext.SaveChangesAsync(stoppingToken);
        transaction.Commit();
        return count;
    }
}, cancellationToken);

```

Finally, the Eventuate Tram CDC service will need to be running and monitoring your database
for new events to publish. This is easiest to do by running it in a docker container. 

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

Note that the Handle method is executed within a transaction scope and uses the execution strategy configured
on the EventuateTramDbContext. If EnableRetryOnFailure is configured on a DbContext used by your handler, you should
also set EnableRetryOnFailure on EventuateTramDbContext. Otherwise, you will get the exception:
```c#
System.InvalidOperationException: The configured execution strategy 'SqlServerRetryingExecutionStrategy' does not 
support user initiated transactions. Use the execution strategy returned by 'DbContext.Database.CreateExecutionStrategy()' 
to execute all the operations in the transaction as a retriable unit.
```
Also, note that if EnableRetryOnFailure is configured, your handler may get called multiple times if there is a 
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

Note that the handle method is executed within a transaction scope and uses the execution strategy configured
on the EventuateTramDbContext. If EnableRetryOnFailure is configured on a DbContext used by your handler, you should
also set EnableRetryOnFailure on EventuateTramDbContext. Otherwise, you will get the exception:
```c#
System.InvalidOperationException: The configured execution strategy 'SqlServerRetryingExecutionStrategy' does not 
support user initiated transactions. Use the execution strategy returned by 'DbContext.Database.CreateExecutionStrategy()' 
to execute all the operations in the transaction as a retriable unit.
```
Also, note that if EnableRetryOnFailure is configured, your handler may get called multiple times if there is a 
transient failure that causes a retry to occur.

## Running Integration Tests

### Services Required

The integration tests require a set of support services to be running and accessible.
The IntegrationTests folder contains a docker-compose.yml that can be used to start the
support services and run the tests.

You can use the following steps to run the tests:

```
$ cd IO.Eventuate.Tram.IntegrationTests
$ dotnet publish -c Release
$ export CDC_SERVICE_DOCKER_IMAGE=<CDC docker image to use>
$ export CDC_SERVICE_DOCKER_VERSION=<CDC docker tag>
$ docker-compose down
$ docker-compose build
$ docker-compose up -d mssql
$ docker-compose up --exit-code-from dbsetup dbsetup
$ docker-compose up -d zookeeper
$ docker-compose up -d kafka
$ docker-compose up -d cdcservice1
$ docker-compose up -d cdcservice2
$ docker-compose up eventuatetramtests
```

Test results will be written to ./bin/Release/netcoreapp2.2/publish/TestResults.

### Environment Variable Configuration

If running the tests from an IDE, set the following environment variables:

> `ConnectionStrings__EventuateTramDbConnection` identifies where to find the database for the outbox messages.  
> `KafkaBootstrapServers` identifies where the Kafka server is running.