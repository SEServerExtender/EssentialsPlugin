namespace EssentialsPlugin.ChatHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Server;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class HandleTicketRemove : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "Removes a ticket and kicks the player. Usage: /ticket remove <SteamID>";
        }

        public override string GetCommandText( )
        {
            return "/ticket remove";
        }

        public override bool IsAdminCommand( )
        {
            return true;
        }

        public override bool AllowedInConsole( )
        {
            return true;
        }

        public override bool HandleCommand( ulong userId, string[] words )
        {
            if ( words.Length != 1 )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }

            ulong steamId;
            if ( !ulong.TryParse( words[0], out steamId ) )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }

            foreach ( TicketPlayerItem item in PluginSettings.Instance.TicketPlayers )
            {
                if ( item.TicketId == steamId )
                {
                    PluginSettings.Instance.TicketPlayers.Remove( item );
                    MyMultiplayer.Static.KickClient( steamId );
                    Communication.SendPrivateInformation( userId, $"Removed ticket {steamId}." );
                    return true;
                }
            }
            Communication.SendPrivateInformation( userId, "Could not find ticket to remove." );
            return true;
        }
    }
}