namespace EssentialsPlugin.ChatHandlers
{
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;

	public class HandleTicketAdd : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Adds a new ticket. Usage: /ticket add <SteamID> <minutes>";
		}

		public override string GetCommandText()
		{
			return "/ticket add";
		}

        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
		    if ( words.Length != 2 )
		    {
		        Communication.SendPrivateInformation( userId, GetHelp( ) );
		    }

		    ulong steamId;
		    int time;

		    if ( !ulong.TryParse( words[0], out steamId ) || !int.TryParse( words[1], out time ) )
		    {
		        Communication.SendPrivateInformation( userId, GetHelp( ) );
		        return true;
		    }

		    PluginSettings.Instance.TicketPlayers.Add( new TicketPlayerItem( steamId, time ) );
            Communication.SendPrivateInformation(userId, $"Added new ticket for {steamId} with {time} minutes.");
		    return true;
		}
	}
}
