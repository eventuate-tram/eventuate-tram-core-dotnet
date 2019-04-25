using System;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Messaging.Consumer
{
	public delegate void MessageHandler(IMessage message, IServiceProvider serviceProvider);
}