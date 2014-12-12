using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

using VRageMath;

namespace EssentialsPlugin.Utility
{
	public class Waypoints
	{
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
				Logging.WriteLineAndConsole(string.Format("Waypoint Load Error: {0}", ex.ToString()));
			}
		}

		private static void Save()
		{
			try
			{
				lock (Instance)
				{
					String fileName = Essentials.PluginPath + "Essential-Waypoints.xml";
					using (StreamWriter writer = new StreamWriter(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(Waypoints));
						x.Serialize(writer, m_instance);
						writer.Close();
					}
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("Error saving Waypoints: {0}", ex.ToString()));
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
	}

	public enum WaypointTypes
	{
		Neutral,
		Allied,
		Enemy
	}

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
	}

}
