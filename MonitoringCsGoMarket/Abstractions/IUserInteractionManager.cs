using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringCsGoMarket.Abstractions
{
	public interface IUserInteractionManager
	{
		void SendUserMessage(string message);
		string GetUserMessage();
		void Flash();
	}
}
