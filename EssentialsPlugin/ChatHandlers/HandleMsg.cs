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

		public override bool IsClientOnly()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			if(words.Count() < 2)
			{
				Communication.SendClientMessage(userId, "/message Server " + GetHelp());
			}

			ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerName(words[0], true);
			if(steamId < 1)
			{
				Communication.SendClientMessage(userId, string.Format("/message Server an not find user: {0}", words[0]));
				return true;
			}

			string userName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
			Communication.SendClientMessage(steamId, string.Format("/message P:{0} {1}", userName, string.Join(" ", words.Skip(1).ToArray())));
			Communication.SendClientMessage(userId, string.Format("/message Server Sent private message to: {0}", PlayerMap.Instance.GetPlayerNameFromSteamId(steamId)));

			return true;
		}
	}
}
