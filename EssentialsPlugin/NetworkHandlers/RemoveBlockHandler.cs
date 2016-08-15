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

    public class RemoveBlockHandler : NetworkHandlerBase
    {
        private static Dictionary<string, bool> _unitTestResults = new Dictionary<string, bool>();
        private const string RazeBlocksName = "RazeBlocksRequest";
        private const string RazeAreaName = "RazeBlocksAreaRequest";

        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name == RazeBlocksName )
            {
                if ( !_unitTestResults.ContainsKey( RazeBlocksName ) )
                {
                    //public void RazeBlocks(List<Vector3I> locations)
                    var parameters = site.MethodInfo.GetParameters();
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
            else if ( site.MethodInfo.Name == RazeAreaName )
            {
                if ( !_unitTestResults.ContainsKey( RazeAreaName ) )
                {
                    //void RazeBlocksAreaRequest(Vector3I pos, Vector3UByte size)
                    var parameters = site.MethodInfo.GetParameters();
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

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if (!PluginSettings.Instance.ProtectedEnabled)
                return false;

            var grid = obj as MyCubeGrid;
            if (grid == null)
            {
                Essentials.Log.Debug("Null grid in RemoveBlockHandler");
                return false;
            }

            bool found = false;
            foreach (var item in PluginSettings.Instance.ProtectedItems)
            {
                if (!item.Enabled)
                    continue;

                if (item.EntityId != grid.EntityId)
                    continue;

                if (!item.ProtectionSettingsDict.Dictionary.ContainsKey(ProtectedItem.ProtectionModeEnum.BlockRemove))
                    continue;

                var settings = item.ProtectionSettingsDict[ProtectedItem.ProtectionModeEnum.BlockRemove];

                if (Protection.Instance.CheckPlayerExempt(settings, grid, remoteUserId))
                    continue;

                if (item.LogOnly)
                {
                    Essentials.Log.Info($"Recieved block remove request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}");
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

                Essentials.Log.Info($"Intercepted block remove request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}");

                if (settings.PunishmentType == ProtectedItem.PunishmentEnum.Kick)
                {
                    _kickTimer.Elapsed += (sender, e) =>
                                         {
                                             Essentials.Log.Info($"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for removing blocks from protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}");
                                             MyMultiplayer.Static.KickClient(remoteUserId);
                                         };
                    _kickTimer.Start();
                }
                else if (settings.PunishmentType == ProtectedItem.PunishmentEnum.Ban)
                {
                    _kickTimer.Elapsed += (sender, e) =>
                                         {
                                             Essentials.Log.Info($"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for removing blocks from protected grid {grid.DisplayNameText ?? "ID"}:{item.EntityId}");
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
