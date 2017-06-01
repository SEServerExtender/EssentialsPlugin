using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using VRage.Game.ModAPI;
using VRage.Network;
using VRage.Utils;

namespace EssentialsPlugin.ProcessHandlers
{
    public class ProcessReservedSlots : ProcessHandlerBase
    {
        private static List<ulong> _reservedPlayers = new List<ulong>();
        private static List<ulong> _waitingPlayers = new List<ulong>();
        private static bool _init;

        private static string ConvertSteamIDFrom64(ulong from)
        {
            from -= 76561197960265728UL;
            return new StringBuilder("STEAM_").Append("0:").Append(from % 2UL).Append(':').Append(from / 2UL).ToString();
        }

        public static void Init()
        {
            if (_init)
                return;

            _init = true;
            RemoveHandlers();
            SteamServerAPI.Instance.GameServer.UserGroupStatus += GameServer_UserGroupStatus;
            SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse += GameServer_ValidateAuthTicketResponse;

            m_waitingForGroup = (HashSet<ulong>)typeof(MyDedicatedServerBase).GetField("m_waitingForGroup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(MyMultiplayer.Static);
            m_groupId = (ulong)typeof(MyDedicatedServerBase).GetField("m_groupId", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(MyMultiplayer.Static);

            Essentials.Log.Info("Reserved slots initialized");
        }

        private static void RemoveHandlers()
        {
            FieldInfo eventField = typeof(GameServer).GetField("<backing_store>ValidateAuthTicketResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            var eventDel = eventField?.GetValue(SteamServerAPI.Instance.GameServer) as MulticastDelegate;
            if (eventDel != null)
            {
                foreach (Delegate handle in eventDel.GetInvocationList())
                {
                    if (handle.Method.Name == "GameServer_ValidateAuthTicketResponse")
                    {
                        SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse -= handle as ValidateAuthTicketResponse;
                        Essentials.Log.Warn("Removed handler from ValidateAuthTicketResponse");
                    }
                }
            }
            eventField = typeof(GameServer).GetField("<backing_store>UserGroupStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            eventDel = eventField?.GetValue(SteamServerAPI.Instance.GameServer) as MulticastDelegate;
            if (eventDel != null)
            {
                foreach (Delegate handle in eventDel.GetInvocationList())
                {
                    if (handle.Method.Name == "GameServer_UserGroupStatus")
                    {
                        SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse -= handle as ValidateAuthTicketResponse;
                        Essentials.Log.Warn("Removed handler from UserGroupSataus");
                    }
                }
            }
        }

        private static ulong m_groupId;
        private static HashSet<ulong> m_waitingForGroup;
        private static readonly HashSet<ulong> _waitingForWhitelistGroup = new HashSet<ulong>();

        /// <summary>
        ///     Reimplementation of the vanilla event
        /// </summary>
        /// <param name="steamID"></param>
        /// <param name="response"></param>
        /// <param name="steamOwner"></param>
        private static void GameServer_ValidateAuthTicketResponse(ulong steamID, AuthSessionResponseEnum response,
                                                                  ulong steamOwner)
        {
            try
            {
                MyLog.Default.WriteLineAndConsole($"Essentials ValidateAuthTicketResponse ({response}), owner: {steamOwner}");

                if (IsClientBanned(steamOwner) || MySandboxGame.ConfigDedicated.Banned.Contains(steamOwner))
                {
                    UserRejected(steamID, JoinResult.BannedByAdmins);
                    RaiseClientKicked(steamID);
                }
                else
                {
                    if (IsClientKicked(steamOwner))
                    {
                        UserRejected(steamID, JoinResult.KickedRecently);
                        RaiseClientKicked(steamID);
                    }
                }

                if (response == AuthSessionResponseEnum.OK)
                {
                    if (MyMultiplayer.Static.MemberLimit > 0 && MyMultiplayer.Static.MemberCount - 1 >= MyMultiplayer.Static.MemberLimit) // Unfortunately, DS counds into the members, so subtract it
                    {
                        if (!PluginSettings.Instance.ReservedSlotsEnabled)
                        {
                            UserRejected(steamID, JoinResult.ServerFull);
                            return;
                        }

                        if (PluginSettings.Instance.ReservedSlotsPlayers.Contains(steamID.ToString()) || PluginSettings.Instance.ReservedSlotsAdmins && MySession.Static.GetUserPromoteLevel(steamID) >= MyPromoteLevel.Admin)
                        {
                            Essentials.Log.Info($"Added whitelisted player {steamID}");
                            UserAccepted(steamID);
                        }
                        else
                        {
                            if (PluginSettings.Instance.ReservedSlotsGroup != 0)
                            {
                                if (SteamServerAPI.Instance.GameServer.RequestGroupStatus(steamID, PluginSettings.Instance.ReservedSlotsGroup))
                                {
                                    // Returns false when there's no connection to Steam
                                    _waitingForWhitelistGroup.Add(steamID);
                                }
                                else
                                    UserRejected(steamID, JoinResult.SteamServersOffline);
                            }
                            else
                                UserRejected(steamID, JoinResult.ServerFull);
                        }
                    }
                    else
                    {
                        if (m_groupId == 0 || MySandboxGame.ConfigDedicated.Administrators.Contains(steamID.ToString()) || MySandboxGame.ConfigDedicated.Administrators.Contains(ConvertSteamIDFrom64(steamID)))
                        {
                            UserAccepted(steamID);
                        }
                        else
                        {
                            if (SteamServerAPI.Instance.GetAccountType(m_groupId) != AccountType.Clan)
                            {
                                UserRejected(steamID, JoinResult.GroupIdInvalid);
                            }
                            else
                            {
                                if (SteamServerAPI.Instance.GameServer.RequestGroupStatus(steamID, m_groupId))
                                {
                                    // Returns false when there's no connection to Steam
                                    m_waitingForGroup.Add(steamID);
                                }
                                else
                                    UserRejected(steamID, JoinResult.SteamServersOffline);
                            }
                        }
                    }
                }
                else
                {
                    var joinResult = JoinResult.TicketInvalid;
                    switch (response)
                    {
                        case AuthSessionResponseEnum.AuthTicketCanceled:
                            joinResult = JoinResult.TicketCanceled;
                            break;
                        case AuthSessionResponseEnum.AuthTicketInvalidAlreadyUsed:
                            joinResult = JoinResult.TicketAlreadyUsed;
                            break;
                        case AuthSessionResponseEnum.LoggedInElseWhere:
                            joinResult = JoinResult.LoggedInElseWhere;
                            break;
                        case AuthSessionResponseEnum.NoLicenseOrExpired:
                            joinResult = JoinResult.NoLicenseOrExpired;
                            break;
                        case AuthSessionResponseEnum.UserNotConnectedToSteam:
                            joinResult = JoinResult.UserNotConnected;
                            break;
                        case AuthSessionResponseEnum.VACBanned:
                            joinResult = JoinResult.VACBanned;
                            break;
                        case AuthSessionResponseEnum.VACCheckTimedOut:
                            joinResult = JoinResult.VACCheckTimedOut;
                            break;
                    }

                    UserRejected(steamID, joinResult);
                }
            }
            catch (Exception ex)
            {
                Essentials.Log.Error(ex);
            }
        }

        private static void UserRejected(ulong steamId, JoinResult joinResult)
        {
            MethodInfo userRejectedMethod = typeof(MyDedicatedServerBase).GetMethod("UserRejected", BindingFlags.NonPublic | BindingFlags.Instance);
            userRejectedMethod.Invoke(MyMultiplayer.Static, new object[] {steamId, joinResult});
        }

        private static void UserAccepted(ulong steamId)
        {
            MethodInfo userAcceptedMethod = typeof(MyDedicatedServerBase).GetMethod("UserAccepted", BindingFlags.NonPublic | BindingFlags.Instance);
            userAcceptedMethod.Invoke(MyMultiplayer.Static, new object[] {steamId});
        }

        private static bool IsClientBanned(ulong steamId)
        {
            return (bool)typeof(MyMultiplayerBase).GetMethod("IsClientBanned", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(MyMultiplayer.Static, new object[] {steamId});
        }

        private static bool IsClientKicked(ulong steamId)
        {
            return (bool)typeof(MyMultiplayerBase).GetMethod("IsClientKicked", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(MyMultiplayer.Static, new object[] {steamId});
        }

        private static void RaiseClientKicked(ulong steamId)
        {
            typeof(MyMultiplayerBase).GetMethod("RaiseClientKicked", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(MyMultiplayer.Static, new object[] {steamId});
        }

        private static void GameServer_UserGroupStatus(ulong userId, ulong groupId, bool member, bool officier)
        {
            if (PluginSettings.Instance.ReservedSlotsEnabled && groupId == PluginSettings.Instance.ReservedSlotsGroup && _waitingForWhitelistGroup.Remove(userId))
            {
                if (member || officier)
                {
                    Essentials.Log.Info("Whitelisted player connected: " + userId);
                    UserAccepted(userId);
                }
                else
                    UserRejected(userId, JoinResult.ServerFull);
            }
            else
            {
                if (groupId == m_groupId && m_waitingForGroup.Remove(userId))
                {
                    if (member || officier)
                        UserAccepted(userId);
                    else
                        UserRejected(userId, JoinResult.NotInGroup);
                }
            }
        }
    }
}
