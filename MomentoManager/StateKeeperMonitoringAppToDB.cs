using System.Collections.Concurrent;

namespace MomentoManager
{
	public class StateKeeperMonitoringAppToDB : IStateKeeperMonitoringApp
	{
		public MomentoMonitoringApp GetState()
		{
			return new MomentoMonitoringApp(new ConcurrentDictionary<string, decimal>(),  new ConcurrentDictionary<string, decimal>());
		}

		public void SetState(MomentoMonitoringApp momentoMonitoringApp)
		{
			
		}
	}
}
