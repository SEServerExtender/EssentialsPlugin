using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI;
using SEModAPIExtensions.API;
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
        //private static int connectedCount = 0;

        public static void Init( )
        {
            if ( _init )
                return;

            _init = true;
            SteamSDK.SteamServerAPI.Instance.GameServer.UserGroupStatus += GameServer_UserGroupStatus;
            
            Essentials.Log.Info( "Reserve slot initialize" );
        }

        public override void OnPlayerJoined( ulong remoteUserId )
        {
            MyAPIGateway.Players.GetPlayers( connectedPlayers );

            if ( connectedPlayers.Count >= Server.Instance.Config.MaxPlayers )
                return;

            if ( reservedPlayers.Count >= PluginSettings.Instance.ReservedSlotsCount )
                return;

            if ( PluginSettings.Instance.ReservedSlotsPlayers.Contains( remoteUserId.ToString( ) ) )
            {
                Essentials.Log.Info( "Allocated slot to " + remoteUserId.ToString( ) );
                reservedPlayers.Add( remoteUserId );
                return;
            }

            if ( PluginSettings.Instance.ReservedSlotsGroup != 0 )
            {
                SteamServerAPI.Instance.GameServer.RequestGroupStatus( remoteUserId, PluginSettings.Instance.ReservedSlotsGroup );
                waitingPlayers.Add( remoteUserId );
                return;
            }

            DenyPlayer( remoteUserId );
        }

        private static void DenyPlayer( ulong remoteUserId )
        {
            Essentials.Log.Info( "deny player " + remoteUserId.ToString( ) );
            MyAPIGateway.Players.GetPlayers( connectedPlayers );

            int publicPlayers = connectedPlayers.Count - reservedPlayers.Count;
            int publicSlots = Server.Instance.Config.MaxPlayers - PluginSettings.Instance.ReservedSlotsCount;

            if ( publicPlayers < publicSlots )
                return;

            if ( waitingPlayers.Contains( remoteUserId ) )
                return;

            Essentials.Log.Info( "Removing user " + remoteUserId.ToString( ) );
            JoinResultMsg msg = new JoinResultMsg( );
            msg.JoinResult = JoinResult.ServerFull;
            msg.Admin = 0;
            ServerNetworkManager.Instance.SendStruct( remoteUserId, msg, msg.GetType( ) );
        }

        private static void GameServer_UserGroupStatus( ulong userId, ulong groupId, bool member, bool officier )
        {
            if ( groupId == PluginSettings.Instance.ReservedSlotsGroup && waitingPlayers.Remove( userId ) )
            {
                if ( member || officier )
                {
                    if ( reservedPlayers.Count < PluginSettings.Instance.ReservedSlotsCount )
                        reservedPlayers.Add( userId );
                    return;
                }
                else
                    DenyPlayer( userId );
            }
        }

        public override void OnPlayerLeft( ulong remoteUserId )
        {
            if ( reservedPlayers.Contains( remoteUserId ) )
                reservedPlayers.Remove( remoteUserId );
        }
    }
}
