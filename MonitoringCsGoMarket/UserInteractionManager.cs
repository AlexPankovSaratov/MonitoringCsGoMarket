using System;

namespace MonitoringCsGoMarket
{
	public static class UserInteractionManager
	{
		public static void SendMessage(string message)
		{
			Console.WriteLine(message);
		}

		public static string GetUserMessage()
		{
			return Console.ReadLine().ToLower();
		}
	}
}
