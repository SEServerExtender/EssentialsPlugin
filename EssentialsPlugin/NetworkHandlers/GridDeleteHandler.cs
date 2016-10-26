namespace EssentialsPlugin.NetworkHandlers
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Timers;
    using ProcessHandlers;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.Entities;
    using Sandbox.Game.World;
    using Sandbox.ModAPI;
    using Settings;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Server;
    using Utility;
    using VRage.Game;
    using VRage.Library.Collections;
    using VRage.Network;
    using VRageMath;

    public class GridDeleteHandler : NetworkHandlerBase
    {
        private static bool? _unitTestResult;

        private readonly Timer _kickTimer = new Timer( 30000 );

        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name != "OnEntityClosedRequest" )
                return false;

            if ( _unitTestResult == null )
            {
                //static void OnEntityClosedRequest(long entityId)
                ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
                if ( parameters.Length != 1 )
                {
                    _unitTestResult = false;
                    return false;
                }

                if ( parameters[0].ParameterType != typeof(long) )
                {
                    _unitTestResult = false;
                    return false;
                }

                _unitTestResult = true;
            }

            return _unitTestResult.Value;
        }

        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if ( !PluginSettings.Instance.ProtectedEnabled )
                return false;

            long entityId = 0;
            Serialize( site.MethodInfo, stream, ref entityId );

            if ( entityId == 0 )
            {
                Essentials.Log.Debug( "Error deserializing argument in GridDeleteHandler" );
                return false;
            }

            MyCubeGrid grid;
            if ( !MyEntities.TryGetEntityById( entityId, out grid ) )
            {
                //Essentials.Log.Debug( "Couldn't find grid in GridDeleteHandler." );
                return false;
            }
            
            foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
            {
                if ( !item.Enabled )
                    continue;

                if (item.EntityId != grid.EntityId && item.EntityId != -1)
                    continue;

                if ( !item.ProtectionSettingsDict.Dictionary.ContainsKey( ProtectedItem.ProtectionModeEnum.GridDelete ) )
                    continue;

                ProtectedItem.ProtectionSettings settings = item.ProtectionSettingsDict[ProtectedItem.ProtectionModeEnum.GridDelete];

                if ( Protection.Instance.CheckPlayerExempt( settings, grid, remoteUserId ) )
                    continue;

                if ( item.LogOnly )
                {
                    Essentials.Log.Info( $"Recieved grid delete request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
                    continue;
                }

                if ( !string.IsNullOrEmpty( settings.PrivateWarningMessage ) )
                    Communication.Notification( remoteUserId, MyFontEnum.Red, 5000, settings.PrivateWarningMessage );

                if ( !string.IsNullOrEmpty( settings.PublicWarningMessage ) )
                    Communication.SendPublicInformation( settings.PublicWarningMessage.Replace( "%player%", PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId ) ) );

                if ( settings.BroadcastGPS )
                {
                    MyPlayer player = MySession.Static.Players.GetPlayerById( new MyPlayer.PlayerId( remoteUserId, 0 ) );
                    Vector3D pos = player.GetPosition( );
                    MyAPIGateway.Utilities.SendMessage( $"GPS:{player.DisplayName}:{pos.X}:{pos.Y}:{pos.Z}:" );
                }

                Essentials.Log.Info( $"Intercepted grid delete request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );

                switch ( settings.PunishmentType )
                {
                    case ProtectedItem.PunishmentEnum.Kick:
                        _kickTimer.Elapsed += ( sender, e ) =>
                                              {
                                                  Essentials.Log.Info( $"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for deleting protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
                                                  MyMultiplayer.Static.KickClient( remoteUserId );
                                              };
                        _kickTimer.AutoReset = false;
                        _kickTimer.Start( );
                        break;
                    case ProtectedItem.PunishmentEnum.Ban:
                        _kickTimer.Elapsed += ( sender, e ) =>
                                              {
                                                  Essentials.Log.Info( $"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for deleting protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
                                                  MyMultiplayer.Static.BanClient( remoteUserId, true );
                                              };
                        _kickTimer.AutoReset = false;
                        _kickTimer.Start( );
                        break;
                    case ProtectedItem.PunishmentEnum.Speed:
                        Task.Run( ( ) =>
                                  {
                                      lock ( ProcessSpeed.SpeedPlayers )
                                      {
                                          long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId( remoteUserId );
                                          ProcessSpeed.SpeedPlayers[playerId] = new Tuple<float, DateTime>( (float)settings.SpeedLimit, DateTime.Now + TimeSpan.FromMinutes( settings.SpeedTime ) );
                                      }
                                  } );
                        Essentials.Log.Info( $"Limited user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )} to {settings.SpeedLimit}m/s for {settings.SpeedTime} minutes" );
                        break;
                }

                //send the fail message to make the client play the paste fail sound
                //just because we can
                var inf = typeof(MyCubeBuilder).GetMethod("SpawnGridReply", BindingFlags.NonPublic | BindingFlags.Static);
                ServerNetworkManager.Instance.RaiseStaticEvent(inf, remoteUserId, false);

                return true;
            }

            return false;
        }
    }
}
