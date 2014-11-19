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
using System.Windows.Forms.Design;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.ComponentModel.Design;

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
		private MTObservableCollection<RestartNotificationItem> m_restartNotificationItems;
		private MTObservableCollection<RestartTimeItem> m_restartTimeItems;
		private string m_restartAddedProcesses;

		private bool m_backupEnabled;
		private MTObservableCollection<BackupItem> m_backupItems;
		private bool m_backupCreateSubDirectories;
		private string m_backupBaseDirectory;
		private bool m_backupCleanup;
		private int m_backupCleanupTime;
		private bool m_backupAsteroids;

		private bool m_newUserTransportEnabled;
		private int m_newUserTransportDistance;

		private bool m_loginEnabled;
		private string[] m_loginEntityWhitelist = new string[] { };
		private string[] m_loginPlayerIdWhitelist = new string[] { };

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

		public MTObservableCollection<RestartNotificationItem> RestartNotificationItems
		{
			get { return m_restartNotificationItems; }
			set { m_restartNotificationItems = value; }
		}

		public MTObservableCollection<RestartTimeItem> RestartTimeItems
		{
			get { return m_restartTimeItems; }
			set { m_restartTimeItems = value; }
		}

		public string RestartAddedProcesses
		{
			get { return m_restartAddedProcesses; }
			set 
			{ 
				m_restartAddedProcesses = value;
				Save();
			}
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

		public bool BackupAsteroids
		{
			get { return m_backupAsteroids; }
			set
			{
				m_backupAsteroids = value;
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

		public bool LoginEnabled
		{
			get { return m_loginEnabled; }
			set 
			{ 
				m_loginEnabled = value;
				if (value && !m_loading)
				{
					if (Players.Instance.PlayerLogins.Count() == 0)
						Players.ProcessServerLogsForLogins();
				}
				Save();
			}
		}

		public string[] LoginEntityWhitelist
		{
			get { return m_loginEntityWhitelist; }
			set
			{
				m_loginEntityWhitelist = value; 
				Save();
			}
		}

		public string[] LoginPlayerIdWhitelist
		{
			get { return m_loginPlayerIdWhitelist; }
			set
			{
				m_loginPlayerIdWhitelist = value; 
				Save();
			}
		}

		#endregion

		#region Constructor
		public PluginSettings()
		{
			// Default is 12 hours
			m_start = DateTime.Now;
			m_newUserTransportDistance = 500;
			m_backupAsteroids = true;

			m_informationItems = new MTObservableCollection<InformationItem>();
			m_restartNotificationItems = new MTObservableCollection<RestartNotificationItem>();
			m_restartTimeItems = new MTObservableCollection<RestartTimeItem>();
			m_backupItems = new MTObservableCollection<BackupItem>();
			m_informationItems.CollectionChanged += ItemsCollectionChanged;
			m_restartNotificationItems.CollectionChanged += ItemsCollectionChanged;
			m_restartTimeItems.CollectionChanged += ItemsCollectionChanged;
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
		internal int position = 0;

		private bool enabled;
		public bool Enabled { get { return enabled; } set { enabled = value; } }

		private string subCommand;
		public string SubCommand { get { return subCommand; } set { subCommand = value; } }

		private string subText;
		[Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
		public string SubText { get { return subText; } set { subText = value; } }

		private int intervalSeconds;
		public int IntervalSeconds { get { return intervalSeconds; } set { intervalSeconds = value; } }
	}

	[Serializable]
	public class RestartTimeItem
	{
		public DateTime Restart;

		private bool enabled;
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		private string restartTime;
		[Editor(typeof(TimePickerEditor), typeof(UITypeEditor))]
		public string RestartTime
		{
			get { return restartTime; }
			set 
			{ 
				restartTime = value;
				Restart = DateTime.Parse(restartTime);
			}
		}

		public RestartTimeItem()
		{
			enabled = false;
			restartTime = DateTime.Now.AddHours(1).ToString("HH:mm");
			Restart = DateTime.Now.AddHours(1);
		}
	}

	[Serializable]
	public class RestartNotificationItem
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

		public RestartNotificationItem()
		{
			completed = false;
		}
	}
}
