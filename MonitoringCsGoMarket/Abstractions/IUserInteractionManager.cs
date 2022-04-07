
namespace MonitoringMarket.Abstractions
{
	public interface IUserInteractionManager
	{
		void SendUserMessage(string message);
		string GetUserMessage();
		void NotifyUser();
	}
}
