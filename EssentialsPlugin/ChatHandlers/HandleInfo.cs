namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using System.Linq;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using SEModAPIInternal.API.Common;

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

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\"Sorry, there's nothing here yet :(\" \"close\" ";
            return longMessage;
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
				if (!words.Any())
				{
					ShowTopicList(userId);
				}
				else
				{
					bool found = false;
					foreach (InformationItem item in PluginSettings.Instance.InformationItems)
					{
						if (item.SubCommand == null || item.SubCommand == "")
							continue;

						if (item.SubCommand.ToLower() == words[0].ToLower() && item.Enabled)
						{
							string userName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
							string subText = item.SubText.Replace("%name%", userName).Split(new string[] { "\n" }, 2, StringSplitOptions.None).First();
							Communication.SendPrivateInformation(userId, subText);
							found = true;
							break;
						}
					}

					if (!found)
					{
						Communication.SendPrivateInformation(userId, "Info Error: Topic not found.");
						ShowTopicList(userId);
					}
				}
			}

			return true;
		}

		private static void ShowTopicList(ulong userId)
		{
			String noticeList = "";
			foreach (InformationItem item in PluginSettings.Instance.InformationItems)
			{
				if (!item.Enabled)
					continue;

				if (item.SubCommand == null || item.SubCommand == "")
					continue;

				if (noticeList != "")
					noticeList += ", ";

				noticeList += item.SubCommand;
			}

			Communication.SendPrivateInformation(userId, "Type /info followed by: " + noticeList + " for more info.  For example: '/info motd' to view MOTD.");
		}
	}
}
