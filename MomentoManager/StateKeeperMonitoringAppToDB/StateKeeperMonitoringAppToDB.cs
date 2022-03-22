using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace StateKeeperMonitoringApp
{
	public class StateKeeperMonitoringAppToDB : IStateKeeperMonitoringApp
	{
		public StateKeeperMonitoringAppToDB()
		{
			using (ContextDb contextDb = new ContextDb())
			{
				contextDb.Database.EnsureCreated();
			}
		}
		public MomentoMonitoringApp GetState()
		{
			var momentoMonitoringApp = new MomentoMonitoringApp(new ConcurrentDictionary<string, decimal>(),  new ConcurrentDictionary<string, decimal>());
			using (ContextDb contextDb = new ContextDb())
			{
				foreach (var item in contextDb.ShoppingItemBuy)
				{
					bool ItemAdded = false;
					while (!ItemAdded)
					{
						ItemAdded = momentoMonitoringApp.currentShoppingList.TryAdd(item.TextLink, item.Price);
					}
				}
				foreach (var item in contextDb.ShoppingItemBlock)
				{
					bool ItemBlockAdded = false;
					while (!ItemBlockAdded)
					{
						ItemBlockAdded = momentoMonitoringApp.currentShoppingBlockList.TryAdd(item.TextLink, item.Price);
					}
				}
			}
			return momentoMonitoringApp;
		}

		public async void SetState(MomentoMonitoringApp momentoMonitoringApp)
		{
			var oldState = GetState();
			var newShoppingItemsBuy = new List<ShoppingItemBuy>();
			foreach (var item in momentoMonitoringApp.currentShoppingList
				  .Where(x => !oldState.currentShoppingList.Any(y => y.Key == x.Key))
				  .ToList())
			{
				newShoppingItemsBuy.Add(new ShoppingItemBuy { TextLink = item.Key, Price = item.Value });
			}
			var newShoppingItemsBlock = new List<ShoppingItemBlock>();
			foreach (var item in momentoMonitoringApp.currentShoppingBlockList
				  .Where(x => !oldState.currentShoppingBlockList.Any(y => y.Key == x.Key))
				  .ToList())
			{
				newShoppingItemsBlock.Add(new ShoppingItemBlock { TextLink = item.Key, Price = item.Value });
			}
			using (ContextDb contextDb = new ContextDb())
			{
				foreach (var item in newShoppingItemsBuy)
				{
					contextDb.Add(item);
				}
				foreach (var item in newShoppingItemsBlock)
				{
					contextDb.Add(item);
				}
				await contextDb.SaveChangesAsync();
			}
		}
	}
}
