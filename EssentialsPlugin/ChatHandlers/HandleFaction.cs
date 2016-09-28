namespace EssentialsPlugin.ChatHandlers
{
	using System.Linq;
	using EssentialsPlugin.Utility;
	using SEModAPIInternal.API.Common;

	public class HandleFaction : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Sends a private message to all Alliance members that are online.  Usage: /a <msg>";
		}

		public override string GetCommandText()
		{
			return "/a";
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
			if(!words.Any())
			{
				Communication.SendPrivateInformation(userId, GetHelp());
			}

			string userName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
			Communication.SendAllianceClientMessage(userId, string.Join(" ", words));
			return true;
		}
	}
}
