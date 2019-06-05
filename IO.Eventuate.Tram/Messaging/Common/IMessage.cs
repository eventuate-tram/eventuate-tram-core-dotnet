/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.common
 */

using System.Collections.Generic;

namespace IO.Eventuate.Tram.Messaging.Common
{
	public interface IMessage
	{
		string Id { get; }
		IDictionary<string, string> Headers { get; set; }
		string Payload { get; set; }

		string GetHeader(string name);
		string GetRequiredHeader(string name);

		bool HasHeader(string name);

		void SetHeader(string name, string value);
		void RemoveHeader(string key);		
	}
}