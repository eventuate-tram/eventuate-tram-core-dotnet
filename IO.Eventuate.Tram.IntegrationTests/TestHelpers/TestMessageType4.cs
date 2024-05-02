/* Copyright (c) 2019 GE Digital. All rights reserved.
 *
 * The copyright to the computer software herein is the property of GE Digital.
 * The software may be used and/or copied only with the written permission of
 * GE Digital or in accordance with the terms and conditions stipulated in the
 * agreement/contract under which the software has been supplied.
 */

using System;
using IO.Eventuate.Tram.Events.Common;

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
	}
}