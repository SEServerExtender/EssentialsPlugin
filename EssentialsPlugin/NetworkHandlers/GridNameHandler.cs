using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssentialsPlugin.NetworkHandlers
{
    using System.Reflection;
    using System.Timers;
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
        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name != "OnChangeDisplayNameRequest" )
                return false;

            if ( _unitTestResult == null )
            {
                //void OnChangeDisplayNameRequest(String displayName)
                var parameters = site.MethodInfo.GetParameters();
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

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if ( !PluginSettings.Instance.ProtectedEnabled )
                return false;

            var grid = obj as MyCubeGrid;
            if ( grid == null )
            {
                Essentials.Log.Debug( "Null grid in GridNameHandler" );
                return false;
            }

            bool found = false;
            foreach ( var item in PluginSettings.Instance.ProtectedItems )
            {
                if ( !item.Enabled )
                    continue;

                if ( item.EntityId != grid.EntityId )
                    continue;

                if(!item.ProtectionSettingsDict.Dictionary.ContainsKey( ProtectedItem.ProtectionModeEnum.GridRename ))
                    continue;

                var settings = item.ProtectionSettingsDict[ProtectedItem.ProtectionModeEnum.GridRename];
                
                if ( Protection.Instance.CheckPlayerExempt( settings, grid, remoteUserId ) )
                    continue;
                
                if (item.LogOnly)
                {
                    Essentials.Log.Info($"Recieved grid rename request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}");
                    continue;
                }

                if (!string.IsNullOrEmpty(settings.PrivateWarningMessage))
                    Communication.Notification(remoteUserId, MyFontEnum.Red, 5000, settings.PrivateWarningMessage);

                if(!string.IsNullOrEmpty( settings.PublicWarningMessage ))
                    Communication.SendPublicInformation( settings.PublicWarningMessage.Replace( "%player%",PlayerMap.Instance.GetFastPlayerNameFromSteamId( remoteUserId ) ) );

                if ( settings.BroadcastGPS )
                {
                    var player = MySession.Static.Players.GetPlayerById( new MyPlayer.PlayerId( remoteUserId, 0 ) );
                    var pos = player.GetPosition();
                    MyAPIGateway.Utilities.SendMessage($"GPS:{player.DisplayName}:{pos.X}:{pos.Y}:{pos.Z}:");
                }

                Essentials.Log.Info($"Intercepted grid rename request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}");

                if (settings.PunishmentType == ProtectedItem.PunishmentEnum.Kick)
                {
                    _kickTimer.Elapsed += (sender, e) =>
                                         {
                                             Essentials.Log.Info($"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for renaming protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}");
                                             MyMultiplayer.Static.KickClient(remoteUserId);
                                         };
                    _kickTimer.Start();
                }
                else if (settings.PunishmentType == ProtectedItem.PunishmentEnum.Ban)
                {
                    _kickTimer.Elapsed += (sender, e) =>
                                         {
                                             Essentials.Log.Info($"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for renaming protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}");
                                             MyMultiplayer.Static.BanClient(remoteUserId, true);
                                         };
                    _kickTimer.Start();
                }

                found = true;
            }
            
            return found;
        }
    }
}
