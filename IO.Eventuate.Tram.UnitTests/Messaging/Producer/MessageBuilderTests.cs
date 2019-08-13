using NUnit.Framework;
using System.Collections.Generic;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Producer;

namespace IO.Eventuate.Tram.UnitTests.Messaging.Producer
{
	public class MessageBuilderTests
	{
		[Test]
		public void WithMessage_MessageWithHeaders_MessageAndHeadersDuplicated()
		{
			// Arrange

			// Act
			Message sourceMessage = new Message("The payload", new Dictionary<string, string>
			{
				{"Header1", "HeaderValue" },
				{MessageHeaders.Id, "MyMessage" }
			});
			Message builtMessage = MessageBuilder.WithMessage(sourceMessage).Build();

			// Assert
			Assert.That(builtMessage, Is.Not.Null, "Built message");
			Assert.That(builtMessage.Payload, Is.EqualTo(sourceMessage.Payload), "Payload");
			Assert.That(builtMessage.Id, Is.EqualTo(sourceMessage.Id), "Id");
			Assert.That(builtMessage.Headers, Is.EquivalentTo(sourceMessage.Headers), "Headers");
		}
	}
}
