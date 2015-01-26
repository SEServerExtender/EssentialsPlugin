namespace EssentialsPlugin.Utility
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Xml.Serialization;

	public class Docking
	{
		private static Docking m_instance;
		private static bool m_loading = false;

		private List<DockingItem> m_dockingItems = new List<DockingItem>();

		public Docking()
		{
			if (!Directory.Exists(Essentials.PluginPath + "\\Docking"))
				Directory.CreateDirectory(Essentials.PluginPath + "\\Docking");
		}

		public static Docking Instance
		{
			get 			
			{
				if (Docking.m_instance == null)
				{
					Docking.m_instance = new Docking();
					Docking.m_instance.Load();
				}

				return Docking.m_instance; 
			}
		}

		public List<DockingItem> DockingItems
		{
			get { return m_dockingItems; }
		}

		public void Save()
		{
			if (m_loading)
				return;

			lock(m_instance)
			{
				try
				{
					string fileName = Essentials.PluginPath + "Eessentials-Docking.xml";
					using (StreamWriter writer = new StreamWriter(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(Docking));
						x.Serialize(writer, m_instance);
						writer.Close();
					}					
				}
				catch (Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("Save(): {0}", ex));
				}
			}
		}

		public void Load()
		{
			if (m_loading)
				return;

			lock (m_instance)
			{
				try
				{
					m_loading = true;
					string fileName = Essentials.PluginPath + "Eessentials-Docking.xml";

					if (File.Exists(fileName))
					{
						using (StreamReader reader = new StreamReader(fileName))
						{
							XmlSerializer x = new XmlSerializer(typeof(Docking));
							m_instance = (Docking)x.Deserialize(reader);
							reader.Close();
						}

						Logging.WriteLineAndConsole(string.Format("Loaded {0} Docking Items", m_instance.DockingItems));
					}
				}
				catch (Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("Load(): {0}", ex));
				}
				finally
				{
					m_loading = false;
				}
			}
		}

		public void Add(DockingItem item)
		{
			lock(m_instance)
				m_dockingItems.Add(item);

			Save();
		}

		public List<DockingItem> Find(Func<DockingItem, bool> search)
		{
			lock (m_instance)
				return m_dockingItems.Where(search).ToList();
		}

		public void Remove(DockingItem item)
		{
			lock (m_instance)
				m_dockingItems.Remove(item);

			Save();
		}
	}
}