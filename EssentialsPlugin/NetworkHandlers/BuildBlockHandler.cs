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

    public class BuildBlockHandler : NetworkHandlerBase
    {
        private const string BuildBlockName = "BuildBlockRequest";
        private const string BuildBlocksName = "BuildBlocksRequest";
        private const string BuildAreaName = "BuildBlocksAreaRequest";
        private static readonly Dictionary<string, bool> _unitTestResults = new Dictionary<string, bool>( );

        private readonly Timer _kickTimer = new Timer( 30000 );

        public override bool CanHandle( CallSite site )
        {
            //okay, there's three distinct methods that build blocks, we need to handle all of them
            if ( site.MethodInfo.Name == BuildBlockName )
            {
                if ( !_unitTestResults.ContainsKey( BuildBlockName ) )
                {
                    //public void BuildBlockRequest(uint colorMaskHsv, MyBlockLocation location, [DynamicObjectBuilder] MyObjectBuilder_CubeBlock blockObjectBuilder, long builderEntityId, bool instantBuild, long ownerId)
                    ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
                    if ( parameters.Length != 6 )
                    {
                        _unitTestResults[BuildBlockName] = false;
                        Essentials.Log.Error( "BuildBlockHandler failed unit test 1!" );
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(uint)
                         || parameters[1].ParameterType != typeof(MyCubeGrid.MyBlockLocation)
                         || parameters[2].ParameterType != typeof(MyObjectBuilder_CubeBlock)
                         || parameters[3].ParameterType != typeof(long)
                         || parameters[4].ParameterType != typeof(bool)
                         || parameters[5].ParameterType != typeof(long) )
                    {
                        _unitTestResults[BuildBlockName] = false;
                        Essentials.Log.Error( "BuildBlockHandler failed unit test 2!" );
                        return false;
                    }

                    _unitTestResults[BuildBlockName] = true;
                }

                return _unitTestResults[BuildBlockName];
            }
            if ( site.MethodInfo.Name == BuildBlocksName )
            {
                if ( !_unitTestResults.ContainsKey( BuildBlocksName ) )
                {
                    //void BuildBlocksRequest(uint colorMaskHsv, HashSet<MyBlockLocation> locations, long builderEntityId, bool instantBuild, long ownerId)
                    ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
                    if ( parameters.Length != 5 )
                    {
                        _unitTestResults[BuildBlocksName] = false;
                        Essentials.Log.Error( "BuildBlockHandler failed unit test 3!" );
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(uint)
                         || parameters[1].ParameterType != typeof(HashSet<MyCubeGrid.MyBlockLocation>)
                         || parameters[2].ParameterType != typeof(long)
                         || parameters[3].ParameterType != typeof(bool)
                         || parameters[4].ParameterType != typeof(long) )
                    {
                        _unitTestResults[BuildBlocksName] = false;
                        Essentials.Log.Error( "BuildBlockHandler failed unit test 4!" );
                        return false;
                    }

                    _unitTestResults[BuildBlocksName] = true;
                }

                return _unitTestResults[BuildBlocksName];
            }
            if ( site.MethodInfo.Name == BuildAreaName )
            {
                if ( !_unitTestResults.ContainsKey( BuildAreaName ) )
                {
                    //private void BuildBlocksAreaRequest(MyCubeGrid.MyBlockBuildArea area, long builderEntityId, bool instantBuild, long ownerId)
                    ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
                    if ( parameters.Length != 4 )
                    {
                        _unitTestResults[BuildAreaName] = false;
                        Essentials.Log.Error( "BuildBlockHandler failed unit test 5!" );
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(MyCubeGrid.MyBlockBuildArea)
                         || parameters[1].ParameterType != typeof(long)
                         || parameters[2].ParameterType != typeof(bool)
                         || parameters[3].ParameterType != typeof(long) )
                    {
                        _unitTestResults[BuildAreaName] = false;
                        Essentials.Log.Error( "BuildBlockHandler failed unit test 6!" );
                        return false;
                    }

                    _unitTestResults[BuildAreaName] = true;
                }

                return _unitTestResults[BuildAreaName];
            }
            return false;
        }

        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if ( !PluginSettings.Instance.ProtectedEnabled )
                return false;

            //Essentials.Log.Debug( "entering buildblockhandler" );

            MyCubeGrid grid = obj as MyCubeGrid;
            if ( grid == null )
            {
                Essentials.Log.Debug( "Null grid in BuildBlockHandler" );
                return false;
            }
            
            foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
            {
                if ( !item.Enabled )
                    continue;

                if ( item.EntityId != grid.EntityId && item.EntityId != -1)
                {
                    //Essentials.Log.Debug( item.EntityId );
                    //Essentials.Log.Debug( grid.EntityId );
                    continue;
                }

                if ( !item.ProtectionSettingsDict.Dictionary.ContainsKey( ProtectedItem.ProtectionModeEnum.BlockAdd ) )
                    continue;

                ProtectedItem.ProtectionSettings settings = item.ProtectionSettingsDict[ProtectedItem.ProtectionModeEnum.BlockAdd];

                if ( Protection.Instance.CheckPlayerExempt( settings, grid, remoteUserId ) )
                    continue;

                if ( item.LogOnly )
                {
                    Essentials.Log.Info( $"Recieved block add request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
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

                Essentials.Log.Info( $"Intercepted block add request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );

                switch ( settings.PunishmentType )
                {
                    case ProtectedItem.PunishmentEnum.Kick:
                        _kickTimer.Elapsed += ( sender, e ) =>
                                              {
                                                  Essentials.Log.Info( $"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for adding blocks to protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
                                                  MyMultiplayer.Static.KickClient( remoteUserId );
                                              };
                        _kickTimer.AutoReset = false;
                        _kickTimer.Start( );
                        break;
                    case ProtectedItem.PunishmentEnum.Ban:
                        _kickTimer.Elapsed += ( sender, e ) =>
                                              {
                                                  Essentials.Log.Info( $"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for adding blocks to protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
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
