namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using EssentialsPlugin.Utility;
    using ProcessHandlers;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Entity.Sector.SectorObject;
    using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
    using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
    using VRage.ModAPI;
    public class HandleAdminSpeed : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "This command allows you to set a player's max speed. Usage: /admin speed <player name> <speed> (minutes)";
        }
        public override string GetCommandText( )
        {
            return "/admin speed";
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

        public override bool IsAdminCommand( )
        {
            return true;
        }

        public override bool AllowedInConsole( )
        {
            return true;
        }

        // /admin ownership change name gridId
        public override bool HandleCommand( ulong userId, string[ ] words )
        {
            if ( words.Length < 2 )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }

            string name = words[0].ToLower( );
            ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerName( name, true );
            if ( steamId == 0 )
            {
                Communication.SendPrivateInformation( userId, string.Format( "Couldn't find player with name '{1}'", words[0] ) );
                return true;
            }
            long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId( steamId ).First( );
            IMyEntity player;
            if ( !MyAPIGateway.Entities.TryGetEntityById( PlayerMap.Instance.GetPlayerEntityId( steamId ), out player ) )
            {
                Communication.SendPrivateInformation( userId, string.Format( "Couldn't find player with name '{1}'", words[0] ) );
                return true;
            }

            float setSpeed;
            if ( !float.TryParse( words[1], out setSpeed ) )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }

                int setTime=0;
            if ( words.Length > 2 )
            {
                if ( !int.TryParse( words[2], out setTime ) )
                {
                    Communication.SendPrivateInformation( userId, GetHelp( ) );
                    return true;
                }
            }
            byte[ ] data = Encoding.UTF8.GetBytes( MyAPIGateway.Utilities.SerializeToXML<float>( setSpeed ) );
            Communication.SendDataMessage( steamId, Communication.DataMessageType.MaxSpeed, data );
            //ProcessSpeed.SpeedPlayers.Add( new Tuple<IMyPlayer, double, int>( (IMyPlayer)player, setSpeed, setTime) );
            Communication.SendPrivateInformation( userId, string.Format( "Set maximum speed of player {0} to {1}m/s.", words[0], setSpeed ) );
            return true;
        }
    }
}
