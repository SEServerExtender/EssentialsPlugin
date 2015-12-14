namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;

	public class HandleMotd : ChatHandlerBase
	{
		DateTime m_start = DateTime.Now;

		public override string GetHelp()
		{
			return "Shows the message of the day. Usage: /motd";
		}

		public override string GetCommandText()
		{
			return "/motd";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\""+GetHelp()+"\" \"close\" ";
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
			if(!PluginSettings.Instance.GreetingItem.Enabled)
			{
				Communication.SendPrivateInformation(userId, string.Format("No MOTD dialog defined on server."));
				return true;
			}

			List<IMyPlayer> players = new List<IMyPlayer>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Players.GetPlayers(players);
			});

			IMyPlayer player = players.FirstOrDefault(x => x.SteamUserId == userId);
			if (player == null)
			{
				Communication.SendPrivateInformation(userId, "Unable to find player id");
				return true;
			}

			SettingsGreetingDialogItem gItem = PluginSettings.Instance.GreetingItem;
			Communication.SendClientMessage(userId, string.Format("/dialog \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"", gItem.Title.Replace("%name%", player.DisplayName), gItem.Header.Replace("%name%", player.DisplayName), " ", gItem.Contents.Replace("%name%", player.DisplayName).Replace("\r", "").Replace("\n", "|").Replace("\"", "'"), gItem.ButtonText));			
			return true;
		}
	}
}
