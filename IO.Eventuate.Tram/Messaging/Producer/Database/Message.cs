using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Messaging.Producer.Database
{
	public class Message
	{
		public Message()
		{
			
		}
		
		public Message(IMessage message)
		{
			Id = message.Id;
			Destination = message.GetRequiredHeader(MessageHeaders.Destination);
			Headers = JsonMapper.ToJson(message.Headers);
			Payload = message.Payload;
		}
		
		public string Id { get; set; }
		public string Destination { get; set; }
		public string Headers { get; set; }
		public string Payload { get; set; }
		public short Published { get; set; }
		public long CreationTime { get; set; }
	}
}