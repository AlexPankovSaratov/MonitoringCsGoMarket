using System.Collections.Concurrent;

namespace StateKeeperMonitoringApp
{
	public class MomentoMonitoringApp
	{
		private ConcurrentDictionary<string, decimal> _currentShoppingList;
		private ConcurrentDictionary<string, decimal> _currentShoppingBlockList;

		public ConcurrentDictionary<string, decimal> currentShoppingList 
		{
			get {return _currentShoppingList; }
		}
		public ConcurrentDictionary<string, decimal> currentShoppingBlockList
		{
			get { return _currentShoppingBlockList; }
		}

		public MomentoMonitoringApp(ConcurrentDictionary<string, decimal> currentShoppingList, ConcurrentDictionary<string, decimal> currentShoppingBlockList)
		{
			_currentShoppingList = currentShoppingList;
			_currentShoppingBlockList = currentShoppingBlockList;
		}
	}
}
