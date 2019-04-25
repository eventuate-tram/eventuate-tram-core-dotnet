namespace IO.Eventuate.Tram.Messaging.Consumer.Kafka
{
	public interface IDuplicateMessageDetector
	{
		bool IsDuplicate(string consumerId, string messageId);
	}
}