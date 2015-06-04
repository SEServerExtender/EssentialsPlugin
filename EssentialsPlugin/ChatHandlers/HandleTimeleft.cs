namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using EssentialsPlugin.ProcessHandlers;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;

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

			if (ProcessRestart.ForcedRestart != null)
				return ProcessRestart.ForcedRestart;

			foreach (RestartTimeItem item in PluginSettings.Instance.RestartTimeItems)
			{
				if (!item.Enabled)
					continue;

				DateTime time = new DateTime(m_start.Year, m_start.Month, m_start.Day, item.Restart.Hour, item.Restart.Minute, 0);
				if (time < m_start.AddMinutes(-1))
					time = time.AddDays(1);

				//Log.Info(string.Format("Time: {0}", time));

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
