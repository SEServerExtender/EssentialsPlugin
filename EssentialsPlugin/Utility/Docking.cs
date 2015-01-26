namespace EssentialsPlugin.Utility
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Xml.Serialization;

	public class Docking
	{
		private static Docking _instance;
		private static bool _loading;

		private readonly List<DockingItem> _dockingItems = new List<DockingItem>();

		public Docking()
		{
			string dockingFolderPath = Path.Combine(Essentials.PluginPath , "\\Docking");
			if (!Directory.Exists(dockingFolderPath))
				Directory.CreateDirectory(dockingFolderPath);
		}

		public static Docking Instance
		{
			get 			
			{
				if (_instance == null)
				{
					_instance = new Docking();
					_instance.Load();
				}

				return _instance; 
			}
		}

		public List<DockingItem> DockingItems
		{
			get { return _dockingItems; }
		}

		public void Save()
		{
			if (_loading)
				return;

			lock(_instance)
			{
				try
				{
					string fileName = Essentials.PluginPath + "Eessentials-Docking.xml";
					using (StreamWriter writer = new StreamWriter(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(Docking));
						x.Serialize(writer, _instance);
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
			if (_loading)
				return;

			lock (_instance)
			{
				try
				{
					_loading = true;
					string fileName = Essentials.PluginPath + "Eessentials-Docking.xml";

					if (File.Exists(fileName))
					{
						using (StreamReader reader = new StreamReader(fileName))
						{
							XmlSerializer x = new XmlSerializer(typeof(Docking));
							_instance = (Docking)x.Deserialize(reader);
							reader.Close();
						}

						Logging.WriteLineAndConsole(string.Format("Loaded {0} Docking Items", _instance.DockingItems));
					}
				}
				catch (Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("Load(): {0}", ex));
				}
				finally
				{
					_loading = false;
				}
			}
		}

		public void Add(DockingItem item)
		{
			lock(_instance)
				_dockingItems.Add(item);

			Save();
		}

		public List<DockingItem> Find(Func<DockingItem, bool> search)
		{
			lock (_instance)
				return _dockingItems.Where(search).ToList();
		}

		public void Remove(DockingItem item)
		{
			lock (_instance)
				_dockingItems.Remove(item);

			Save();
		}
	}
}