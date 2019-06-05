namespace IO.Eventuate.Tram.Consumer.Database
{
	public class ReceivedMessage
	{
		public string ConsumerId { get; set; }
		public string MessageId { get; set; }
		public long CreationTime { get; set; }
	}
}