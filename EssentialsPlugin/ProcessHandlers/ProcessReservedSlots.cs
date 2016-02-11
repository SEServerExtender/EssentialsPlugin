namespace EssentialsPlugin.ProcessHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.AI.BrainSimulatorIntegration;
    using Sandbox.ModAPI;
    using SEModAPIExtensions.API;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Server;
    using SteamSDK;
    using VRageMath;

    public class ProcessReservedSlots : ProcessHandlerBase
    {
        private static readonly List<ulong> ReservedPlayers = new List<ulong>( );
        private static readonly List<ulong> WaitingPlayers = new List<ulong>( );
        private static readonly List<IMyPlayer> ConnectedPlayers = new List<IMyPlayer>( );
        private static bool _init;

        public static void Init( )
        {
            if (_init)
                return;

            _init = true;
            SteamServerAPI.Instance.GameServer.UserGroupStatus += GameServer_UserGroupStatus;
            SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse += GameServer_ValidateAuthTicketResponse;

            Essentials.Log.Info( "Reserved slots initialized" );
        }

        private static void GameServer_ValidateAuthTicketResponse( ulong remoteUserId, AuthSessionResponseEnum response,
                                                                   ulong ownerSteamId )
        {
            //using the player join event takes too long, sometimes they can load in before we boot them
            //we're not replacing the lobby yet, but hooking into this event will give us more time to verify players

            if (!PluginSettings.Instance.ReservedSlotsEnabled)
                return;

            if (response != AuthSessionResponseEnum.OK)
                return;

            if (PluginSettings.Instance.ReservedSlotsPlayers.Contains( remoteUserId.ToString( ) ))
            {
                ReservedPlayers.Add( remoteUserId );
                Essentials.Log.Info( "Whitelisted player connected: " + remoteUserId );
                Essentials.Log.Info( "{0} whitelisted players connected. {1} of {2} reserved slots allocated.",
                                     ReservedPlayers.Count,
                                     Math.Min( ReservedPlayers.Count, PluginSettings.Instance.ReservedSlotsCount ),
                                     PluginSettings.Instance.ReservedSlotsCount );
                return;
            }

            if (PluginSettings.Instance.ReservedSlotsAdmins && PlayerManager.Instance.IsUserAdmin( remoteUserId ))
            {
                ReservedPlayers.Add( remoteUserId );
                Essentials.Log.Info( "Whitelisted admin connected: " + remoteUserId );
                Essentials.Log.Info( "{0} whitelisted players connected. {1} of {2} reserved slots allocated.",
                                     ReservedPlayers.Count,
                                     Math.Min( ReservedPlayers.Count, PluginSettings.Instance.ReservedSlotsCount ),
                                     PluginSettings.Instance.ReservedSlotsCount );

                return;
            }

            if (PluginSettings.Instance.ReservedSlotsGroup != 0)
            {
                //ask Steam if the connecting player is in the whitelisted group. response is raised as an event; GameServer_UserGroupStatus
                SteamServerAPI.Instance.GameServer.RequestGroupStatus( remoteUserId,
                                                                       PluginSettings.Instance.ReservedSlotsGroup );
                WaitingPlayers.Add( remoteUserId );
                return;
            }

            DenyPlayer( remoteUserId );
        }

        /*
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
        */

        private static void DenyPlayer( ulong remoteUserId )
        {
            //get the list of current players just so we can count them (this is a stupid solution)
            MyAPIGateway.Players.GetPlayers( ConnectedPlayers );

            int publicPlayers = ConnectedPlayers.Count -
                                Math.Min( ReservedPlayers.Count, PluginSettings.Instance.ReservedSlotsCount );
            int publicSlots = Server.Instance.Config.MaxPlayers - PluginSettings.Instance.ReservedSlotsCount;

            if (publicPlayers < publicSlots)
                return;

            //don't do anything while we're waiting for group authorization
            if (WaitingPlayers.Contains( remoteUserId ))
                return;

            //kick the player with the "Server is full" message
            //too bad we can't send a custom message, but they're hardcoded into the client
            Essentials.Log.Info( "Player denied: " + remoteUserId );
            JoinResultMsg msg = new JoinResultMsg( );
            msg.JoinResult = JoinResult.ServerFull;
            msg.Admin = 0;
            ServerNetworkManager.Instance.SendStruct( remoteUserId, msg, msg.GetType( ) );
        }

        private static void GameServer_UserGroupStatus( ulong userId, ulong groupId, bool member, bool officier )
        {
            if (!PluginSettings.Instance.ReservedSlotsEnabled)
                return;

            if (groupId == PluginSettings.Instance.ReservedSlotsGroup && WaitingPlayers.Remove( userId ))
            {
                if (member || officier)
                {
                    ReservedPlayers.Add( userId );
                    Essentials.Log.Info( "Whitelisted player connected: " + userId );
                    Essentials.Log.Info( "{0} whitelisted players connected. {1} of {2} reserved slots allocated.",
                                         ReservedPlayers.Count,
                                         Math.Min( ReservedPlayers.Count, PluginSettings.Instance.ReservedSlotsCount ),
                                         PluginSettings.Instance.ReservedSlotsCount );
                }
                else
                    DenyPlayer( userId );
            }
        }

        public override void OnPlayerLeft( ulong remoteUserId )
        {
            //free up allocated slots so someone else can use it
            if (ReservedPlayers.Contains( remoteUserId ))
            {
                Essentials.Log.Info( "Freed slot from: " + remoteUserId );
                ReservedPlayers.Remove( remoteUserId );
                Essentials.Log.Info( "{0} slots of {1} allocated.", ReservedPlayers.Count,
                                     PluginSettings.Instance.ReservedSlotsCount );
            }
        }
    }
}
