namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using VRage.Game.ModAPI;

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

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "";
            DialogItem.content = GetHelp( );
            DialogItem.buttonText = "close";
            return DialogItem;
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
			//Wrapper.GameAction(() =>
			//{
				MyAPIGateway.Players.GetPlayers(players);
			//});

			IMyPlayer player = players.FirstOrDefault(x => x.SteamUserId == userId);
			if (player == null)
			{
				Communication.SendPrivateInformation(userId, "Unable to find player id");
				return true;
			}

			SettingsGreetingDialogItem gItem = PluginSettings.Instance.GreetingItem;
			Communication.DisplayDialog(userId, gItem.Title.Replace("%name%", player.DisplayName), gItem.Header.Replace("%name%", player.DisplayName), 
                gItem.Contents.Replace("%name%", player.DisplayName).Replace("\r", "").Replace("\n", "|").Replace("\"", "'"), gItem.ButtonText);
                        
            return true;
		}
	}
}
