namespace EssentialsPlugin.ChatHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class HandleTicketExtend : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "Adds time to a ticket. Usage: /ticket extend <SteamID> <minutes>";
        }

        public override string GetCommandText( )
        {
            return "/ticket extend";
        }

        public override bool IsAdminCommand( )
        {
            return false;
        }

        public override bool AllowedInConsole( )
        {
            return true;
        }

        public override bool HandleCommand( ulong userId, string[] words )
        {
            if ( words.Length != 2 )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }

            ulong steamId;
            int time;

            if ( !ulong.TryParse( words[0], out steamId ) || !int.TryParse( words[1], out time ) )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }

            foreach ( TicketPlayerItem item in PluginSettings.Instance.TicketPlayers )
            {
                if ( item.TicketId == steamId )
                {
                    item.TimeAllocated += time;
                    Communication.SendPrivateInformation( userId, $"Added {time} minutes to ticket {steamId}. Time remaining: {item.TimeAllocated - item.TimeUsed}." );
                    return true;
                }
            }

            Communication.SendPrivateInformation( userId, "Could not find ticket." );
            return true;
        }
    }
}
