namespace IO.Eventuate.Tram.Messaging.Consumer.Kafka
{
	public class ReceivedMessage
	{
		public string ConsumerId { get; set; }
		public string MessageId { get; set; }
	}
}