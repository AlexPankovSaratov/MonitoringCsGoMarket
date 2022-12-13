using MonitoringCsGoMarket.Implementations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonitoringCsGoMarket
{
	class Program
	{
		static void Main(string[] args)
		{
			List<Task> tasks = new List<Task>();
			tasks.Add(Task.Run(MarketManager.SearchingItemsMarket));
			tasks.Add(Task.Run(MarketManager.MonitoringСurrentShoppingList3));
			Task.WaitAll(tasks.ToArray());
		}
	}
}
