using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
