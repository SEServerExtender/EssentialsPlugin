namespace EssentialsPlugin.NetworkHandlers
{
    using System;
    using System.Collections.Generic;
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

    public class RemoveBlockHandler : NetworkHandlerBase
    {
        private const string RazeBlocksName = "RazeBlocksRequest";
        private const string RazeAreaName = "RazeBlocksAreaRequest";
        private static readonly Dictionary<string, bool> _unitTestResults = new Dictionary<string, bool>( );

        private readonly Timer _kickTimer = new Timer( 30000 );

        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name == RazeBlocksName )
            {
                if ( !_unitTestResults.ContainsKey( RazeBlocksName ) )
                {
                    //public void RazeBlocks(List<Vector3I> locations)
                    ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
                    if ( parameters.Length != 1 )
                    {
                        _unitTestResults[RazeBlocksName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(List<Vector3I>) )
                    {
                        _unitTestResults[RazeBlocksName] = false;
                        return false;
                    }

                    _unitTestResults[RazeBlocksName] = true;
                }

                return _unitTestResults[RazeBlocksName];
            }
            if ( site.MethodInfo.Name == RazeAreaName )
            {
                if ( !_unitTestResults.ContainsKey( RazeAreaName ) )
                {
                    //void RazeBlocksAreaRequest(Vector3I pos, Vector3UByte size)
                    ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
                    if ( parameters.Length != 2 )
                    {
                        _unitTestResults[RazeAreaName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(Vector3I)
                         || parameters[1].ParameterType != typeof(Vector3UByte) )
                    {
                        _unitTestResults[RazeAreaName] = false;
                        return false;
                    }

                    _unitTestResults[RazeAreaName] = true;
                }

                return _unitTestResults[RazeAreaName];
            }
            return false;
        }

        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if ( !PluginSettings.Instance.ProtectedEnabled )
                return false;

            MyCubeGrid grid = obj as MyCubeGrid;
            if ( grid == null )
            {
                Essentials.Log.Debug( "Null grid in RemoveBlockHandler" );
                return false;
            }
            
            foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
            {
                if ( !item.Enabled )
                    continue;

                if (item.EntityId != grid.EntityId && item.EntityId != -1)
                    continue;

                if ( !item.ProtectionSettingsDict.Dictionary.ContainsKey( ProtectedItem.ProtectionModeEnum.BlockRemove ) )
                    continue;

                ProtectedItem.ProtectionSettings settings = item.ProtectionSettingsDict[ProtectedItem.ProtectionModeEnum.BlockRemove];

                if ( Protection.Instance.CheckPlayerExempt( settings, grid, remoteUserId ) )
                    continue;

                if ( item.LogOnly )
                {
                    Essentials.Log.Info( $"Recieved block remove request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
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

                Essentials.Log.Info( $"Intercepted block remove request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );

                switch ( settings.PunishmentType )
                {
                    case ProtectedItem.PunishmentEnum.Kick:
                        _kickTimer.Elapsed += ( sender, e ) =>
                                              {
                                                  Essentials.Log.Info( $"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for removing blocks from protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
                                                  MyMultiplayer.Static.KickClient( remoteUserId );
                                              };
                        _kickTimer.AutoReset = false;
                        _kickTimer.Start( );
                        break;
                    case ProtectedItem.PunishmentEnum.Ban:
                        _kickTimer.Elapsed += ( sender, e ) =>
                                              {
                                                  Essentials.Log.Info( $"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for removing blocks from protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
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

                return true;
            }
            return false;
        }
    }
}
