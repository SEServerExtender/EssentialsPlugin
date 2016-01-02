namespace EssentialsPlugin.ChatHandlers
{
	using System.Linq;
	using EssentialsPlugin.Utility;
	using SEModAPIInternal.API.Common;

	public class HandleMsg : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Sends a private message to a user that is online.  Usage: /msg <username> <msg>";
		}

		public override string GetCommandText()
		{
			return "/msg";
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

		public override bool IsClientOnly()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			if(words.Count() < 2)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
			}

			ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerName(words[0], true);
			if(steamId < 1)
			{
				Communication.SendPrivateInformation(userId, string.Format("Can not find user: {0}", words[0]));
				return true;
			}
            
			string userName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
			Communication.SendPrivateInformation(steamId,  string.Join(" ", words.Skip(1).ToArray()), userName);
			Communication.SendPrivateInformation(userId, string.Format("Sent private message to: {0}", PlayerMap.Instance.GetPlayerNameFromSteamId(steamId)));

			return true;
		}
	}
}
