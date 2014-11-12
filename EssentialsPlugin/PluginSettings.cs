using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EssentialsPlugin.Utility;
using EssentialsPlugin.UtilityClasses;
using VRage.Common.Utils;

namespace EssentialsPlugin
{
	[Serializable]
	public class PluginSettings
	{
		#region Private Fields
		private static PluginSettings m_instance;
		private static bool m_loading = false;
		private static DateTime m_start;

		private bool m_informationEnabled;
		private MTObservableCollection<InformationItem> m_informationItems;

		private bool m_greetingEnabled;
		private string m_greetingMessage;
		private string m_greetingNewUserMessage;

		private bool m_restartEnabled;
		private MTObservableCollection<RestartItem> m_restartItems;
		private int m_restartTime;

		private bool m_backupEnabled;
		private MTObservableCollection<BackupItem> m_backupItems;
		private bool m_backupCreateSubDirectories;
		private string m_backupBaseDirectory;
		private bool m_backupCleanup;
		private int m_backupCleanupTime;
		private bool m_backupAsteroids;

		private bool m_newUserTransportEnabled;
		private int m_newUserTransportDistance;

		#endregion

		#region Static Properties
		public static DateTime RestartStartTime
		{
			get
			{
				return m_start;
			}
		}

		public static PluginSettings Instance
		{
			get
			{
				if (m_instance == null)
					m_instance = new PluginSettings();

				return m_instance;
			}
		}
		#endregion

		#region Properties

		// Information
		public bool InformationEnabled
		{
			get { return m_informationEnabled; }
			set 
			{ 
				m_informationEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<InformationItem> InformationItems
		{
			get { return m_informationItems; }
		}

		// Automated Restarts
		public bool RestartEnabled
		{
			get { return m_restartEnabled; }
			set 
			{
				m_restartEnabled = value;
				Save();

				if (m_restartEnabled)
					Communication.SendPublicInformation("[NOTICE]: Automatic restarts have been enabled by the Administrator.  You will be notified in advance before a restart occurs.");
				else
					Communication.SendPublicInformation("[NOTICE]: Automatic restarts have been disabled by the Administrator.");
			}
		}

		public int RestartTime
		{
			get { return m_restartTime; }
			set 
			{ 
				m_restartTime = value;
				Save();

				if(m_restartEnabled)
				{
					Communication.SendPublicInformation(string.Format("[NOTICE]: Automatic restart time has been changed by the Administrator.  The new time is: {0}", General.TimeSpanToString(TimeSpan.FromMinutes(m_restartTime))));
				}

				foreach (RestartItem item in m_restartItems)
					item.completed = false;
			}
		}

		public MTObservableCollection<RestartItem> RestartItems
		{
			get { return m_restartItems; }
			set { m_restartItems = value; }
		}

		// Greetings

		public bool GreetingEnabled
		{
			get { return m_greetingEnabled; }
			set 
			{ 
				m_greetingEnabled = value;
				Save();
			}
		}

		public string GreetingMessage
		{
			get { return m_greetingMessage; }
			set 
			{ 
				m_greetingMessage = value;
				Save();
			}
		}

		public string GreetingNewUserMessage
		{
			get { return m_greetingNewUserMessage; }
			set
			{
				m_greetingNewUserMessage = value;
				Save();
			}
		}

		// Automated Backups
		public bool BackupEnabled
		{
			get { return m_backupEnabled; }
			set 
			{ 
				m_backupEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<BackupItem> BackupItems
		{
			get { return m_backupItems; }
			set 
			{ 
				m_backupItems = value;
			}
		}

		public bool BackupCreateSubDirectories
		{
			get { return m_backupCreateSubDirectories; }
			set 
			{ 
				m_backupCreateSubDirectories = value;
				Save();
			}
		}

		public string BackupBaseDirectory
		{
			get { return m_backupBaseDirectory; }
			set
			{
				m_backupBaseDirectory = value;
				if (m_backupBaseDirectory == "")
					m_backupBaseDirectory = MyFileSystem.UserDataPath + "\\Backup";

				Save();
			}
		}

		public bool BackupCleanup
		{
			get { return m_backupCleanup; }
			set
			{
				m_backupCleanup = value; 
				Save();
			}
		}

		public int BackupCleanupTime
		{
			get { return m_backupCleanupTime; }
			set
			{
				m_backupCleanupTime = value; 
				Save();
			}		
		}
		public bool NewUserTransportEnabled
		{
			get { return m_newUserTransportEnabled; }
			set
			{
				m_newUserTransportEnabled = value;
				Save();
			}
		}

		public int NewUserTransportDistance
		{
			get { return m_newUserTransportDistance; }
			set
			{
				m_newUserTransportDistance = Math.Max(500, value);
				Save(); 
			}
		}

		public bool BackupAsteroids
		{
			get { return m_backupAsteroids; }
			set 
			{ 
				m_backupAsteroids = value;
				Save();
			}
		}

		#endregion

		#region Constructor
		public PluginSettings()
		{
			// Default is 12 hours
			m_restartTime = 720;
			m_start = DateTime.Now;
			m_newUserTransportDistance = 500;
			m_backupAsteroids = true;

			m_informationItems = new MTObservableCollection<InformationItem>();
			m_restartItems = new MTObservableCollection<RestartItem>();
			m_backupItems = new MTObservableCollection<BackupItem>();
			m_informationItems.CollectionChanged += ItemsCollectionChanged;
			m_restartItems.CollectionChanged += ItemsCollectionChanged;
			m_backupItems.CollectionChanged += ItemsCollectionChanged;

			m_greetingMessage = "";
		}

		#endregion

		#region Loading and Saving

		/// <summary>
		/// Loads our settings
		/// </summary>
		public void Load()
		{
			m_loading = true;

			try
			{
				lock (this)
				{
					String fileName = Essentials.PluginPath + "Essential-Settings.xml";
					if (File.Exists(fileName))
					{
						using (StreamReader reader = new StreamReader(fileName))
						{
							XmlSerializer x = new XmlSerializer(typeof(PluginSettings));
							PluginSettings settings = (PluginSettings)x.Deserialize(reader);
							reader.Close();

							m_instance = settings;
						}
					}					
				}
			}
			catch(Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("Load(): {0}", ex.ToString()));
			}
			finally
			{
				m_loading = false;
			}
		}

		/// <summary>
		/// Saves our settings
		/// </summary>
		public void Save()
		{
			if (m_loading)
				return;

			try
			{
				lock (this)
				{
					String fileName = Essentials.PluginPath + "Essential-Settings.xml";
					using (StreamWriter writer = new StreamWriter(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(PluginSettings));
						x.Serialize(writer, m_instance);
						writer.Close();
					}					
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("Save(): {0}", ex.ToString()));
			}
		}

		#endregion

		#region Events

		/// <summary>
		/// Triggered when items changes.  
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Save();
		}

		#endregion
	}

	[Serializable]
	public class BackupItem
	{
		private int hour;
		public int Hour
		{
			get { return hour; }
			set 
			{
				hour = Math.Min(Math.Max(-1, value), 23);
			}
		}

		private int minute;
		public int Minute
		{
			get { return minute; }
			set 
			{
				minute = Math.Min(Math.Max(0, value), 59);
			}
		}

		private bool enabled;
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}
	}

	[Serializable]
	public class InformationItem
	{
		internal DateTime lastUpdate = DateTime.Now;

		private String subCommand;
		public String SubCommand { get { return subCommand; } set { subCommand = value; } }

		private String subText;
		public String SubText { get { return subText; } set { subText = value; } }

		private Int32 intervalSeconds;
		public Int32 IntervalSeconds { get { return intervalSeconds; } set { intervalSeconds = value; } }

		private Boolean enabled;
		public Boolean Enabled { get { return enabled; } set { enabled = value; } }
	}

	[Serializable]
	public class RestartItem
	{
		internal bool completed;

		private string message;
		public string Message
		{
			get { return message; }
			set { message = value; }
		}

		private int minutesBeforeRestart;
		public int MinutesBeforeRestart
		{
			get { return minutesBeforeRestart; }
			set { minutesBeforeRestart = value; }
		}

		private bool save;
		public bool Save
		{
			get { return save; }
			set { save = value; }
		}

		private bool stopAllShips;
		public bool StopAllShips
		{
			get { return stopAllShips; }
			set { stopAllShips = value; }
		}

		public RestartItem()
		{
			completed = false;
		}
	}
}
