using MonitoringMarket.Abstractions;
using MonitoringMarket.HelperClasses;
using System;

namespace MonitoringMarket.Implementations
{
	public class ConsoleUserInteractionManager : IUserInteractionManager
	{
		public void SendUserMessage(string message)
		{
			Console.WriteLine(message);
		}

		public string GetUserMessage()
		{
			return Console.ReadLine().ToLower();
		}

		public void NotifyUser()
		{
			FlashWindowManager.Flash();
		}
	}
}
