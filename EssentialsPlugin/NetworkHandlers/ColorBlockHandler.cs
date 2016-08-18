namespace EssentialsPlugin.NetworkHandlers
{
    using System;
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

    public class ColorBlockHandler : NetworkHandlerBase
    {
        private static bool? _unitTestResult;
        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name != "ColorBlockRequest" )
            {
                return false;
            }

            if ( _unitTestResult == null )
            {
                //make sure Keen hasn't changed the method somehow
                //private void ColorBlockRequest(Vector3I min, Vector3I max, Vector3 newHSV, bool playSound)
                var parameters = site.MethodInfo.GetParameters();
                if ( parameters.Length != 4 )
                {
                    _unitTestResult = false;
                    Essentials.Log.Error( "Failed unit test in ColorBlockHandler!" );
                    return false;
                }

                if ( parameters[0].ParameterType != typeof(Vector3I)
                     || parameters[1].ParameterType != typeof(Vector3I)
                     || parameters[2].ParameterType != typeof(Vector3)
                     || parameters[3].ParameterType != typeof(bool) )
                {
                    _unitTestResult = false;
                    Essentials.Log.Error("Failed unit test in ColorBlockHandler!");
                    return false;
                }

                _unitTestResult = true;
            }

            return _unitTestResult.Value;
        }

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if ( !PluginSettings.Instance.ProtectedEnabled )
                return false;

            var grid = obj as MyCubeGrid;
            if ( grid == null )
            {
                Essentials.Log.Debug( "null grid in ColorBlockHandler" );
                return false;
            }
            
            /*
            Vector3I min = new Vector3I( );
            Vector3I max = new Vector3I( );
            Vector3 hsv = new Vector3( );
            bool playsound = false;

            base.Serialize<Vector3I, Vector3I, Vector3, bool>( site.MethodInfo, stream, ref min, ref max, ref hsv, ref playsound );

            Essentials.Log.Debug( min );
            Essentials.Log.Debug( max );
            Essentials.Log.Debug( hsv );
            Essentials.Log.Debug( playsound );
            */

            bool found = false;
            foreach ( var item in PluginSettings.Instance.ProtectedItems )
            {
                if (!item.Enabled)
                    continue;

                if ( item.EntityId != grid.EntityId )
                    continue;

                if (!item.ProtectionSettingsDict.Dictionary.ContainsKey(ProtectedItem.ProtectionModeEnum.BlockPaint))
                    continue;

                var settings = item.ProtectionSettingsDict[ProtectedItem.ProtectionModeEnum.BlockPaint];

                if ( Protection.Instance.CheckPlayerExempt( settings, grid, remoteUserId ) )
                    continue;

                if ( item.LogOnly )
                {
                    Essentials.Log.Info($"Recieved block color request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for grid {grid.DisplayNameText??"ID"}:{item.EntityId}");
                    continue;
                }

                if (!string.IsNullOrEmpty(settings.PrivateWarningMessage))
                    Communication.Notification(remoteUserId, MyFontEnum.Red, 5000, settings.PrivateWarningMessage);

                if (!string.IsNullOrEmpty(settings.PublicWarningMessage))
                    Communication.SendPublicInformation(settings.PublicWarningMessage.Replace("%player%", PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)));

                if (settings.BroadcastGPS)
                {
                    var player = MySession.Static.Players.GetPlayerById(new MyPlayer.PlayerId(remoteUserId, 0));
                    var pos = player.GetPosition();
                    MyAPIGateway.Utilities.SendMessage($"GPS:{player.DisplayName}:{pos.X}:{pos.Y}:{pos.Z}:");
                }
                
                Essentials.Log.Info($"Intercepted block color request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for grid {grid.DisplayNameText??"ID"}:{item.EntityId}" );

                switch ( settings.PunishmentType )
                {
                    case ProtectedItem.PunishmentEnum.Kick:
                        _kickTimer.Elapsed += ( sender, e ) =>
                                              {
                                                  Essentials.Log.Info($"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for painting protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}" );
                                                  MyMultiplayer.Static.KickClient( remoteUserId );
                                              };
                        _kickTimer.AutoReset = false;
                        _kickTimer.Start();
                        break;
                    case ProtectedItem.PunishmentEnum.Ban:
                        _kickTimer.Elapsed += (sender, e) =>
                                              {
                                                  Essentials.Log.Info($"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for painting protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}");
                                                  MyMultiplayer.Static.BanClient( remoteUserId, true );
                                              };
                        _kickTimer.AutoReset = false;
                        _kickTimer.Start();
                        break;
                    case ProtectedItem.PunishmentEnum.Speed:
                        Task.Run(() =>
                                 {
                                     lock (ProcessSpeed.SpeedPlayers)
                                     {
                                         long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(remoteUserId);
                                         ProcessSpeed.SpeedPlayers[playerId] = new Tuple<float, DateTime>((float)settings.SpeedLimit, DateTime.Now + TimeSpan.FromMinutes(settings.SpeedTime));
                                     }
                                 });
                        Essentials.Log.Info($"Limited user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)} to {settings.SpeedLimit}m/s for {settings.SpeedTime} minutes");
                        break;
                }

                found = true;
            }
            return found;
        }
    }
}
