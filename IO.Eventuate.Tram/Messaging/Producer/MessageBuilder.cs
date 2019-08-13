/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.producer
 */

using System.Collections.Generic;
using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Messaging.Producer
{
	public class MessageBuilder
	{
		protected readonly string Body;
		protected readonly IDictionary<string, string> Headers = new Dictionary<string, string>();

		protected MessageBuilder()
		{
			
		}

		public MessageBuilder(string body)
		{
			Body = body;
		}

		public MessageBuilder(IMessage message) : this(message.Payload)
		{
			Headers = message.Headers;
		}
		
		public static MessageBuilder WithPayload(string payload) {
			return new MessageBuilder(payload);
		}

		public MessageBuilder WithHeader(string name, string value) {
			Headers[name] = value;
			return this;
		}

		public MessageBuilder WithExtraHeaders(string prefix, IDictionary<string, string> headers) {

			foreach (KeyValuePair<string,string> pair in headers)
			{
				Headers[prefix + pair.Key] = pair.Value;
			}

			return this;
		}

		public Message Build() {
			return new Message(Body, Headers);
		}

		public static MessageBuilder WithMessage(Message message) {
			return new MessageBuilder(message);
		}		
	}
}