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
			if(!PluginSettings.Instance.RestartEnabled)
				Communication.SendPrivateInformation(userId, "Automatic Restarts are disabled.");

			Communication.SendPrivateInformation(userId, string.Format("Time remaining until restart: {0}", General.TimeSpanToString(TimeSpan.FromMinutes(PluginSettings.Instance.RestartTime) - (DateTime.Now - PluginSettings.RestartStartTime))));
			return true;
		}
	}
}
