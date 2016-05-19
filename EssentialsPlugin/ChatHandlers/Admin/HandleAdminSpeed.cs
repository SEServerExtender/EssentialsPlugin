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
    using VRage.Game.ModAPI;
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
                Communication.SendPrivateInformation( userId, $"Couldn't find player with name '{words[0]}'" );
                return true;
            }
            
            long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId( steamId ).First( );
            IMyEntity player;
            if ( !MyAPIGateway.Entities.TryGetEntityById( PlayerMap.Instance.GetPlayerEntityId( steamId ), out player ) )
            {
                Communication.SendPrivateInformation( userId, $"Couldn't find player with name '{words[0]}'" );
                return true;
            }

            float setSpeed;
            if ( !float.TryParse( words[1], out setSpeed ) )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }

            int setMinutes = int.MaxValue;
            if ( words.Length > 2 )
            {
                if ( !int.TryParse( words[2], out setMinutes ) )
                {
                    Communication.SendPrivateInformation( userId, GetHelp( ) );
                    return true;
                }
            }
            DateTime setTime = DateTime.Now + TimeSpan.FromMinutes( setMinutes );
           // byte[ ] data = Encoding.UTF8.GetBytes( MyAPIGateway.Utilities.SerializeToXML<float>( setSpeed ) );
           // Communication.SendDataMessage( steamId, Communication.DataMessageType.MaxSpeed, data );
           lock(ProcessSpeed.SpeedPlayers)
            { ProcessSpeed.SpeedPlayers[playerId] = new Tuple< float, DateTime>( setSpeed, setTime);}
            Communication.SendPrivateInformation( userId, $"Set maximum speed of player {words[0]} to {setSpeed}m/s{( setMinutes == int.MaxValue ? "." : $" for {setMinutes} minutes." )}" );
            return true;
        }
    }
}
