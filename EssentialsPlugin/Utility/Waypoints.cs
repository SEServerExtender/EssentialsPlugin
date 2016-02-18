namespace EssentialsPlugin.Utility
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Xml.Serialization;
	using EssentialsPlugin.Settings;
	using NLog;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using VRageMath;

	public class Waypoints
	{
		private static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
		private static Waypoints m_instance;
		public static Waypoints Instance
		{
			get
			{
				if (m_instance == null)
				{
					Load();
				}

				return m_instance;
			}
		}

		private List<WaypointItem> waypointItems = new List<WaypointItem>();
		public List<WaypointItem> WaypointItems
		{
			get { return waypointItems; }
			set { waypointItems = value; }
		}

		private static void Load()
		{
			try
			{
				String fileName = Essentials.PluginPath + "Essential-Waypoints.xml";
				if (File.Exists(fileName))
				{
					using (StreamReader reader = new StreamReader(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(Waypoints));
						m_instance = (Waypoints)x.Deserialize(reader);
						reader.Close();
					}
				}
				else
					m_instance = new Waypoints();
			}
			catch (Exception ex)
			{
				Log.Info(string.Format("Waypoint Load Error: {0}", ex.ToString()));
			}
		}

		private static void Save()
		{
			try
			{
				lock (Instance)
				{
					string fileName = Essentials.PluginPath + "Essential-Waypoints.xml";
					string fileNameNew = Essentials.PluginPath + "Essential-Waypoints.xml.new";
					using (StreamWriter writer = new StreamWriter(fileNameNew))
					{
						XmlSerializer x = new XmlSerializer(typeof(Waypoints));
						x.Serialize(writer, m_instance);
						writer.Close();
					}

					if (File.Exists(fileName))
						File.Delete(fileName);

					File.Move(fileNameNew, fileName);
				}
			}
			catch (Exception ex)
			{
				Log.Info(string.Format("Error saving Waypoints: {0}", ex.ToString()));
			}
		}

		public void Add(WaypointItem item)
		{
			lock (Instance)
			{
				waypointItems.Add(item);
				Save();
			}
		}

		public void Remove(ulong steamId, string name)
		{
			lock (Instance)
			{
				waypointItems.RemoveAll(x => x.SteamId == steamId && x.Name.ToLower() == name.ToLower());
				Save();
			}
		}

		public List<WaypointItem> Get(ulong steamId)
		{
			lock (Instance)
			{
				return waypointItems.Where(x => x.SteamId == steamId).ToList();
			}
		}

		public bool GroupAdd(ulong steamId, string name, string group)
		{
			lock (Instance)
			{
				WaypointItem item = waypointItems.FirstOrDefault(x => x.SteamId == steamId && x.Name.ToLower() == name.ToLower());
				if (item == null)
					return false;

				item.Group = group;
				Save();

				return true;
			}
		}

		public bool GroupRemove(ulong steamId, string name)
		{
			lock (Instance)			
			{
				WaypointItem item = waypointItems.FirstOrDefault(x => x.SteamId == steamId && x.Name.ToLower() == name.ToLower());
				if (item == null)
					return false;

				item.Group = "";
				Save();

				return true;
			}
		}

		public bool GroupExists(ulong steamId, string group)
		{
			lock (Instance)
			{
				WaypointItem item = waypointItems.FirstOrDefault(x => x.SteamId == steamId && x.Group.ToLower() == group.ToLower());
				if (item != null)
					return true;

				long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(steamId);
				IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
				if (faction != null)
				{
					item = waypointItems.FirstOrDefault(x => x.SteamId == (ulong)faction.FactionId && x.Group.ToLower() == group.ToLower());
					if (item != null)
						return true;
				}

				return false;
			}
		}

		public void Toggle(ulong steamId, string group = "")
		{
			lock (Instance)
			{
				foreach (WaypointItem item in waypointItems.Where(x => x.SteamId == steamId))
				{
					if (group == "")
					{
						ToggleItem(steamId, item);
					}
					else
					{
						if (item.Group.ToLower() == group.ToLower())
						{
							ToggleItem(steamId, item);
						}
					}
				}

				long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(steamId);
				IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
				if (faction != null)
				{
					foreach (WaypointItem item in waypointItems.Where(x => x.SteamId == (ulong)faction.FactionId))
					{
						if (group == "")
						{
							ToggleItem(steamId, item);
						}
						else
						{
							if (item.Group.ToLower() == group.ToLower())
							{
								ToggleItem(steamId, item);
							}
						}
					}
				}

				Save();
			}
		}

		private static void ToggleItem(ulong steamId, WaypointItem item)
		{
			if (item.Toggle.Contains(steamId))
				item.Toggle.Remove(steamId);
			else
				item.Toggle.Add(steamId);
		}

		public static void SendClientWaypoints(ulong userId)
		{
			List<WaypointItem> items = Instance.Get(userId);
			//string waypoints = "clear";
		    Communication.WaypointMessage( userId, "clear" );
			foreach (WaypointItem item in items)
			{
				if (!item.Toggle.Contains(userId))
                {
                    Communication.WaypointMessage( userId, string.Format( "add \"{0}\" \"{1}\" {2} {3} {4} {5}", item.Name, item.Text, item.WaypointType, Math.Floor( item.Position.X ), Math.Floor( item.Position.Y ), Math.Floor( item.Position.Z ) ) );
                }
            }

			long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(userId);
			IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
			if (faction != null)
			{
				items = Instance.Get((ulong)faction.FactionId);
				foreach (WaypointItem item in items.OrderBy(x => x.Group))
				{
					if (!item.Toggle.Contains(userId))
                    {
                        Communication.WaypointMessage( userId, string.Format( "add \"{0}\" \"{1}\" {2} {3} {4} {5}", item.Name, item.Text, item.WaypointType, Math.Floor( item.Position.X ), Math.Floor( item.Position.Y ), Math.Floor( item.Position.Z ) ) );
                    }
                }
			}

			foreach (ServerWaypointItem item in PluginSettings.Instance.WaypointServerItems)
			{
				if (!item.Enabled)
					continue;

                Communication.WaypointMessage( userId, string.Format( "add \"{0}\" \"{0}\" Neutral {1} {2} {3}", item.Name, item.X, item.Y, item.Z ) );
            }

        }
	}

	public enum WaypointTypes
	{
		Neutral,
		Allied,
		Enemy
	}

	[Serializable]
	public class WaypointItem
	{
		private ulong steamId;
		public ulong SteamId
		{
			get { return steamId; }
			set { steamId = value; }
		}

		private string name;
		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		private string text;
		public string Text
		{
			get { return text; }
			set { text = value; }
		}

		private Vector3D position;
		public Vector3D Position
		{
			get { return position; }
			set { position = value; }
		}

		private WaypointTypes waypointType;
		public WaypointTypes WaypointType
		{
			get { return waypointType; }
			set { waypointType = value; }
		}

		private string group = "";
		public string Group
		{
			get { return group; }
			set { group = value; }
		}

		private bool leader = false;
		public bool Leader
		{
			get { return leader; }
			set { leader = value; }
		}

		private List<ulong> toggle = new List<ulong>();
		public List<ulong> Toggle
		{
			get { return toggle; }
		}
	}
}
