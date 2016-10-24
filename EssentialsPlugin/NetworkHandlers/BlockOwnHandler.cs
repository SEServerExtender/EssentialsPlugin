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
    using VRage.Game.Entity;
    using VRage.Library.Collections;
    using VRage.Network;
    using VRageMath;

    public class BlockOwnHandler : NetworkHandlerBase
    {
        private const string ChangeOwnerName = "OnChangeOwnerRequest";
        private const string ChangeOwnersName = "OnChangeOwnersRequest";
        private static readonly Dictionary<string, bool> _unitTestResults = new Dictionary<string, bool>( );

        private readonly Timer _kickTimer = new Timer( 30000 );

        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name == ChangeOwnerName )
            {
                if ( !_unitTestResults.ContainsKey( ChangeOwnerName ) )
                {
                    //void OnChangeOwnerRequest(long blockId, long owner, MyOwnershipShareModeEnum shareMode)
                    ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
                    if ( parameters.Length != 3 )
                    {
                        _unitTestResults[ChangeOwnerName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(long)
                         || parameters[1].ParameterType != typeof(long)
                         || parameters[2].ParameterType != typeof(MyOwnershipShareModeEnum) )
                    {
                        _unitTestResults[ChangeOwnerName] = false;
                        return false;
                    }
                    _unitTestResults[ChangeOwnerName] = true;
                }

                return _unitTestResults[ChangeOwnerName];
            }
            if ( site.MethodInfo.Name == ChangeOwnersName )
            {
                if ( !_unitTestResults.ContainsKey( ChangeOwnersName ) )
                {
                    //private static void OnChangeOwnersRequest(MyOwnershipShareModeEnum shareMode, List<MySingleOwnershipRequest> requests, long requestingPlayer)   
                    ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
                    if ( parameters.Length != 3 )
                    {
                        _unitTestResults[ChangeOwnersName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(MyOwnershipShareModeEnum)
                         || parameters[1].ParameterType != typeof(List<MyCubeGrid.MySingleOwnershipRequest>)
                         || parameters[2].ParameterType != typeof(long) )
                    {
                        _unitTestResults[ChangeOwnersName] = false;
                        return false;
                    }
                    _unitTestResults[ChangeOwnersName] = true;
                }
                return _unitTestResults[ChangeOwnersName];
            }
            return false;
        }

        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if ( !PluginSettings.Instance.ProtectedEnabled )
                return false;
            HashSet<MyCubeGrid> processGrids = new HashSet<MyCubeGrid>( );
            if ( site.MethodInfo.Name == ChangeOwnerName )
            {
                MyCubeGrid grid = obj as MyCubeGrid;
                if ( grid == null )
                {
                    Essentials.Log.Debug( "Null grid in BlockOwnHandler" );
                    return false;
                }
                processGrids.Add( grid );
            }
            else if ( site.MethodInfo.Name == ChangeOwnersName )
            {
                MyOwnershipShareModeEnum shareMode = MyOwnershipShareModeEnum.None;
                List<MyCubeGrid.MySingleOwnershipRequest> requests = new List<MyCubeGrid.MySingleOwnershipRequest>( );
                long requestingPlayer = 0;

                Serialize( site.MethodInfo, stream, ref shareMode, ref requests, ref requestingPlayer );

                foreach ( MyCubeGrid.MySingleOwnershipRequest request in requests )
                {
                    MyEntity entity;
                    MyEntities.TryGetEntityById( request.BlockId, out entity );
                    MyCubeBlock block = entity as MyCubeBlock;
                    if ( block?.CubeGrid == null )
                        continue;

                    processGrids.Add( block.CubeGrid );
                }
            }

            bool found = false;
            Parallel.ForEach( processGrids, grid =>
                                            {
                                                foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
                                                {
                                                    if ( !item.Enabled )
                                                        continue;

                                                    if ( item.EntityId != grid.EntityId )
                                                        continue;

                                                    if ( !item.ProtectionSettingsDict.Dictionary.ContainsKey( ProtectedItem.ProtectionModeEnum.BlockOwn ) )
                                                        continue;

                                                    ProtectedItem.ProtectionSettings settings = item.ProtectionSettingsDict[ProtectedItem.ProtectionModeEnum.BlockOwn];

                                                    if ( Protection.Instance.CheckPlayerExempt( settings, grid, remoteUserId ) )
                                                        continue;

                                                    if ( item.LogOnly )
                                                    {
                                                        Essentials.Log.Info( $"Recieved block ownership change request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
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

                                                    Essentials.Log.Info( $"Intercepted block ownership change request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );

                                                    switch ( settings.PunishmentType )
                                                    {
                                                        case ProtectedItem.PunishmentEnum.Kick:
                                                            _kickTimer.Elapsed += ( sender, e ) =>
                                                                                  {
                                                                                      Essentials.Log.Info( $"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for changing block ownership on protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
                                                                                      MyMultiplayer.Static.KickClient( remoteUserId );
                                                                                  };
                                                            _kickTimer.AutoReset = false;
                                                            _kickTimer.Start( );
                                                            break;
                                                        case ProtectedItem.PunishmentEnum.Ban:
                                                            _kickTimer.Elapsed += ( sender, e ) =>
                                                                                  {
                                                                                      Essentials.Log.Info( $"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for changing block ownership on protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
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
                                            } );
            return found;
        }
    }
}
