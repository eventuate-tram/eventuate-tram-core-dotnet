using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.IntegrationTests.TestHelpers;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestFixtures;

public class BackpressureTests : IntegrationTestsBase
{
	private const uint PauseThreshold = 25;
	private const uint ResumeThreshold = 5;
	
	[SetUp]
	public async Task Setup()
	{
		await CleanupKafkaTopics();
		// Initialize the backpressure properties
		var properties = new EventuateKafkaConsumerConfigurationProperties
		{
			BackPressure =
			{
				PauseThreshold = PauseThreshold,
				ResumeThreshold = ResumeThreshold
			}
		};
		TestSetup("eventuate", false, properties);
		CleanupTest();
	}

	[TearDown]
	public void TearDown()
	{
		DisposeTestHost();
	}

	[Test]
	public void PublishWithBackpressure_Send1000Messages_AllMessagesEventuallyProcessed()
	{
		// Arrange
		uint numberOfMessages = 1000;
		TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
		for (int x = 0; x < numberOfMessages; x++)
		{
			GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> { msg1 });
		}

		// Act
		TestEventConsumer consumer = GetTestConsumer();
		TestEventConsumer.EventStatistics type1Statistics = consumer.GetEventStatistics(
			typeof(TestMessageType1));

		// Allow time for messages to process
		int count = 300;
		while (type1Statistics.MessageCount < numberOfMessages && count > 0)
		{
			Thread.Sleep(1000);
			count--;
		}

		ShowTestResults();

		// Assert
		Assert.AreEqual(numberOfMessages, GetDbContext().Messages.Count(),
			$"Expect {numberOfMessages} messages produced");
		Assert.AreEqual(numberOfMessages, type1Statistics.MessageCount,
			$"Received by consumer count must be {numberOfMessages}");
		Assert.AreEqual(0, GetDbContext().Messages.Count(msg => msg.Published == 0), "No unpublished messages");
		Assert.AreEqual(numberOfMessages, GetDbContext().ReceivedMessages.Count(msg => msg.MessageId != null),
			$"Expect {numberOfMessages} messages received");

		TestContext.WriteLine("Performance Test completed in {0} seconds",
			type1Statistics.GetDuration().TotalSeconds);
	}
}