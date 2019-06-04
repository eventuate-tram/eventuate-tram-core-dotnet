/* Copyright (c) 2019 GE Digital. All rights reserved.
 *
 * The copyright to the computer software herein is the property of GE Digital.
 * The software may be used and/or copied only with the written permission of
 * GE Digital or in accordance with the terms and conditions stipulated in the
 * agreement/contract under which the software has been supplied.
 */

using System;
using IO.Eventuate.Tram.Events.Common;
using IO.Eventuate.Tram.Events.Subscriber;
using IO.Eventuate.Tram.Messaging.Common;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    [EventType(EventTypeName)]
    public class TestMessageType4 : TestMessageType2
    {
        public new const string EventTypeName = "testing.TestMessageType4";
        
        public TestMessageType4(String name, int value)
		: base(name, value)
        {
        }

        public void AssertGoodMessageReceived(IDomainEventEnvelope<TestMessageType4> receivedMessage)
        {
            Assert.True(receivedMessage.Message.HasHeader(MessageHeaders.Id), "Message ID is in the header");
            Assert.AreEqual(Name, receivedMessage.Event.Name, "Message Name is the same");
            Assert.AreEqual(Value, receivedMessage.Event.Value, "Message Value is the same");
        }
    }
}
