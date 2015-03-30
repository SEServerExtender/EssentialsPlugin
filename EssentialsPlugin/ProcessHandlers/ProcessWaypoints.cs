namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using VRageMath;

	public class ProcessWaypoints : ProcessHandlerBase
	{
		private List<ulong> m_waypointAdd = new List<ulong>( );

		public ProcessWaypoints()
		{

		}

		public override int GetUpdateResolution()
		{
			return 5000;
		}

		public override void Handle()
		{
			lock (m_waypointAdd)
			{
				if(m_waypointAdd.Count < 1)
					return;
			}

			if (MyAPIGateway.Players == null)
				return;

			List<IMyPlayer> players = new List<IMyPlayer>();
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
					Log.Info(string.Format("Waypoints(): Unable to get player list: {0}", ex.ToString()));
				}
			});

			if (!result)
				return;

			lock (m_waypointAdd)
			{
				for (int r = m_waypointAdd.Count - 1; r >= 0; r--)
				{
					ulong steamId = m_waypointAdd[r];

					IMyPlayer player = players.FirstOrDefault(x => x.SteamUserId == steamId && x.Controller != null && x.Controller.ControlledEntity != null);
					if (player != null)
					{
						Log.Info("Player in game, creating waypoints");
						m_waypointAdd.Remove(steamId);

						// Add defaults
						if (Waypoints.Instance.Get(steamId).Count < 1)
						{
							foreach (ServerWaypointItem item in PluginSettings.Instance.WaypointDefaultItems)
							{
								WaypointItem newItem = new WaypointItem();
								newItem.Name = item.Name;
								newItem.Text = item.Name;
								newItem.WaypointType = WaypointTypes.Neutral;
								newItem.Position = new Vector3D(item.X, item.Y, item.Z);
								newItem.SteamId = steamId;
								Waypoints.Instance.Add(newItem);
							}
						}

						Waypoints.SendClientWaypoints(steamId);
					}
				}
			}

			base.Handle();
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			if (!PluginSettings.Instance.WaypointsEnabled)
				return;

			lock(m_waypointAdd)
			{
				//if (Waypoints.Instance.Get(remoteUserId).Count < 1)
					//return;

				m_waypointAdd.Add(remoteUserId);
			}

			base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerLeft(ulong remoteUserId)
		{
			lock (m_waypointAdd)
			{
				m_waypointAdd.RemoveAll(x => x == remoteUserId);
			}

			base.OnPlayerLeft(remoteUserId);
		}

	}
}

