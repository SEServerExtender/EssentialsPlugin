using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssentialsPlugin.Utility;
using SEModAPIInternal.API.Common;
using EssentialsPlugin.Settings;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleInfo : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Show administrator defined information about the server.  Usage: /info [topic]";
		}

		public override string GetCommandText()
		{
			return "/info";
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
			if (PluginSettings.Instance.InformationEnabled)
			{
				if (words.Count() < 1)
				{
					String noticeList = "";
					foreach (InformationItem item in PluginSettings.Instance.InformationItems)
					{
						if (!item.Enabled)
							continue;

						if (item.SubCommand == "")
							continue;

						if (noticeList != "")
							noticeList += ", ";

						noticeList += item.SubCommand;
					}

					Communication.SendPrivateInformation(userId, "Type /info followed by: " + noticeList + " for more info.  For example: '/info motd' to view MOTD.");
				}
				else
				{
					foreach (InformationItem item in PluginSettings.Instance.InformationItems)
					{
						if (item.SubCommand == "")
							continue;

						if (item.SubCommand.ToLower() == words[0].ToLower() && item.Enabled)
						{
							string userName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
							string subText = item.SubText.Replace("%name%", userName).Split(new string[] { "\n" }, 2, StringSplitOptions.None).First();
							Communication.SendPrivateInformation(userId, subText);
							break;
						}
					}
				}
			}

			return true;
		}
	}
}
