using IO.Eventuate.Tram.Messaging.Common;

namespace IO.Eventuate.Tram.Messaging.Producer
{
	public interface IMessageSender
	{
		void Send(IMessage message);
	}
}