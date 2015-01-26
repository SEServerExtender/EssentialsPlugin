using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using VRage.Common.Utils;
using System.ComponentModel;
using System.Reflection;

using EssentialsPlugin.Utility;
using EssentialsPlugin.UtilityClasses;
using EssentialsPlugin.Settings;

namespace EssentialsPlugin
{
	[Serializable]
	public class PluginSettings
	{
		#region Private Fields
		private static PluginSettings m_instance;
		private static bool m_loading = false;
		private static DateTime m_start;

		private string m_serverName;
		private bool m_serverUtilityGridsShowCoords;

		private bool m_informationEnabled;
		private MTObservableCollection<InformationItem> m_informationItems;

		private bool m_greetingEnabled;
		private string m_greetingMessage;
		private bool m_greetingPublic;
		private SettingsGreetingDialogItem m_greetingItem;
		private string m_greetingNewUserMessage;
		private SettingsGreetingDialogItem m_greetingNewUserItem;

		private bool m_restartEnabled;
		private MTObservableCollection<RestartNotificationItem> m_restartNotificationItems;
		private MTObservableCollection<RestartTimeItem> m_restartTimeItems;
		private string m_restartAddedProcesses;
		private bool m_restartWhenUnresponsive;

		private bool m_backupEnabled;
		private MTObservableCollection<BackupItem> m_backupItems;
		private bool m_backupCreateSubDirectories;
		private string m_backupBaseDirectory;
		private bool m_backupCleanup;
		private int m_backupCleanupTime;
		private bool m_backupAsteroids;
		private bool m_backupEssentials;

		private bool m_newUserTransportEnabled;
		private int m_newUserTransportDistance;
		private bool m_newUserTransportMoveAllSpawnShips;
		private int m_newUserTransportAsteroidDistance;
		private NewUserTransportSpawnPoint m_newUserTransportSpawnType;
		private string[] m_NewUserTransportSpawnShipNames = new string[] { };
		private bool m_newUserTransportStopRunawaySpawnShips;

		private bool m_loginEnabled;
		private string[] m_loginEntityWhitelist = new string[] { };
		private string[] m_loginPlayerIdWhitelist = new string[] { };

		private bool m_protectedEnabled;
		private MTObservableCollection<ProtectedItem> m_protectedItems;

		private bool m_dockingEnabled;
		private int m_dockingShipsPerZone;

		private bool m_dynamicConcealEnabled;
		private float m_dynamicConcealDistance;
		private bool m_dynamicConcealIncludeLargeGrids;
		private string[] m_dynamicConcealIgnoreSubTypeList = new string[] { };
		private bool m_dynamicConcealIncludeMedBays;
		private bool m_dynamicShowMessages;
		private bool m_dynamicTurretManagementEnabled;
		private int m_dynamicTurretTargetDistance;
		private bool m_dynamicTurretAllowExemption;
		private DynamicTurretManagementTypes m_dynamicTurretManagementType;

		private bool m_waypointsEnabled;
		private int m_waypointsMaxPerPlayer;
		private MTObservableCollection<ServerWaypointItem> m_waypointServerItems;
		private MTObservableCollection<ServerWaypointItem> m_waypointDefaultItems;
		private int m_waypointsMaxPerFaction;

		private bool m_cleanupEnabled;
		private MTObservableCollection<SettingsCleanupTriggerItem> m_cleanupTriggerItems;
		private MTObservableCollection<SettingsCleanupTimedItem> m_cleanupTimedItems;
		private MTObservableCollection<SettingsCleanupNotificationItem> m_cleanupNotificationItems;

		private bool m_blockEnforcementEnabled;
		private MTObservableCollection<SettingsBlockEnforcementItem> m_blockEnforcementItems;

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
			get { return m_instance ?? ( m_instance = new PluginSettings( ) ); }
		}
		#endregion

		#region Properties

		// General
		public string ServerName
		{
			get { return m_serverName; }
			set
			{
				m_serverName = value;
				Save();
			}
		}

		public bool ServerUtilityGridsShowCoords
		{
			get { return m_serverUtilityGridsShowCoords; }
			set 
			{ 
				m_serverUtilityGridsShowCoords = value;
				Save();
			}
		}

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

				if (m_restartEnabled && !m_loading)
					Communication.SendPublicInformation("[NOTICE]: Automatic restarts have been enabled by the Administrator.  You will be notified in advance before a restart occurs.");
				else if(!m_loading)
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

		public bool RestartWhenUnresponsive
		{
			get { return m_restartWhenUnresponsive; }
			set 
			{ 
				m_restartWhenUnresponsive = value;
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

		public bool GreetingPublic
		{
			get { return m_greetingPublic; }
			set 
			{ 
				m_greetingPublic = value;
				Save();
			}
		}

		public SettingsGreetingDialogItem GreetingItem
		{
			get { return m_greetingItem; }
			set
			{
				m_greetingItem = value;
				m_greetingItem.PropertyChanged += OnPropertyChanged;
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

		public SettingsGreetingDialogItem GreetingNewUserItem
		{
			get { return m_greetingNewUserItem; }
			set
			{
				m_greetingNewUserItem = value;
				m_greetingNewUserItem.PropertyChanged += OnPropertyChanged; 
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

		public bool BackupEssentials
		{
			get { return m_backupEssentials; }
			set 
			{ 
				m_backupEssentials = value;
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

		public int NewUserTransportAsteroidDistance
		{
			get { return m_newUserTransportAsteroidDistance; }
			set 
			{
				m_newUserTransportAsteroidDistance = value;
				Save();
			}
		}

		public bool NewUserTransportMoveAllSpawnShips
		{
			get { return m_newUserTransportMoveAllSpawnShips; }
			set 
			{
				m_newUserTransportMoveAllSpawnShips = value;
				Save();
			}
		}

		public NewUserTransportSpawnPoint NewUserTransportSpawnType
		{
			get { return m_newUserTransportSpawnType; }
			set
			{
				m_newUserTransportSpawnType = value;
				Save();
			}
		}

		public string[] NewUserTransportSpawnShipNames
		{
			get { return m_NewUserTransportSpawnShipNames; }
			set 
			{ 
				m_NewUserTransportSpawnShipNames = value;
				Save();
			}
		}

		public bool NewUserTransportStopRunawaySpawnShips
		{
			get { return m_newUserTransportStopRunawaySpawnShips; }
			set
			{ 
				m_newUserTransportStopRunawaySpawnShips = value;
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
						Players.ProcessServerLogsForLogins(true);
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

		public bool ProtectedEnabled
		{
			get { return m_protectedEnabled; }
			set
			{
				m_protectedEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<ProtectedItem> ProtectedItems
		{
			get { return m_protectedItems; }
			set { m_protectedItems = value; }
		}


		public bool DockingEnabled
		{
			get { return m_dockingEnabled; }
			set 
			{ 
				m_dockingEnabled = value;
				Save();
			}
		}

		public int DockingShipsPerZone
		{
			get { return m_dockingShipsPerZone; }
			set 
			{ 
				m_dockingShipsPerZone = value;
				Save();
			}
		}

		public bool DynamicConcealEnabled
		{
			get { return m_dynamicConcealEnabled; }
			set
			{
				m_dynamicConcealEnabled = value;
				Save();
			}
		}

		public float DynamicConcealDistance
		{
			get { return m_dynamicConcealDistance; }
			set
			{
				m_dynamicConcealDistance = value;
				Save();
			}
		}

		public bool ConcealIncludeLargeGrids
		{
			get { return m_dynamicConcealIncludeLargeGrids; }
			set 
			{ 
				m_dynamicConcealIncludeLargeGrids = value;
				Save();
			}
		}

		public string[] DynamicConcealIgnoreSubTypeList
		{
			get { return m_dynamicConcealIgnoreSubTypeList; }
			set 
			{ 
				m_dynamicConcealIgnoreSubTypeList = value;
				Save();
			}
		}

		public bool DynamicConcealIncludeMedBays
		{
			get { return m_dynamicConcealIncludeMedBays; }
			set 
			{ 
				m_dynamicConcealIncludeMedBays = value;
				Save();
			}
		}

		public bool DynamicShowMessages
		{
			get { return m_dynamicShowMessages; }
			set 
			{ 
				m_dynamicShowMessages = value;
				Save();
			}
		}

		public int DynamicTurretTargetDistance
		{
			get { return m_dynamicTurretTargetDistance; }
			set 
			{ 
				m_dynamicTurretTargetDistance = value;
				Save();
			}
		}

		public bool DynamicTurretManagmentEnabled
		{
			get { return m_dynamicTurretManagementEnabled; }
			set 
			{
				m_dynamicTurretManagementEnabled = value;
				Save();
			}
		}

		public bool DynamicTurretAllowExemption
		{
			get { return m_dynamicTurretAllowExemption; }
			set 
			{ 
				m_dynamicTurretAllowExemption = value;
				Save();
			}
		}

		public DynamicTurretManagementTypes DynamicTurretManagementType
		{
			get { return m_dynamicTurretManagementType; }
			set 
			{ 
				m_dynamicTurretManagementType = value;
				//m_dynamicTurretManagementType = DynamicTurretManagementTypes.All;
				Save();
			}
		}

		public bool WaypointsEnabled
		{
			get { return m_waypointsEnabled; }
			set
			{
				m_waypointsEnabled = value;
				Save();
			}
		}


		public int WaypointsMaxPerPlayer
		{
			get { return m_waypointsMaxPerPlayer; }
			set
			{
				m_waypointsMaxPerPlayer = value;
				Save();
			}
		}

		public MTObservableCollection<ServerWaypointItem> WaypointServerItems
		{
			get { return m_waypointServerItems; }
			set { m_waypointServerItems = value; }
		}

		public MTObservableCollection<ServerWaypointItem> WaypointDefaultItems
		{
			get { return m_waypointDefaultItems; }
			set { m_waypointDefaultItems = value; }
		}

		public int WaypointsMaxPerFaction
		{
			get { return m_waypointsMaxPerFaction; }
			set 
			{ 
				m_waypointsMaxPerFaction = value;
				Save();
			}
		}

		public bool CleanupEnabled
		{
			get { return m_cleanupEnabled; }
			set 
			{ 
				m_cleanupEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<SettingsCleanupTriggerItem> CleanupTriggerItems
		{
			get { return m_cleanupTriggerItems; }
			set 
			{
				m_cleanupTriggerItems = value;
				Save();
			}
		}

		public MTObservableCollection<SettingsCleanupTimedItem> CleanupTimedItems
		{
			get { return m_cleanupTimedItems; }
			set 
			{ 
				m_cleanupTimedItems = value;
				Save();
			}
		}

		public MTObservableCollection<SettingsCleanupNotificationItem> CleanupNotificationItems
		{
			get { return m_cleanupNotificationItems; }
			set 
			{ 
				m_cleanupNotificationItems = value;
				Save();
			}
		}

		public bool BlockEnforcementEnabled
		{
			get { return m_blockEnforcementEnabled; }
			set 
			{ 
				m_blockEnforcementEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<SettingsBlockEnforcementItem> BlockEnforcementItems
		{
			get { return m_blockEnforcementItems; }
		}


		#endregion

		#region Constructor
		public PluginSettings()
		{
			// Default is 12 hours
			m_start = DateTime.Now;
			m_newUserTransportDistance = 500;
			m_backupAsteroids = true;

			m_greetingItem = new SettingsGreetingDialogItem();
			m_greetingItem.PropertyChanged += OnPropertyChanged;
			m_greetingNewUserItem = new SettingsGreetingDialogItem();
			m_greetingNewUserItem.PropertyChanged += OnPropertyChanged;

			m_informationItems = new MTObservableCollection<InformationItem>();
			m_restartNotificationItems = new MTObservableCollection<RestartNotificationItem>();
			m_restartTimeItems = new MTObservableCollection<RestartTimeItem>();
			m_backupItems = new MTObservableCollection<BackupItem>();
			m_protectedItems = new MTObservableCollection<ProtectedItem>();
			m_informationItems.CollectionChanged += ItemsCollectionChanged;
			m_restartNotificationItems.CollectionChanged += ItemsCollectionChanged;
			m_restartTimeItems.CollectionChanged += ItemsCollectionChanged;
			m_backupItems.CollectionChanged += ItemsCollectionChanged;
			m_protectedItems.CollectionChanged += ItemsCollectionChanged;

			m_greetingMessage = "";

			m_dynamicConcealDistance = 8000;
			m_dynamicShowMessages = false;
			m_dynamicTurretTargetDistance = 2000;
			m_dynamicTurretManagementEnabled = false;

			m_dockingShipsPerZone = 1;

			m_newUserTransportAsteroidDistance = 0;

			m_waypointServerItems = new MTObservableCollection<ServerWaypointItem>();
			m_waypointServerItems.CollectionChanged += ItemsCollectionChanged;
			m_waypointDefaultItems = new MTObservableCollection<ServerWaypointItem>();
			m_waypointDefaultItems.CollectionChanged += ItemsCollectionChanged;

			m_cleanupTriggerItems = new MTObservableCollection<SettingsCleanupTriggerItem>();
			m_cleanupTriggerItems.CollectionChanged += ItemsCollectionChanged;
			m_cleanupTimedItems = new MTObservableCollection<SettingsCleanupTimedItem>();
			m_cleanupTimedItems.CollectionChanged += ItemsCollectionChanged;
			m_cleanupNotificationItems = new MTObservableCollection<SettingsCleanupNotificationItem>();
			m_cleanupNotificationItems.CollectionChanged += ItemsCollectionChanged;

			m_blockEnforcementItems = new MTObservableCollection<SettingsBlockEnforcementItem>();
			m_blockEnforcementItems.CollectionChanged += ItemsCollectionChanged;
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

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Console.WriteLine("PropertyChanged()");
			Save();
		}

		#endregion

		#region External Settings Modification

		public enum SettingsOperators
		{
			List,
			Set,
			Add,
			Remove
		}

		public string GetOrSetSettings(string line)
		{
			string result = "";

			try
			{
				string[] words = line.Split(new char[] { ' ' });
				SettingsOperators so = SettingsOperators.List;
				string newValue = "";
				string name = "";

				if (words.Length > 0)
					name = words[0];

				if (words.Length == 2)
				{
					if (words[1].ToLower() == "add")
						so = SettingsOperators.Add;
					else
						so = SettingsOperators.Remove;
				}
				else if (words.Length > 2)
				{
					if (words[1].ToLower() == "set")
					{
						so = SettingsOperators.Set;
						newValue = string.Join(" ", words.Skip(2));
					}
					else if(words[1].ToLower() == "remove")
					{
						so = SettingsOperators.Remove;
						newValue = string.Join(" ", words.Skip(2));
					}
				}
				else
				{
					result += string.Format("Getting Value(s) For Setting: {0}\r\n", name == "" ? "root" : name);
				}

				result += ReflectObject(m_instance, name, null, so, newValue);
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("GetSettings() Error: {0}", ex.ToString()));
			}
		
			return result;
		}

		private string ReflectObject(object obj, string name = "", PropertyInfo propertyInfo=null, SettingsOperators so = SettingsOperators.List, string newValue = "")
		{
			string result = "";
			bool found = false;

			try
			{
				Type checkType = obj.GetType();
				if (propertyInfo != null)
					checkType = propertyInfo.PropertyType;
				
				string[] names = name.Split(new char[] { '.' }, 2);
				if (checkType.IsArray)
				{
					ReflectArray(obj, propertyInfo, so, newValue, ref result, names);
				}
				else if(checkType.IsGenericType && checkType.InheritsOrImplements(typeof(IList<>)))
				{
					ReflectGenericList(obj, propertyInfo, so, newValue, ref result, names);
				}
				else if(checkType.IsValueType || checkType == typeof(string))
				{
					if(so == SettingsOperators.Set && propertyInfo != null && newValue != "")
					{
						Logging.WriteLineAndConsole(string.Format("Setting Value of {0} to '{1}'", checkType.Name, newValue));
						propertyInfo.SetValue(obj, Convert.ChangeType(newValue, propertyInfo.PropertyType));
						Save();
					}

					result += propertyInfo.GetValue(obj);
				}
				else
				{
					PropertyInfo[] properties = checkType.GetProperties();
					foreach (PropertyInfo info in properties)
					{
						bool display = false;
						bool recurse = false;
						string recurseName = "";

						if (info.Name.ToLower() == names[0].ToLower())
						{
							display = true;
							recurse = true;
							if(names.Length > 1)
								recurseName = names[1];	
						}

						if (name == "")
							display = true;

						if (display)
						{
							found = true;
							string value = "";
							if (info.PropertyType.IsValueType || info.PropertyType == typeof(string))
							{
								value += string.Format("{0}: {1}", info.Name, ReflectObject(obj, recurseName, info, so, newValue));
							}
							else if (info.PropertyType.IsArray)
							{								
								if(!recurse)
									value += string.Format("{0}: (array)", info.Name);
								else
									value += ReflectObject(obj, recurseName, info, so, newValue);
							}
							else if(info.GetValue(obj) != null && info.GetValue(obj).GetType().InheritsOrImplements(typeof(IList<>)))
							{							
								if (!recurse)
									value += string.Format("{0}: (list)", info.Name);
								else
									value += ReflectObject(obj, recurseName, info, so, newValue);

								found = true;
							}

							if (result != "" && value != "")
									result += "\r\n";

							if (value != "")
								result += value;
						}
					}

					if(!found)
					{
						result += string.Format("Unabled to find setting: {0}", name);
					}
				}
			}
			catch(Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("ReflectObject(): {0}", ex.ToString()));
			}

			return result;
		}

		private void ReflectArray(object obj, PropertyInfo info, SettingsOperators so, string newValue, ref string result, string[] names)
		{
			int item = 0;
			bool list = true;
			string recurseName = "";
			if (names[0] != "")
			{
				item = int.Parse(names[0]);
				list = false;

				if (names.Length > 1)
					recurseName = names[1];
			}

			Array arrayToReflect = (Array)info.GetValue(obj);
			if(so == SettingsOperators.Add)
			{
				Type elementType = arrayToReflect.GetType().GetElementType();
				Array newArray = Array.CreateInstance(elementType, arrayToReflect.Length + 1);
				Array.Copy(arrayToReflect, newArray, Math.Min(arrayToReflect.Length, newArray.Length));
				info.SetValue(obj, newArray);
				result += string.Format("{{ Added New Array Element - New size: {0} }}", arrayToReflect.Length + 1);
				Save();
				return;
			}
			else if (so == SettingsOperators.Remove)
			{
				item = int.Parse(newValue);
				Type elementType = arrayToReflect.GetType().GetElementType();
				Array newArray = Array.CreateInstance(elementType, arrayToReflect.Length - 1);

				if (item > 0)
					Array.Copy(arrayToReflect, 0, newArray, 0, item);

				if (item < arrayToReflect.Length - 1)
					Array.Copy(arrayToReflect, item + 1, newArray, item, arrayToReflect.Length - item - 1);

				info.SetValue(obj, newArray);
				result += string.Format("{{ Removed Array Element At: {0} }}", item);
				Save();
				return;
			}

			bool added = false;
			for (int r = 0; r < arrayToReflect.Length; r++)
			{
				if (list || (!list && r == item))
				{
					if (added)
						result += "\r\n";

					if (so == SettingsOperators.Set && newValue != "")
					{
						result += (string.Format("Setting Value of {0} item #{1} to '{2}'", info.Name, r, newValue));
						arrayToReflect.SetValue(Convert.ChangeType(newValue, arrayToReflect.GetType().GetElementType()), r);
						Save();
					}
					else
					{
						object itemToReflect = arrayToReflect.GetValue(r);
						result += "{" + string.Format("{0}: {1}", r, itemToReflect) + "}";
					}

					added = true;
				}
			}

			if(arrayToReflect.Length == 0)
			{
				result += "{ No Items In Array }";
			}
			else if(!added)
			{
				result += "{ Unable to find element requrested }";
			}
		}

		private void ReflectGenericList(object obj, PropertyInfo info, SettingsOperators so, string newValue, ref string result, string[] names)
		{
			int item = 0;
			bool list = true;
			string recurseName = "";
			if (names[0] != "")
			{
				item = int.Parse(names[0]);
				list = false;

				if (names.Length > 1)
					recurseName = names[1];
			}

			IList listToReflect = (IList)info.GetValue(obj);
			if (so == SettingsOperators.Add)
			{
				Type elementType = listToReflect.GetType().GetGenericArguments()[0];
				listToReflect.Add(Activator.CreateInstance(elementType));
				result += string.Format("{{ Added New List Element - New size: {0} }}", listToReflect.Count);
				Save();
				return;
			}
			else if (so == SettingsOperators.Remove)
			{
				item = int.Parse(newValue);
				listToReflect.RemoveAt(item);
				result += string.Format("{{ Removed List Element At: {0} - New Size: {1} }}", item, listToReflect.Count);
				Save();
				return;
			}

			int count = (int)listToReflect.GetType().GetProperty("Count").GetValue(listToReflect);
			bool added = false;
			for(int r = 0; r < count; r++)
			{
				if (list || (!list && r == item))
				{
					if (added)
						result += "\r\n";

					object itemToReflect = listToReflect.GetType().GetProperty("Item").GetValue(listToReflect, new object[] { r });
					String value = string.Format("{0} : {1}", r, ReflectObject(itemToReflect, recurseName, listToReflect.GetType().GetProperty("Item"), so, newValue));
					value = "{" + value.Replace("\r\n", ", ") + "}";
					result += value;					
					added = true;
				}
			}

			if(count == 0)
			{
				result += "{ No Items In List }";
			}
			else if (!added)
			{
				result += "{ Unable to find element requested }";
			}
		}

		#endregion
	}

	public enum NewUserTransportSpawnPoint
	{
		Asteroids,
		Origin
	}

	public enum DynamicTurretManagementTypes
	{
		All,
		AllButOwner,
		NeutralAndEnemy,
		Enemy
	}
}
