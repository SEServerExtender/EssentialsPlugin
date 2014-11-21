using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssentialsPlugin.Utility;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleTimeleft : ChatHandlerBase
	{
		DateTime m_start = DateTime.Now;

		public override string GetHelp()
		{
			return "Shows the time remaining until the Automated Restart.";
		}

		public override string GetCommandText()
		{
			return "/timeleft";
		}

		public override bool IsAdminCommand()
		{
			return false;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			if (!PluginSettings.Instance.RestartEnabled)
			{
				Communication.SendPrivateInformation(userId, "Automatic Restarts are disabled.");
				return true;
			}

			if (GetNextRestartTime() == null)
			{
				Communication.SendPrivateInformation(userId, "No restart time defined.");
				return true;
			}
				
			Communication.SendPrivateInformation(userId, string.Format("Time remaining until restart: {0}", General.TimeSpanToString(GetNextRestartTime().Value - DateTime.Now)));
			return true;
		}

		private DateTime? GetNextRestartTime()
		{
			DateTime? result = null;

			foreach (RestartTimeItem item in PluginSettings.Instance.RestartTimeItems)
			{
				if (!item.Enabled)
					continue;

				DateTime time = new DateTime(m_start.Year, m_start.Month, m_start.Day, item.Restart.Hour, item.Restart.Minute, 0);
				if (time < m_start.AddMinutes(-1))
					time = time.AddDays(1);

				//Logging.WriteLineAndConsole(string.Format("Time: {0}", time));

				if (result == null)
					result = time;
				else
				{
					if (result.Value > time)
						result = time;
				}
			}

			return result;
		}
	}
}
