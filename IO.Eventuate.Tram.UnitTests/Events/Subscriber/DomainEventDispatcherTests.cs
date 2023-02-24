using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Publisher;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Consumer;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace IO.Eventuate.Tram.UnitTests.Events.Subscriber
{
    public class DomainEventDispatcherTests
    {
        private const String SubscriberId = "123ABC";
        private const String AggregateType = "AggregateType";

        private const String AggregateId = "xyz";
        private readonly String _messageId = "message-" + DateTime.Now;

        private class MyTarget
        {
            public readonly ConcurrentQueue<IDomainEventEnvelope<IDomainEvent>> Queue = new();

            public DomainEventHandlers DomainEventHandlers()
            {
                return DomainEventHandlersBuilder
                    .ForAggregateType(AggregateType)
                    .OnEvent<MyDomainEvent>(HandleAccountDebitedAsync)
                    .Build();
            }

            private Task HandleAccountDebitedAsync(IDomainEventEnvelope<MyDomainEvent> message)
            {
                Queue.Enqueue(message);
                return Task.CompletedTask;
            }

        }

        private class MyDomainEvent : IDomainEvent
        {
        }

        [Test]
        public async Task MessageHandler_ValidMessage_RegisteredHandlerCalled()
        {
			// Arrange
            MyTarget target = new MyTarget();

            var messageConsumer = Substitute.For<IMessageConsumer>();
            var serviceProvider = Substitute.For<IServiceProvider>();
	        var logger = Substitute.For<ILogger<DomainEventDispatcher>>();
            var eventTypeNamingStrategy = Substitute.For<IEventTypeNamingStrategy>();
            eventTypeNamingStrategy.GetEventTypeName(typeof(MyDomainEvent)).Returns(typeof(MyDomainEvent).FullName);

            DomainEventDispatcher dispatcher = new DomainEventDispatcher(
	            SubscriberId, target.DomainEventHandlers(), messageConsumer, eventTypeNamingStrategy, logger);

            dispatcher.Initialize();

			// Act
            await dispatcher.MessageHandlerAsync(DomainEventPublisher.MakeMessageForDomainEvent(AggregateType,
                AggregateId, new Dictionary<string, string> {{ MessageHeaders.Id, _messageId } },
                new MyDomainEvent(), eventTypeNamingStrategy), serviceProvider);

			// Assert
            Assert.True(target.Queue.TryPeek(out var dee));
            Assert.NotNull(dee);
            Assert.AreEqual(AggregateId, dee.AggregateId);
            Assert.AreEqual(AggregateType, dee.AggregateType);
            Assert.AreEqual(_messageId, dee.EventId);
        }
    }
}
