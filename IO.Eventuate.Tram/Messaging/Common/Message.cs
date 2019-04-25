using System;
using System.Collections.Generic;

namespace IO.Eventuate.Tram.Messaging.Common
{
	public class Message : IMessage
	{
		public Message()
		{
			
		}

		public Message(string payload, IDictionary<string, string> headers)
		{
			Payload = payload;
			Headers = headers;
		}

		/// <summary>
		/// Id header.
		/// Return null instead of exception of not present otherwise logging
		/// and other users have issues.
		/// </summary>
		public string Id => GetHeader(MessageHeaders.Id);

		public IDictionary<string, string> Headers { get; set; }
		
		public string Payload { get; set; }
		
		public string GetHeader(string name)
		{
			if (Headers == null)
			{
				return null;
			}
			
			return Headers.TryGetValue(name, out string value) ? value : null;
		}

		public string GetRequiredHeader(string name)
		{
			string value = GetHeader(name);
			if (value == null)
			{
				throw new ArgumentException($"No such header: {name} in this message {this}", nameof(name));
			}

			return value;
		}

		public bool HasHeader(string name)
		{
			return Headers != null && Headers.ContainsKey(name);
		}

		public void SetHeader(string name, string value)
		{
			if (Headers == null)
			{
				Headers = new Dictionary<string, string>();
			}

			Headers[name] = value;
		}

		public void RemoveHeader(string key)
		{
			Headers?.Remove(key);
		}

		public override string ToString()
		{
			return $"{nameof(Id)}: {Id}, {nameof(Headers)}: {Headers}, {nameof(Payload)}: {Payload}";
		}
	}
}