namespace EssentialsPlugin.ChatHandlers
{
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using SEModAPIInternal.API.Common;

	public class HandleTicketTimeleft : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Checks how long is left on a ticket. Running with no argument returns your own ticket. Usage: /ticket timeleft (player name)";
		}

		public override string GetCommandText()
		{
			return "/ticket timeleft";
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
		    ulong steamId = 0;
		    if ( words.Length > 0 )
		        steamId = PlayerMap.Instance.GetSteamIdFromPlayerName( words[1], true );
		    if ( steamId == 0 )
		        steamId = userId;

		    foreach ( TicketPlayerItem item in PluginSettings.Instance.TicketPlayers )
		    {
		        if ( item.TicketId == steamId )
		        {
		            int timeleft = item.TimeAllocated - item.TimeUsed;
		            string playerName = PlayerMap.Instance.GetFastPlayerNameFromSteamId( steamId );
		            Communication.SendPrivateInformation( userId, $"{playerName} has {timeleft} minutes remaining." );
		        }
		    }
                return true;
		}
	}
}
