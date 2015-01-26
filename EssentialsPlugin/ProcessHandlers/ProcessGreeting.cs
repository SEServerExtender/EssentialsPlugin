using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using SEModAPIInternal.API.Common;

using EssentialsPlugin.Utility;
using EssentialsPlugin.Settings;

namespace EssentialsPlugin.ProcessHandler
{
	class GreetingItem
	{
		private DateTime start;
		public DateTime Start
		{
			get { return start; }
			set { start = value; }
		}

		private ulong steamId;
		public ulong SteamId
		{
			get { return steamId; }
			set { steamId = value; }
		}

		private TimeSpan timeout;
		public TimeSpan Timeout
		{
			get { return timeout; }
			set { timeout = value; }
		}

		private bool isNewUser;
		public bool IsNewUser
		{
			get { return isNewUser; }
			set { isNewUser = value; }
		}
	}

	public class ProcessGreeting : ProcessHandlerBase
	{
		private List<GreetingItem> m_greetingList = new List<GreetingItem>();
		private DateTime m_start = DateTime.Now;

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (PluginSettings.Instance.GreetingMessage != "")
			{
				if (MyAPIGateway.Players == null)
					return;

				int pos = 0;
				try
				{
					List<IMyPlayer> players = new List<IMyPlayer>();
					pos = 1;
					bool result = false;
					Wrapper.GameAction(() =>
					{
						try
						{
							MyAPIGateway.Players.GetPlayers(players, null);
							result = true;
						}
						catch (Exception ex)
						{
							Logging.WriteLineAndConsole(string.Format("Failed to get player list: {0}", ex.ToString()));
						}
					});

					if(!result)
						return;

					pos = 2;
					lock (m_greetingList)
					{
						for (int r = m_greetingList.Count - 1; r >= 0; r--)
						{
							pos = 3;
							GreetingItem item = m_greetingList[r];
							if(DateTime.Now - item.Start > item.Timeout)
							{
								m_greetingList.RemoveAt(r);
								continue;
							}
							pos = 4;
							IMyPlayer player = players.FirstOrDefault(x => x.SteamUserId == item.SteamId && x.Controller != null && x.Controller.ControlledEntity != null);
							pos = 5;
							if (player != null)
							{
								pos = 6;
								m_greetingList.RemoveAt(r);

								string message = "";

								if (item.IsNewUser)
									message = PluginSettings.Instance.GreetingNewUserMessage.Replace("%name%", player.DisplayName);
								else
									message = PluginSettings.Instance.GreetingMessage.Replace("%name%", player.DisplayName);

								string finalMessage = message;

								if(PluginSettings.Instance.GreetingPublic)
									Communication.SendPublicInformation(finalMessage);
								else
									Communication.SendPrivateInformation(item.SteamId, finalMessage);

								if (item.IsNewUser)
								{
									if (PluginSettings.Instance.GreetingNewUserItem.Enabled)
									{
										SettingsGreetingDialogItem gItem = PluginSettings.Instance.GreetingNewUserItem;
										Communication.SendClientMessage(item.SteamId, string.Format("/dialog \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"", gItem.Title.Replace("%name%", player.DisplayName), gItem.Header.Replace("%name%", player.DisplayName), " ", gItem.Contents.Replace("%name%", player.DisplayName).Replace("\r", "").Replace("\n", "|").Replace("\"", "'"), gItem.ButtonText));
									}
								}
								else
								{
									if (PluginSettings.Instance.GreetingItem.Enabled)
									{
										SettingsGreetingDialogItem gItem = PluginSettings.Instance.GreetingItem;
										Communication.SendClientMessage(item.SteamId, string.Format("/dialog \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"", gItem.Title.Replace("%name%", player.DisplayName), gItem.Header.Replace("%name%", player.DisplayName), " ", gItem.Contents.Replace("%name%", player.DisplayName).Replace("\r", "").Replace("\n", "|").Replace("\"", "'"), gItem.ButtonText));
									}
								}

							}
						}

						pos = 7;

					}
				}
				catch (Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("Handle(): Error at pos - {0}: {1}", pos, ex.ToString()));
				}
			}

			base.Handle();
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			GreetingItem item = new GreetingItem();
			item.SteamId = remoteUserId;
			item.Timeout = TimeSpan.FromMinutes(10);
			item.Start = DateTime.Now;
			item.IsNewUser = !PlayerMap.Instance.GetPlayerIdsFromSteamId(remoteUserId).Any();

			Logging.WriteLineAndConsole(string.Format("New User: {0}", remoteUserId));

			lock (m_greetingList)
			{
				m_greetingList.Add(item);
				Logging.WriteLineAndConsole(string.Format("Greeting Added => {0} (New user: {1})", remoteUserId, item.IsNewUser));
			}

			base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerLeft(ulong remoteUserId)
		{
			lock (m_greetingList)
			{
				if (m_greetingList.Find(x => x.SteamId == remoteUserId) != null)
				{
					Logging.WriteLineAndConsole(string.Format("Greeting Removed => {0}", remoteUserId));
					m_greetingList.RemoveAll(x => x.SteamId == remoteUserId);
				}
			}

			base.OnPlayerLeft(remoteUserId);
		}
	}
}

