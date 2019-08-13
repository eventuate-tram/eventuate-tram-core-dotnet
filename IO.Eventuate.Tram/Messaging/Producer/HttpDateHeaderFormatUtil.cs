/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.producer
 */

using System;

namespace IO.Eventuate.Tram.Messaging.Producer
{
	public static class HttpDateHeaderFormatUtil
	{
		public static string NowAsHttpDateString()
		{
			// Use RFC 1123 format
			return DateTimeOffset.UtcNow.ToString("R");
		}
	}
}