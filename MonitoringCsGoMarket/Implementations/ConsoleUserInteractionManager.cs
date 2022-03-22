using MonitoringCsGoMarket.Abstractions;
using MonitoringCsGoMarket.HelperClasses;
using System;

namespace MonitoringCsGoMarket.Implementations
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
