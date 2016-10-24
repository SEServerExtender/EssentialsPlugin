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

    public class GridNameHandler : NetworkHandlerBase
    {
        private static bool? _unitTestResult;

        private readonly Timer _kickTimer = new Timer( 30000 );

        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name != "OnChangeDisplayNameRequest" )
                return false;

            if ( _unitTestResult == null )
            {
                //void OnChangeDisplayNameRequest(String displayName)
                ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
                if ( parameters.Length != 1 )
                {
                    _unitTestResult = false;
                    return false;
                }

                if ( parameters[0].ParameterType != typeof(string) )
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

            MyCubeGrid grid = obj as MyCubeGrid;
            if ( grid == null )
            {
                Essentials.Log.Debug( "Null grid in GridNameHandler" );
                return false;
            }

            bool found = false;
            foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
            {
                if ( !item.Enabled )
                    continue;

                if ( item.EntityId != grid.EntityId )
                    continue;

                if ( !item.ProtectionSettingsDict.Dictionary.ContainsKey( ProtectedItem.ProtectionModeEnum.GridRename ) )
                    continue;

                ProtectedItem.ProtectionSettings settings = item.ProtectionSettingsDict[ProtectedItem.ProtectionModeEnum.GridRename];

                if ( Protection.Instance.CheckPlayerExempt( settings, grid, remoteUserId ) )
                    continue;

                if ( item.LogOnly )
                {
                    Essentials.Log.Info( $"Recieved grid rename request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
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

                Essentials.Log.Info( $"Intercepted grid rename request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );

                switch ( settings.PunishmentType )
                {
                    case ProtectedItem.PunishmentEnum.Kick:
                        _kickTimer.Elapsed += ( sender, e ) =>
                                              {
                                                  Essentials.Log.Info( $"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for renaming protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
                                                  MyMultiplayer.Static.KickClient( remoteUserId );
                                              };
                        _kickTimer.AutoReset = false;
                        _kickTimer.Start( );
                        break;
                    case ProtectedItem.PunishmentEnum.Ban:
                        _kickTimer.Elapsed += ( sender, e ) =>
                                              {
                                                  Essentials.Log.Info( $"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for renaming protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
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

                found = true;
            }

            return found;
        }
    }
}
