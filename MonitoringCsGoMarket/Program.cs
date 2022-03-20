using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringCsGoMarket
{
	class Program
	{
		static void Main(string[] args)
		{
			List<Task> tasks = new List<Task>();
			tasks.Add(Task.Run(MarketManager.SearchingItemsMarket));
			tasks.Add(Task.Run(MarketManager.MonitoringСurrentShoppingList));
			Task.WaitAll(tasks.ToArray());
		}
	}
}
