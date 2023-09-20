using System.Collections.Generic;
using IO.Eventuate.Tram.Messaging.Common;
using IO.Eventuate.Tram.Messaging.Producer;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace IO.Eventuate.Tram.UnitTests.Messaging.Producer
{
    public class AbstractMessageProducerTests
    {

        public class MyMessageProducer : AbstractMessageProducer
        {
            public MyMessageProducer(IEnumerable<IMessageInterceptor> messageInterceptors,
	            ILogger logger)
                : base(messageInterceptors, logger)
            {

            }

            public void Send(string destination, IMessage message, IMessageSender ms)
            {
                SendMessage("id", destination, message, ms);
            }


        }

        [Test]
        public void Send_SimpleMessage_MessageHeadersAreApplied()
        {
			// Arrange
            Message sendMessage = null;

            var ms = Substitute.For<IMessageSender>();
            ms.Send(Arg.Do<Message>(arg => sendMessage = arg));

			// Act
            MyMessageProducer mp = new MyMessageProducer(new List<IMessageInterceptor>(),
	            Substitute.For<ILogger>());
            mp.Send("Destination", MessageBuilder.WithPayload("x").Build(), ms);

			// Assert
            Assert.NotNull(sendMessage);
            Assert.NotNull(sendMessage.GetRequiredHeader(MessageHeaders.Id));
            Assert.NotNull(sendMessage.GetRequiredHeader(MessageHeaders.Destination));
            Assert.NotNull(sendMessage.GetRequiredHeader(MessageHeaders.Date));
        }
    }
}