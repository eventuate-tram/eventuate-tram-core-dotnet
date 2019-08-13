using System;
using IO.Eventuate.Tram.Events.Common;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    [EventType(EventTypeName)]
    public class TestMessageType3 : TestMessageType2
    {
        public new const string EventTypeName = "testing.TestMessageType3";
        
        public TestMessageType3(String name, int value)
		: base(name, value)
        {
        }
    }
}
