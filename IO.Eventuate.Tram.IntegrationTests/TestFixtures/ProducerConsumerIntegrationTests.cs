using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.IntegrationTests.TestHelpers;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestFixtures
{
    [TestFixture("eventuate")]
    [TestFixture("schema1")]
    public class ProducerConsumerIntegrationTests : IntegrationTestsBase
    {
        private readonly string _schema;

        public ProducerConsumerIntegrationTests(string schema)
        {
            _schema = schema;
        }

        [SetUp]
        public async Task Setup()
        {
            await CleanupKafka();
            TestSetup(_schema, true, EventuateKafkaConsumerConfigurationProperties.Empty());
            CleanupTest();
        }

        [TearDown]
        public void TearDown()
        {
            DisposeTestHost();
        }

        [Test]
        public void Publish_SingleSubscribedMessageType1_MessageReceived()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
            TestEventConsumer.EventStatistics eventStatistics = GetTestConsumer().GetEventStatistics(
                typeof(TestMessageType1));

            // Act
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg1});

            // Allow time for messages to process
            AssertMessagesArePublishedAndConsumed(eventStatistics);

            msg1.AssertGoodMessageReceived(eventStatistics.ReceivedMessages[0]);

            GetTestMessageInterceptor()?.AssertCounts(1, 1, 1, 1, 1, 1);
        }

        [Test]
        public void Publish_SingleSubscribedMessageType2_MessageReceived()
        {
            // Arrange
            TestMessageType2 msg2 = new TestMessageType2("Msg2", 2);
            TestEventConsumer.EventStatistics eventStatistics = GetTestConsumer().GetEventStatistics(
                typeof(TestMessageType2));

            // Act
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg2});

            // Allow time for messages to process
            AssertMessagesArePublishedAndConsumed(eventStatistics);

            msg2.AssertGoodMessageReceived(
                (IDomainEventEnvelope<TestMessageType2>) eventStatistics.ReceivedMessages[0]);

            GetTestMessageInterceptor()?.AssertCounts(1, 1, 1, 1, 1, 1);
        }

        [Test]
        public void Publish_MultipleSubscribedMessageTypes_AllMessagesReceived()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
            TestMessageType2 msg2 = new TestMessageType2("Msg2", 2);
            TestMessageType3 msg3 = new TestMessageType3("Msg3", 3);
            TestMessageType4 msg4 = new TestMessageType4("Msg4", 4);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg1});
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg2});
            GetTestPublisher().Publish(AggregateType34, AggregateType34, new List<IDomainEvent> {msg3});
            GetTestPublisher().Publish(AggregateType34, AggregateType34, new List<IDomainEvent> {msg4});

            // Allow time for messages to process
            int count = 10;
            while (consumer.TotalMessageCount() < 4 && count > 0)
            {
                Thread.Sleep(1000);
                count--;
            }

            ShowTestResults();

            // Assert
            Assert.That(GetDbContext().Messages.Count(),
                Is.EqualTo(4), "Number of messages produced");
            Assert.That(GetDbContext().Messages.Count(msg => msg.Published == 0),
                Is.EqualTo(0), "Number of unpublished messages");
            foreach (Type eventType in consumer.GetEventTypes())
            {
                TestEventConsumer.EventStatistics eventStatistics = consumer.GetEventStatistics(eventType);
                Assert.That(eventStatistics.MessageCount,
                    Is.EqualTo(1), $"Number of {eventType.Name} messages received by consumer");
                Assert.That(eventStatistics.ReceivedMessages.Count,
                    Is.EqualTo(1), $"Number of received {eventType.Name} messages");

            }

            Assert.That(consumer.TotalMessageCount(),
                Is.EqualTo(4), "Total number of messages received by consumer");
            Assert.That(GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null),
                Is.EqualTo(4), "Number of received messages");
            msg1.AssertGoodMessageReceived(consumer.GetEventStatistics(typeof(TestMessageType1)).ReceivedMessages[0]);
            msg2.AssertGoodMessageReceived(consumer.GetEventStatistics(typeof(TestMessageType2)).ReceivedMessages[0]);
            msg3.AssertGoodMessageReceived(consumer.GetEventStatistics(typeof(TestMessageType3)).ReceivedMessages[0]);
            msg4.AssertGoodMessageReceived(consumer.GetEventStatistics(typeof(TestMessageType4)).ReceivedMessages[0]);

            GetTestMessageInterceptor()?.AssertCounts(4, 4, 4, 4, 4, 4);
        }

        [Test]
        public void Publish_SubscribedMessageTypeAndUnsubscribedMessageType_ReceivedOnlySubscribedMessageType()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
            TestMessageUnsubscribedType unsubscribedMessage = new TestMessageUnsubscribedType("Msg3");
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {unsubscribedMessage});
            // Send a following message to identify when we're done
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg1});

            // Allow time for messages to process
            int count = 10;
            TestEventConsumer.EventStatistics type1Statistics = consumer.GetEventStatistics(typeof(TestMessageType1));
            while (type1Statistics.MessageCount < 1 && count > 0)
            {
                Thread.Sleep(1000);
                count--;
            }

            ShowTestResults();

            // Assert
            Assert.That(GetDbContext().Messages.Count(),
                Is.EqualTo(2), "Number of messages produced");
            Assert.That(GetDbContext().Messages.Count(msg => msg.Published == 0),
                Is.EqualTo(0), "Number of unpublished messages");
            Assert.That(GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null),
                Is.EqualTo(2), "Number of received messages");
            Assert.That(consumer.TotalMessageCount(),
                Is.EqualTo(1), "Total number of messages received by consumer");

            GetTestMessageInterceptor()?.AssertCounts(2, 2, 2, 2, 2, 2);
        }

        [Test]
        public void Publish_SubscribedTopicAndUnsubscribedTopic_ReceivesOnlySubscribedMessage()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish("BadTopic", "BadTopic", new List<IDomainEvent> {msg1});
            // Send a following message to identify when we're done
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg1});

            // Allow time for messages to process
            int count = 10;
            TestEventConsumer.EventStatistics type1Statistics = consumer.GetEventStatistics(typeof(TestMessageType1));
            while (type1Statistics.MessageCount < 1 && count > 0)
            {
                Thread.Sleep(1000);
                count--;
            }

            ShowTestResults();

            // Assert
            Assert.That(GetDbContext().Messages.Count(),
                Is.EqualTo(2), "Number of messages produced");
            Assert.That(GetDbContext().Messages.Count(msg => msg.Published == 0),
                Is.EqualTo(0), "Number of unpublished messages");
            Assert.That(GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null),
                Is.EqualTo(1), "Number of received messages");
            Assert.That(consumer.TotalMessageCount(),
                Is.EqualTo(1), "Total number of messages received by consumer");

            GetTestMessageInterceptor()?.AssertCounts(2, 2, 1, 1, 1, 1);
        }

        [Test]
        public void Publish_SubscriberThrowsExceptionOnFirstOfMultipleMessages_MessagesHandlingStops()
        {
            // Arrange
            TestMessageType1 badmsg1 = new TestMessageType1("ThrowException", 1, 1.2);
            TestMessageType2 msg2A = new TestMessageType2("Msg2a", 1);
            TestMessageType2 msg2B = new TestMessageType2("Msg2b", 2);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {badmsg1});
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg2A});
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg2B});

            // Allow time for messages to process
            int count = 10;
            TestEventConsumer.EventStatistics type2Statistics = consumer.GetEventStatistics(typeof(TestMessageType2));
            while (type2Statistics.MessageCount < 2 && count > 0)
            {
                Thread.Sleep(1000);
                count--;
            }

            ShowTestResults();

            // Assert
            Assert.That(GetDbContext().Messages.Count(),
                Is.EqualTo(3), "Number of messages produced");
            Assert.That(GetDbContext().Messages.Count(msg => msg.Published == 0),
                Is.EqualTo(0), "Number of unpublished messages");
            Assert.That(GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null),
                Is.EqualTo(0), "Number of received messages");
            Assert.That(consumer.TotalMessageCount(),
                Is.EqualTo(1), "Total number of messages received by consumer");
            Assert.That(consumer.GetEventStatistics(typeof(TestMessageType1)).MessageCount,
                Is.EqualTo(1), "Number of Type 1 messages received by consumer");
            Assert.That(consumer.GetEventStatistics(typeof(TestMessageType2)).MessageCount,
                Is.EqualTo(0), "Number of Type 2 messages received by consumer");
        }

        [Test]
        public void Publish_DefaultEventTypeName_CorrectEventTypeHeader()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 2, 3.3);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg1});

            // Allow time for messages to process
            int count = 10;
            TestEventConsumer.EventStatistics type1Statistics = consumer.GetEventStatistics(typeof(TestMessageType1));
            while (type1Statistics.MessageCount < 1 && count > 0)
            {
                Thread.Sleep(1000);
                count--;
            }

            ShowTestResults();

            // Assert
            Assert.That(GetDbContext().Messages.Count(),
                Is.EqualTo(1), "Number of messages produced");
            Assert.That(GetDbContext().Messages.Count(msg => msg.Published == 0),
                Is.EqualTo(0), "Number of unpublished messages");
            Assert.That(GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null),
                Is.EqualTo(1), "Number of received messages");
            Assert.That(type1Statistics.MessageCount,
                Is.EqualTo(1), "Number of Type 1 messages received by consumer");
            Assert.That(type1Statistics.ReceivedMessages.Count,
                Is.EqualTo(1), "Number of received type 1 messages");

            msg1.AssertGoodMessageReceived(type1Statistics.ReceivedMessages[0]);
            Assert.That(type1Statistics.ReceivedMessages[0].Message.GetHeader(EventMessageHeaders.EventType),
                Is.EqualTo(typeof(TestMessageType1).FullName), "Event type header");

            GetTestMessageInterceptor()?.AssertCounts(1, 1, 1, 1, 1, 1);
        }

        [Test]
        public void Publish_CustomEventTypeName_CorrectEventTypeHeader()
        {
            // Arrange
            TestMessageType2 msg2 = new TestMessageType2("Msg2", 2);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> {msg2});

            // Allow time for messages to process
            int count = 10;
            TestEventConsumer.EventStatistics type2Statistics = consumer.GetEventStatistics(typeof(TestMessageType2));
            while (type2Statistics.MessageCount < 1 && count > 0)
            {
                Thread.Sleep(1000);
                count--;
            }

            ShowTestResults();

            // Assert
            Assert.That(GetDbContext().Messages.Count(),
                Is.EqualTo(1), "Number of messages produced");
            Assert.That(GetDbContext().Messages.Count(msg => msg.Published == 0),
                Is.EqualTo(0), "Number of unpublished messages");
            Assert.That(GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null),
                Is.EqualTo(1), "Number of received messages");
            Assert.That(type2Statistics.MessageCount,
                Is.EqualTo(1), "Number of Type 2 messages received by consumer");
            Assert.That(type2Statistics.ReceivedMessages.Count,
                Is.EqualTo(1), "Number of received type 2 messages");

            msg2.AssertGoodMessageReceived(type2Statistics.ReceivedMessages[0]);
            Assert.That(type2Statistics.ReceivedMessages[0].Message.GetHeader(EventMessageHeaders.EventType),
                Is.EqualTo(TestMessageType2.EventTypeName), "Event type header");

            GetTestMessageInterceptor()?.AssertCounts(1, 1, 1, 1, 1, 1);
        }

        private void AssertMessagesArePublishedAndConsumed(TestEventConsumer.EventStatistics eventStatistics)
        {
            int count = 10;
            while (eventStatistics.MessageCount < 1 && count > 0)
            {
                Thread.Sleep(1000);
                count--;
            }

            ShowTestResults();

            // Assert
            Assert.That(GetDbContext().Messages.Count(),
                Is.EqualTo(1), "Number of messages produced");
            Assert.That(GetDbContext().Messages.Count(msg => msg.Published == 0),
                Is.EqualTo(0), "Number of unpublished messages");
            Assert.That(GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null),
                Is.EqualTo(1), "Number of received messages");
            Assert.That(eventStatistics.MessageCount,
                Is.EqualTo(1), "Number of Type 1 messages received by consumer");
            Assert.That(eventStatistics.ReceivedMessages.Count,
                Is.EqualTo(1), "Number of received type 1 messages");
        }
    }
}
