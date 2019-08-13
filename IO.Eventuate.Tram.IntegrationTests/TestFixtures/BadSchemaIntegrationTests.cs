using System.Collections.Generic;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.IntegrationTests.TestHelpers;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;

namespace IO.Eventuate.Tram.IntegrationTests.TestFixtures
{
    [TestFixture]
    public class BadSchemaIntegrationTests : IntegrationTestsBase
    {
        [SetUp]
        public void Setup()
        {
            TestSetup("badschema", false, EventuateKafkaConsumerConfigurationProperties.Empty());
        }

        [TearDown]
        public void TearDown()
        {
            DisposeTestHost();
        }

        [Test]
        public void Publish_DatabaseSchemaNotCreated_ThrowsException()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            Assert.Throws<DbUpdateException>(delegate ()
            {
                GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> { msg1 });
            });
        }
    }
}
