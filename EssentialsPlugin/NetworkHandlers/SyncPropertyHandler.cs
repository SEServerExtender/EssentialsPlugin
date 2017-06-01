namespace EssentialsPlugin.NetworkHandlers
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Timers;
    using ProcessHandlers;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using Sandbox.Game.Replication;
    using Sandbox.Game.Replication.StateGroups;
    using Sandbox.Game.World;
    using Sandbox.ModAPI;
    using Settings;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Server;
    using SEModAPIInternal.Support;
    using SpaceEngineers.Game.Entities.Blocks;
    using Utility;
    using VRage.Collections;
    using VRage.Game;
    using VRage.Library.Collections;
    using VRage.Network;
    using VRage.Sync;
    using VRageMath;

    public class SyncPropertyHandler : NetworkHandlerBase
    {
        private readonly Timer _kickTimer = new Timer( 30000 );
        private bool? _unitTestResult;

        public override bool CanHandle( CallSite site )
        {
            //TODO
            return false;
            if ( !site.MethodInfo.Name.Contains( "SyncPropertyChanged" ) )
                return false;

            if ( _unitTestResult.HasValue )
                return _unitTestResult.Value;

            ParameterInfo[] parameters = site.MethodInfo.GetParameters( );
            if ( parameters.Length != 2 )
            {
                _unitTestResult = false;
                return false;
            }
            if ( parameters[0].ParameterType != typeof(byte)
                 || parameters[1].ParameterType != typeof(BitReaderWriter) )
            {
                _unitTestResult = false;
                return false;
            }

            _unitTestResult = true;
            return true;
        }

        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            //TODO
            return false;
            //MyPropertySyncStateGroup sync = (MyPropertySyncStateGroup)obj;

            //var properties = (ListReader<SyncBase>)sync.GetType().GetField("m_properties", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sync);

            //byte index = 0;
            //BitReaderWriter bits = new BitReaderWriter();

            //Serialize(site.MethodInfo, stream, ref index, ref bits);

            //MyTerminalBlock entity = null;

            //MyExternalReplicable<MySyncedBlock> rep = sync.Owner as MyExternalReplicable<MySyncedBlock>;

            //if ( rep == null )
            //{
            //    //there are lots of reasons this wouldn't be MySyncedBlock, so just ignore it and move on
            //    return false;
            //}

            //entity = rep.Instance as MyTerminalBlock;
            
            //MyCubeGrid grid = entity?.CubeGrid;

            //if ( grid == null )
            //{
            //    Essentials.Log.Info( "Null grid in SyncPropertyHandler" );
            //    return false;
            //}


            //Essentials.Log.Warn( $"{entity.CustomName} | {index} | {properties[index].ValueType}" );

            //if (entity is MyLandingGear)
            //{
            //    //clients sometimes send updates for landing gear for no discernable reason?
            //    //just ignore it, it's mostly harmless
            //    return false;
            //}

            //bool found = false;
            //foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
            //{
            //    if ( !item.Enabled )
            //        continue;

            //    if ( item.EntityId != grid.EntityId )
            //        continue;
                
            //    if ( !item.ProtectionSettingsDict.Dictionary.ContainsKey( ProtectedItem.ProtectionModeEnum.BlockSettings ) )
            //        continue;

            //    ProtectedItem.ProtectionSettings settings = item.ProtectionSettingsDict[ProtectedItem.ProtectionModeEnum.BlockSettings];
                
            //    if ( Protection.Instance.CheckPlayerExempt( settings, grid, remoteUserId ) )
            //        continue;

            //    if ( item.LogOnly )
            //    {
            //        Essentials.Log.Info( $"Recieved block settings change request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for block {entity.CustomName} on grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
            //        continue;
            //    }

            //    if ( !string.IsNullOrEmpty( settings.PrivateWarningMessage ) )
            //        Communication.Notification( remoteUserId, MyFontEnum.Red, 5000, settings.PrivateWarningMessage );

            //    if ( !string.IsNullOrEmpty( settings.PublicWarningMessage ) )
            //        Communication.SendPublicInformation( settings.PublicWarningMessage.Replace( "%player%", PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId ) ) );

            //    if ( settings.BroadcastGPS )
            //    {
            //        MyPlayer player = MySession.Static.Players.GetPlayerById( new MyPlayer.PlayerId( remoteUserId, 0 ) );
            //        Vector3D pos = player.GetPosition( );
            //        MyAPIGateway.Utilities.SendMessage( $"GPS:{player.DisplayName}:{pos.X}:{pos.Y}:{pos.Z}:" );
            //    }

            //    Essentials.Log.Info($"Intercepted block settings change request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for block {entity.CustomName} on grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );

            //    switch ( settings.PunishmentType )
            //    {
            //        case ProtectedItem.PunishmentEnum.Kick:
            //            _kickTimer.Elapsed += ( sender, e ) =>
            //                                  {
            //                                      Essentials.Log.Info( $"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for changing block settings on protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
            //                                      MyMultiplayer.Static.KickClient( remoteUserId );
            //                                  };
            //            _kickTimer.AutoReset = false;
            //            _kickTimer.Start( );
            //            break;
            //        case ProtectedItem.PunishmentEnum.Ban:
            //            _kickTimer.Elapsed += ( sender, e ) =>
            //                                  {
            //                                      Essentials.Log.Info( $"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )}:{remoteUserId} for changing block settings on protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
            //                                      MyMultiplayer.Static.BanClient( remoteUserId, true );
            //                                  };
            //            _kickTimer.AutoReset = false;
            //            _kickTimer.Start( );
            //            break;
            //        case ProtectedItem.PunishmentEnum.Speed:
            //            Task.Run( ( ) =>
            //                      {
            //                          lock ( ProcessSpeed.SpeedPlayers )
            //                          {
            //                              long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId( remoteUserId );
            //                              ProcessSpeed.SpeedPlayers[playerId] = new Tuple<float, DateTime>( (float)settings.SpeedLimit, DateTime.Now + TimeSpan.FromMinutes( settings.SpeedTime ) );
            //                          }
            //                      } );
            //            Essentials.Log.Info( $"Limited user {PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId )} to {settings.SpeedLimit}m/s for {settings.SpeedTime} minutes" );
            //            break;
            //    }

            //    found = true;
            //}

            //return found;
        }
    }
}
