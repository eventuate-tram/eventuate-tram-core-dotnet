using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Events.Common;
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
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg1 });
            GetDbContext().SaveChanges();

            // Allow time for messages to process
            int count = 10;
            while (consumer.Type1MessageCount < 1 && count > 0)
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
	        Assert.That(consumer.Type1MessageCount,
		        Is.EqualTo(1), "Number of Type 1 messages received by consumer");
	        Assert.That(consumer.ReceivedType1Messages.Count,
		        Is.EqualTo(1), "Number of received type 1 messages");
            msg1.AssertGoodMessageReceived(consumer.ReceivedType1Messages[0]);

            GetTestMessageInterceptor()?.AssertCounts(1, 1, 1, 1, 1, 1);
        }

        [Test]
        public void Publish_SingleSubscribedMessageType2_MessageReceived()
        {
            // Arrange
            TestMessageType2 msg2 = new TestMessageType2("Msg2", 2);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg2 });
            GetDbContext().SaveChanges();

            // Allow time for messages to process
            int count = 10;
            while (consumer.Type2MessageCount < 1 && count > 0)
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
	        Assert.That(consumer.Type2MessageCount,
		        Is.EqualTo(1), "Number of Type 2 messages received by consumer");
	        Assert.That(consumer.ReceivedType2Messages.Count,
		        Is.EqualTo(1), "Number of received type 2 messages");

			msg2.AssertGoodMessageReceived(consumer.ReceivedType2Messages[0]);

            GetTestMessageInterceptor()?.AssertCounts(1, 1, 1, 1, 1, 1);
        }

        [Test]
        public void Publish_MultipleSubscribedMessageTypes_AllMessagesReceived()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
            TestMessageType2 msg2 = new TestMessageType2("Msg2", 2);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg1 });
            GetDbContext().SaveChanges();
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg2 });
            GetDbContext().SaveChanges();

            // Allow time for messages to process
            int count = 10;
            while (consumer.TotalMessageCount() < 2 && count > 0)
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
		        Is.EqualTo(2), "Total number of messages received by consumer");
	        Assert.That(consumer.Type1MessageCount,
		        Is.EqualTo(1), "Number of Type 1 messages received by consumer");
	        Assert.That(consumer.Type2MessageCount,
		        Is.EqualTo(1), "Number of Type 2 messages received by consumer");
			Assert.That(consumer.ReceivedType1Messages.Count,
				Is.EqualTo(1), "Number of received type 1 messages");
	        Assert.That(consumer.ReceivedType2Messages.Count,
		        Is.EqualTo(1), "Number of received type 2 messages");
            msg1.AssertGoodMessageReceived(consumer.ReceivedType1Messages[0]);
            msg2.AssertGoodMessageReceived(consumer.ReceivedType2Messages[0]);

            GetTestMessageInterceptor()?.AssertCounts(2, 2, 2, 2, 2, 2);
        }

        [Test]
        public void Publish_SubscribedMessageTypeAndUnsubscribedMessageType_ReceivedOnlySubscribedMessageType()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
            TestMessageType3 msg3 = new TestMessageType3("Msg3");
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg3 });
            GetDbContext().SaveChanges();
            // Send a following message to identify when we're done
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg1 });
            GetDbContext().SaveChanges();

            // Allow time for messages to process
            int count = 10;
            while (consumer.Type1MessageCount < 1 && count > 0)
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
            GetTestPublisher().Publish("BadTopic", "BadTopic", new List<IDomainEvent> { msg1 });
            GetDbContext().SaveChanges();
            // Send a following message to identify when we're done
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg1 });
            GetDbContext().SaveChanges();

            // Allow time for messages to process
            int count = 10;
            while (consumer.Type1MessageCount < 1 && count > 0)
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
        public void Publish_SubscriberThrowsExceptionOn1OfMultipleMessages_AllMessagesHandled()
        {
            // Arrange
            TestMessageType1 badmsg1 = new TestMessageType1("ThrowException", 1, 1.2);
            TestMessageType2 msg2a = new TestMessageType2("Msg2a", 1);
            TestMessageType2 msg2b = new TestMessageType2("Msg2b", 2);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { badmsg1 });
            GetDbContext().SaveChanges();
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg2a });
            GetDbContext().SaveChanges();
            GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg2b });
            GetDbContext().SaveChanges();

            // Allow time for messages to process
            int count = 10;
            while (consumer.Type2MessageCount < 2 && count > 0)
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
				Is.EqualTo(3), "Number of received messages");
			Assert.That(consumer.TotalMessageCount(), 
				Is.EqualTo(3), "Total number of messages received by consumer");
	        Assert.That(consumer.Type1MessageCount,
		        Is.EqualTo(1), "Number of Type 1 messages received by consumer");
			Assert.That(consumer.Type2MessageCount,
		        Is.EqualTo(2), "Number of Type 2 messages received by consumer");
        }

	    [Test]
	    public void Publish_DefaultEventTypeName_CorrectEventTypeHeader()
	    {
		    // Arrange
		    TestMessageType1 msg1 = new TestMessageType1("Msg1", 2, 3.3);
		    TestEventConsumer consumer = GetTestConsumer();

		    // Act
		    GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg1 });
		    GetDbContext().SaveChanges();

		    // Allow time for messages to process
		    int count = 10;
		    while (consumer.Type1MessageCount < 1 && count > 0)
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
		    Assert.That(consumer.Type1MessageCount,
			    Is.EqualTo(1), "Number of Type 1 messages received by consumer");
		    Assert.That(consumer.ReceivedType1Messages.Count,
			    Is.EqualTo(1), "Number of received type 1 messages");

		    msg1.AssertGoodMessageReceived(consumer.ReceivedType1Messages[0]);
		    Assert.That(consumer.ReceivedType1Messages[0].Message.GetHeader(EventMessageHeaders.EventType),
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
		    GetTestPublisher().Publish(AggregateType, AggregateType, new List<IDomainEvent> { msg2 });
		    GetDbContext().SaveChanges();

		    // Allow time for messages to process
		    int count = 10;
		    while (consumer.Type2MessageCount < 1 && count > 0)
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
		    Assert.That(consumer.Type2MessageCount,
			    Is.EqualTo(1), "Number of Type 2 messages received by consumer");
		    Assert.That(consumer.ReceivedType2Messages.Count,
			    Is.EqualTo(1), "Number of received type 2 messages");

		    msg2.AssertGoodMessageReceived(consumer.ReceivedType2Messages[0]);
		    Assert.That(consumer.ReceivedType2Messages[0].Message.GetHeader(EventMessageHeaders.EventType),
			    Is.EqualTo(TestMessageType2.EventTypeName), "Event type header");

		    GetTestMessageInterceptor()?.AssertCounts(1, 1, 1, 1, 1, 1);
	    }
    }
}
