using System;
using IO.Eventuate.Tram.Events.Common;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    public class TestMessageType3 : IDomainEvent
    {
        public String Name { get; set; }
        public TestMessageType3(String name)
        {
            this.Name = name;
        }
    }
}
