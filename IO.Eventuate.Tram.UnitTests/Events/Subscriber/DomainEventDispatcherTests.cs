using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private String _subscriberId;
        private static String aggregateType = "AggregateType";

        private String aggregateId = "xyz";
        private String messageId = "message-" + DateTime.Now;

        public class MyTarget
        {
            public ConcurrentQueue<IDomainEventEnvelope<IDomainEvent>> Queue = new ConcurrentQueue<IDomainEventEnvelope<IDomainEvent>>();

            public DomainEventHandlers domainEventHandlers()
            {
                return DomainEventHandlersBuilder
                    .ForAggregateType(aggregateType)
                    .OnEvent<MyDomainEvent>(HandleAccountDebited)
                    .Build();
            }

            internal void HandleAccountDebited(IDomainEventEnvelope<MyDomainEvent> message)
            {
                Queue.Enqueue(message);
            }

        }

        public class MyDomainEvent : IDomainEvent
        {
        }

        [Test]
        public void MessageHandler_ValidMessage_RegisteredHandlerCalled()
        {
			// Arrange
            MyTarget target = new MyTarget();

            var messageConsumer = Substitute.For<IMessageConsumer>();
            var serviceProvider = Substitute.For<IServiceProvider>();
	        var logger = Substitute.For<ILogger<DomainEventDispatcher>>();
            var eventTypeNamingStrategy = Substitute.For<IEventTypeNamingStrategy>();
            eventTypeNamingStrategy.GetEventTypeName(typeof(MyDomainEvent)).Returns(typeof(MyDomainEvent).FullName);

            DomainEventDispatcher dispatcher = new DomainEventDispatcher(
	            _subscriberId, target.domainEventHandlers(), messageConsumer, eventTypeNamingStrategy, logger);

            dispatcher.Initialize();

			// Act
            dispatcher.MessageHandler(DomainEventPublisher.MakeMessageForDomainEvent(aggregateType,
                aggregateId, new Dictionary<string, string>() {{ MessageHeaders.Id, messageId } },
                new MyDomainEvent(), eventTypeNamingStrategy), serviceProvider);

			// Assert
            Assert.True(target.Queue.TryPeek(out var dee));
            Assert.NotNull(dee);
            Assert.AreEqual(aggregateId, dee.AggregateId);
            Assert.AreEqual(aggregateType, dee.AggregateType);
            Assert.AreEqual(messageId, dee.EventId);
        }
    }
}
