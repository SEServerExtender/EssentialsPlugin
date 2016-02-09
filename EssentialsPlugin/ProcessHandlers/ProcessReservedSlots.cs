using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI;
using SEModAPIExtensions.API;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using SteamSDK;

namespace EssentialsPlugin.ProcessHandlers
{
    public class ProcessReservedSlots : ProcessHandlerBase
    {
        private static List<ulong> reservedPlayers = new List<ulong>( );
        private static List<ulong> waitingPlayers = new List<ulong>( );
        private static List<IMyPlayer> connectedPlayers = new List<IMyPlayer>( );
        private static bool _init = false;

        public static void Init( )
        {
            if ( _init )
                return;

            _init = true;
            SteamSDK.SteamServerAPI.Instance.GameServer.UserGroupStatus += GameServer_UserGroupStatus;
            //SteamSDK.SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse -= MyDedicatedServerBase.GameServer_ValidateAuthTicketResponse( );

            Essentials.Log.Info( "Reserved slots initialized" );
        }

        public override void OnPlayerJoined( ulong remoteUserId )
        {
            //it might be better to hook into ValidateAuthTicketResponse, but doing it this way lets the game
            //take care of denying banned players and group/friend whitelisting

            if ( !PluginSettings.Instance.ReservedSlotsEnabled )
                return;

            if ( PluginSettings.Instance.ReservedSlotsPlayers.Contains( remoteUserId.ToString( ) ) )
            {
                reservedPlayers.Add( remoteUserId );
                Essentials.Log.Info( "Whitelisted player connected: " + remoteUserId.ToString( ) );
                Essentials.Log.Info( string.Format( "{0} whitelisted players connected. {1} of {2} reserved slots allocated.",
                    reservedPlayers.Count, Math.Min( reservedPlayers.Count, PluginSettings.Instance.ReservedSlotsCount ), PluginSettings.Instance.ReservedSlotsCount ) );

                return;
            }

            if ( PluginSettings.Instance.ReservedSlotsAdmins && PlayerManager.Instance.IsUserAdmin( remoteUserId ) )
            {
                reservedPlayers.Add( remoteUserId );
                Essentials.Log.Info( "Whitelisted admin connected: " + remoteUserId.ToString( ) );
                Essentials.Log.Info( string.Format( "{0} whitelisted players connected. {1} of {2} reserved slots allocated.",
                    reservedPlayers.Count, Math.Min( reservedPlayers.Count, PluginSettings.Instance.ReservedSlotsCount ), PluginSettings.Instance.ReservedSlotsCount ) );

                return;
            }

            if ( PluginSettings.Instance.ReservedSlotsGroup != 0 )
            {
                //ask Steam if the connecting player is in the whitelisted group. response is raised as an event; GameServer_UserGroupStatus
                SteamServerAPI.Instance.GameServer.RequestGroupStatus( remoteUserId, PluginSettings.Instance.ReservedSlotsGroup );
                waitingPlayers.Add( remoteUserId );
                return;
            }

            DenyPlayer( remoteUserId );
        }

        private static void DenyPlayer( ulong remoteUserId )
        {
            //get the list of current players just so we can count them (this is a stupid solution)
            MyAPIGateway.Players.GetPlayers( connectedPlayers );

            int publicPlayers = connectedPlayers.Count - Math.Min( reservedPlayers.Count, PluginSettings.Instance.ReservedSlotsCount );
            int publicSlots = Server.Instance.Config.MaxPlayers - PluginSettings.Instance.ReservedSlotsCount;

            if ( publicPlayers < publicSlots )
                return;

            //don't do anything while we're waiting for group authorization
            if ( waitingPlayers.Contains( remoteUserId ) )
                return;

            //kick the player with the "Server is full" message
            //too bad we can't send a custom message, but they're hardcoded into the client
            Essentials.Log.Info( "Player denied: " + remoteUserId.ToString( ) );
            JoinResultMsg msg = new JoinResultMsg( );
            msg.JoinResult = JoinResult.ServerFull;
            msg.Admin = 0;
            ServerNetworkManager.Instance.SendStruct( remoteUserId, msg, msg.GetType( ) );
        }

        private static void GameServer_UserGroupStatus( ulong userId, ulong groupId, bool member, bool officier )
        {
            if ( !PluginSettings.Instance.ReservedSlotsEnabled )
                return;

            if ( groupId == PluginSettings.Instance.ReservedSlotsGroup && waitingPlayers.Remove( userId ) )
            {
                if ( member || officier )
                {
                    reservedPlayers.Add( userId );
                    Essentials.Log.Info( "Whitelisted player connected: " + userId.ToString( ) );
                    Essentials.Log.Info( string.Format( "{0} whitelisted players connected. {1} of {2} reserved slots allocated.",
                    reservedPlayers.Count, Math.Min( reservedPlayers.Count, PluginSettings.Instance.ReservedSlotsCount ), PluginSettings.Instance.ReservedSlotsCount ) );
                }
                else
                    DenyPlayer( userId );
            }
        }

        public override void OnPlayerLeft( ulong remoteUserId )
        {
            //free up allocated slots so someone else can use it
            if ( reservedPlayers.Contains( remoteUserId ) )
            {
                Essentials.Log.Info( "Freed slot from: " + remoteUserId );
                reservedPlayers.Remove( remoteUserId );
                Essentials.Log.Info( string.Format( "{0} slots of {1} allocated.", reservedPlayers.Count, PluginSettings.Instance.ReservedSlotsCount ) );
            }
        }
    }
}
