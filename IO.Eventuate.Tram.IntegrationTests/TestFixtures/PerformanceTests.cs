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
    [TestFixture]
    public class PerformanceTests : IntegrationTestsBase
    {
        [SetUp]
        public async Task Setup()
        {
            await CleanupKafkaTopics();
            TestSetup("eventuate", false, EventuateKafkaConsumerConfigurationProperties.Empty());
            CleanupTest();
        }

        [TearDown]
        public void TearDown()
        {
            DisposeTestHost();
        }

        [Test]
        public void Send1000Message_Within1Minute()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
            TestEventConsumer consumer = GetTestConsumer();
            TestEventConsumer.EventStatistics type1Statistics = consumer.GetEventStatistics(
                typeof(TestMessageType1));

            // Act
            for (int x = 0; x < 1000; x++)
            {
                GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> { msg1 });
            }

            // Allow time for messages to process
            int count = 300;
            while (type1Statistics.MessageCount < 1000 && count > 0)
            {
                Thread.Sleep(1000);
                count--;
            }

            ShowTestResults();

            // Assert
            Assert.AreEqual(1000, GetDbContext().Messages.Count(), "Expect 1000 messages produced");
            Assert.AreEqual(1000, type1Statistics.MessageCount, "Received by consumer count must be 1000");
            Assert.AreEqual(0, GetDbContext().Messages.Count(msg => msg.Published == 0), "No unpublished messages");
            Assert.AreEqual(1000, GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null), "Expect 1000 messages received");
            Assert.Less(type1Statistics.GetDuration().TotalSeconds, 60.0, "Time to send 1000 messages");

            TestContext.WriteLine("Performance Test completed in {0} seconds",
                type1Statistics.GetDuration().TotalSeconds);
        }
    }
}
