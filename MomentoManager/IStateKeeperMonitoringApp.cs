
namespace StateKeeperMonitoringApp
{
	public interface IStateKeeperMonitoringApp
	{
		void SetState(MomentoMonitoringApp momentoMonitoringApp);
		MomentoMonitoringApp GetState();
	}
}
