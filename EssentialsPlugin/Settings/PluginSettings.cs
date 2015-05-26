using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Reflection;

using EssentialsPlugin.Utility;
using EssentialsPlugin.UtilityClasses;
using EssentialsPlugin.Settings;
using EssentialsPlugin.GameModes;

namespace EssentialsPlugin
{
	using VRage.FileSystem;

	[Serializable]
	public class PluginSettings
	{
		#region Private Fields
		private static PluginSettings _instance;
		private static bool _loading = false;
		private static DateTime _start;

		private string _serverName;
		private bool _serverUtilityGridsShowCoords;
		private bool _serverRespawnMenuOverride;

		private bool _informationEnabled;
		private MTObservableCollection<InformationItem> _informationItems;

		private bool _greetingEnabled;
		private string _greetingMessage;
		private bool _greetingPublic;
		private SettingsGreetingDialogItem _greetingItem;
		private string _greetingNewUserMessage;
		private SettingsGreetingDialogItem _greetingNewUserItem;

		private bool _restartEnabled;
		private MTObservableCollection<RestartNotificationItem> _restartNotificationItems;
		private MTObservableCollection<RestartTimeItem> _restartTimeItems;
		private string _restartAddedProcesses;
		private bool _restartWhenUnresponsive;

		private bool _backupEnabled;
		private MTObservableCollection<BackupItem> _backupItems;
		private bool _backupCreateSubDirectories;
		private string _backupBaseDirectory;
		private bool _backupCleanup;
		private int _backupCleanupTime;
		private bool _backupAsteroids;
		private bool _backupEssentials;

		private bool _newUserTransportEnabled;
		private int _newUserTransportDistance;
		private bool _newUserTransportMoveAllSpawnShips;
		private int _newUserTransportAsteroidDistance;
		private NewUserTransportSpawnPoint _newUserTransportSpawnType;
		private string[] _newUserTransportSpawnShipNames = { };
		private bool _newUserTransportStopRunawaySpawnShips;

		private bool _loginEnabled;
		private string[] _loginEntityWhitelist = { };
		private string[] _loginPlayerIdWhitelist = { };

		private bool _protectedEnabled;
		private MTObservableCollection<ProtectedItem> _protectedItems;

		private bool _dockingEnabled;
		private int _dockingShipsPerZone;

		private bool _dynamicConcealEnabled;
		private float _dynamicConcealDistance;
		private bool _dynamicConcealIncludeLargeGrids;
		private string[] _dynamicConcealIgnoreSubTypeList = { };
		private bool _dynamicConcealIncludeMedBays;
		private bool _dynamicShowMessages;
		private bool _dynamicTurretManagementEnabled;
		private int _dynamicTurretTargetDistance;
		private bool _dynamicTurretAllowExemption;
		private bool _dynamicBlockManagementEnabled;
		private DynamicTurretManagementMode _mDynamicTurretManagementMode;
        private bool _dynamicVoxelManagementEnabled;
        private int _dynamicVoxelDistance;
		
		private bool _dynamicConcealServerOnly;
		private bool _dynamicClientConcealEnabled;
		private float _dynamicClientConcealDistance;

		private bool _waypointsEnabled;
		private int _waypointsMaxPerPlayer;
		private MTObservableCollection<ServerWaypointItem> _waypointServerItems;
		private MTObservableCollection<ServerWaypointItem> _waypointDefaultItems;
		private int _waypointsMaxPerFaction;

		private bool _cleanupEnabled;
		private MTObservableCollection<SettingsCleanupTriggerItem> _cleanupTriggerItems;
		private MTObservableCollection<SettingsCleanupTimedItem> _cleanupTimedItems;
		private MTObservableCollection<SettingsCleanupNotificationItem> _cleanupNotificationItems;

		private bool _blockEnforcementEnabled;
		private MTObservableCollection<SettingsBlockEnforcementItem> _blockEnforcementItems;

		private bool _gameModeConquestEnabled;
		#endregion

		#region Static Properties
		public static DateTime RestartStartTime
		{
			get
			{
				return _start;
			}
		}

		public static PluginSettings Instance
		{
			get { return _instance ?? ( _instance = new PluginSettings( ) ); }
		}
		#endregion

		#region Properties

		// General
		public string ServerName
		{
			get { return _serverName; }
			set
			{
				_serverName = value;
				Save();
			}
		}

		public bool ServerUtilityGridsShowCoords
		{
			get { return _serverUtilityGridsShowCoords; }
			set 
			{ 
				_serverUtilityGridsShowCoords = value;
				Save();
			}
		}

		public bool ServerRespawnMenuOverride
		{
			get { return _serverRespawnMenuOverride; }
			set 
			{ 
				_serverRespawnMenuOverride = value;
				Save();
			}
		}

		// Information
		public bool InformationEnabled
		{
			get { return _informationEnabled; }
			set 
			{ 
				_informationEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<InformationItem> InformationItems
		{
			get { return _informationItems; }
		}

		// Automated Restarts
		public bool RestartEnabled
		{
			get { return _restartEnabled; }
			set 
			{
				_restartEnabled = value;
				Save();

				if (_restartEnabled && !_loading)
					Communication.SendPublicInformation("[NOTICE]: Automatic restarts have been enabled by the Administrator.  You will be notified in advance before a restart occurs.");
				else if(!_loading)
					Communication.SendPublicInformation("[NOTICE]: Automatic restarts have been disabled by the Administrator.");
			}
		}

		public MTObservableCollection<RestartNotificationItem> RestartNotificationItems
		{
			get { return _restartNotificationItems; }
			set { _restartNotificationItems = value; }
		}

		public MTObservableCollection<RestartTimeItem> RestartTimeItems
		{
			get { return _restartTimeItems; }
			set { _restartTimeItems = value; }
		}

		public string RestartAddedProcesses
		{
			get { return _restartAddedProcesses; }
			set 
			{ 
				_restartAddedProcesses = value;
				Save();
			}
		}

		public bool RestartWhenUnresponsive
		{
			get { return _restartWhenUnresponsive; }
			set 
			{ 
				_restartWhenUnresponsive = value;
				Save();
			}
		}

		// Greetings
		public bool GreetingEnabled
		{
			get { return _greetingEnabled; }
			set 
			{ 
				_greetingEnabled = value;
				Save();
			}
		}

		public string GreetingMessage
		{
			get { return _greetingMessage; }
			set 
			{ 
				_greetingMessage = value;
				Save();
			}
		}

		public bool GreetingPublic
		{
			get { return _greetingPublic; }
			set 
			{ 
				_greetingPublic = value;
				Save();
			}
		}

		public SettingsGreetingDialogItem GreetingItem
		{
			get { return _greetingItem; }
			set
			{
				_greetingItem = value;
				_greetingItem.PropertyChanged += OnPropertyChanged;
				Save();
			}
		}

		public string GreetingNewUserMessage
		{
			get { return _greetingNewUserMessage; }
			set
			{
				_greetingNewUserMessage = value;
				Save();
			}
		}

		public SettingsGreetingDialogItem GreetingNewUserItem
		{
			get { return _greetingNewUserItem; }
			set
			{
				_greetingNewUserItem = value;
				_greetingNewUserItem.PropertyChanged += OnPropertyChanged; 
				Save();
			}
		}

		// Automated Backups
		public bool BackupEnabled
		{
			get { return _backupEnabled; }
			set 
			{ 
				_backupEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<BackupItem> BackupItems
		{
			get { return _backupItems; }
			set 
			{ 
				_backupItems = value;
			}
		}

		public bool BackupCreateSubDirectories
		{
			get { return _backupCreateSubDirectories; }
			set 
			{ 
				_backupCreateSubDirectories = value;
				Save();
			}
		}

		public string BackupBaseDirectory
		{
			get { return _backupBaseDirectory; }
			set
			{
				_backupBaseDirectory = value;
				if (_backupBaseDirectory == "")
					_backupBaseDirectory = MyFileSystem.UserDataPath + "\\Backup";

				Save();
			}
		}

		public bool BackupCleanup
		{
			get { return _backupCleanup; }
			set
			{
				_backupCleanup = value; 
				Save();
			}
		}

		public int BackupCleanupTime
		{
			get { return _backupCleanupTime; }
			set
			{
				_backupCleanupTime = value; 
				Save();
			}		
		}

		public bool BackupAsteroids
		{
			get { return _backupAsteroids; }
			set
			{
				_backupAsteroids = value;
				Save();
			}
		}

		public bool BackupEssentials
		{
			get { return _backupEssentials; }
			set 
			{ 
				_backupEssentials = value;
				Save();
			}
		}

		public bool NewUserTransportEnabled
		{
			get { return _newUserTransportEnabled; }
			set
			{
				_newUserTransportEnabled = value;
				Save();
			}
		}

		public int NewUserTransportDistance
		{
			get { return _newUserTransportDistance; }
			set
			{
				_newUserTransportDistance = Math.Max(500, value);
				Save(); 
			}
		}

		public int NewUserTransportAsteroidDistance
		{
			get { return _newUserTransportAsteroidDistance; }
			set 
			{
				_newUserTransportAsteroidDistance = value;
				Save();
			}
		}

		public bool NewUserTransportMoveAllSpawnShips
		{
			get { return _newUserTransportMoveAllSpawnShips; }
			set 
			{
				_newUserTransportMoveAllSpawnShips = value;
				Save();
			}
		}

		public NewUserTransportSpawnPoint NewUserTransportSpawnType
		{
			get { return _newUserTransportSpawnType; }
			set
			{
				_newUserTransportSpawnType = value;
				Save();
			}
		}

		public string[] NewUserTransportSpawnShipNames
		{
			get { return _newUserTransportSpawnShipNames; }
			set 
			{ 
				_newUserTransportSpawnShipNames = value;
				Save();
			}
		}

		public bool NewUserTransportStopRunawaySpawnShips
		{
			get { return _newUserTransportStopRunawaySpawnShips; }
			set
			{ 
				_newUserTransportStopRunawaySpawnShips = value;
				Save();
			}
		}

		public bool LoginEnabled
		{
			get { return _loginEnabled; }
			set 
			{ 
				_loginEnabled = value;
				if (value && !_loading)
				{
					if (!Players.Instance.PlayerLogins.Any())
						Players.ProcessServerLogsForLogins(true);
				}
				Save();
			}
		}

		public string[] LoginEntityWhitelist
		{
			get { return _loginEntityWhitelist; }
			set
			{
				_loginEntityWhitelist = value; 
				Save();
			}
		}

		public string[] LoginPlayerIdWhitelist
		{
			get { return _loginPlayerIdWhitelist; }
			set
			{
				_loginPlayerIdWhitelist = value; 
				Save();
			}
		}

		public bool ProtectedEnabled
		{
			get { return _protectedEnabled; }
			set
			{
				_protectedEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<ProtectedItem> ProtectedItems
		{
			get { return _protectedItems; }
			set { _protectedItems = value; }
		}


		public bool DockingEnabled
		{
			get { return _dockingEnabled; }
			set 
			{ 
				_dockingEnabled = value;
				Save();
			}
		}

		public int DockingShipsPerZone
		{
			get { return _dockingShipsPerZone; }
			set 
			{ 
				_dockingShipsPerZone = value;
				Save();
			}
		}

		public bool DynamicConcealEnabled
		{
			get { return _dynamicConcealEnabled; }
			set
			{
				_dynamicConcealEnabled = value;
				Save();
			}
		}

		public float DynamicConcealDistance
		{
			get { return _dynamicConcealDistance; }
			set
			{
				_dynamicConcealDistance = value;
				Save();
			}
		}

		public bool ConcealIncludeLargeGrids
		{
			get { return _dynamicConcealIncludeLargeGrids; }
			set 
			{ 
				_dynamicConcealIncludeLargeGrids = value;
				Save();
			}
		}

		public string[] DynamicConcealIgnoreSubTypeList
		{
			get { return _dynamicConcealIgnoreSubTypeList; }
			set 
			{ 
				_dynamicConcealIgnoreSubTypeList = value;
				Save();
			}
		}

		public bool DynamicConcealIncludeMedBays
		{
			get { return _dynamicConcealIncludeMedBays; }
			set 
			{ 
				_dynamicConcealIncludeMedBays = value;
				Save();
			}
		}

		public bool DynamicConcealServerOnly
		{
			get { return _dynamicConcealServerOnly; }
			set 
			{ 
				_dynamicConcealServerOnly = value;
				Save();
			}
		}

		public bool DynamicClientConcealEnabled
		{
			get { return _dynamicClientConcealEnabled; }
			set 
			{ 
				_dynamicClientConcealEnabled = value;
				Save();
			}
		}

		public float DynamicClientConcealDistance
		{
			get { return _dynamicClientConcealDistance; }
			set 
			{ 
				_dynamicClientConcealDistance = value;
				Save();
			}
		}
		
		public bool DynamicShowMessages
		{
			get { return _dynamicShowMessages; }
			set 
			{ 
				_dynamicShowMessages = value;
				Save();
			}
		}

		public int DynamicTurretTargetDistance
		{
			get { return _dynamicTurretTargetDistance; }
			set 
			{ 
				_dynamicTurretTargetDistance = value;
				Save();
			}
		}

		public bool DynamicTurretManagmentEnabled
		{
			get { return _dynamicTurretManagementEnabled; }
			set 
			{
				_dynamicTurretManagementEnabled = value;
				Save();
			}
		}

		public bool DynamicTurretAllowExemption
		{
			get { return _dynamicTurretAllowExemption; }
			set 
			{ 
				_dynamicTurretAllowExemption = value;
				Save();
			}
		}

		public DynamicTurretManagementMode DynamicTurretManagementMode
		{
			get { return _mDynamicTurretManagementMode; }
			set 
			{ 
				_mDynamicTurretManagementMode = value;
				//_mDynamicTurretManagementMode = DynamicTurretManagementMode.All;
				Save();
			}
		}

        public bool DynamicVoxelManagementEnabled
        {
            get { return _dynamicVoxelManagementEnabled; }
            set
            {
                _dynamicVoxelManagementEnabled = value;
                Save();
            }
        }

        public int DynamicVoxelDistance
        {
            get { return _dynamicVoxelDistance; }
            set
            {
                _dynamicVoxelDistance = value;
                Save();
            }
        }

		public bool DynamicBlockManagementEnabled
		{
			get { return _dynamicBlockManagementEnabled; }
			set 
			{ 
				_dynamicBlockManagementEnabled = value;
				Save();
			}
		}

		public bool WaypointsEnabled
		{
			get { return _waypointsEnabled; }
			set
			{
				_waypointsEnabled = value;
				Save();
			}
		}


		public int WaypointsMaxPerPlayer
		{
			get { return _waypointsMaxPerPlayer; }
			set
			{
				_waypointsMaxPerPlayer = value;
				Save();
			}
		}

		public MTObservableCollection<ServerWaypointItem> WaypointServerItems
		{
			get { return _waypointServerItems; }
			set { _waypointServerItems = value; }
		}

		public MTObservableCollection<ServerWaypointItem> WaypointDefaultItems
		{
			get { return _waypointDefaultItems; }
			set { _waypointDefaultItems = value; }
		}

		public int WaypointsMaxPerFaction
		{
			get { return _waypointsMaxPerFaction; }
			set 
			{ 
				_waypointsMaxPerFaction = value;
				Save();
			}
		}

		public bool CleanupEnabled
		{
			get { return _cleanupEnabled; }
			set 
			{ 
				_cleanupEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<SettingsCleanupTriggerItem> CleanupTriggerItems
		{
			get { return _cleanupTriggerItems; }
			set 
			{
				_cleanupTriggerItems = value;
				Save();
			}
		}

		public MTObservableCollection<SettingsCleanupTimedItem> CleanupTimedItems
		{
			get { return _cleanupTimedItems; }
			set 
			{ 
				_cleanupTimedItems = value;
				Save();
			}
		}

		public MTObservableCollection<SettingsCleanupNotificationItem> CleanupNotificationItems
		{
			get { return _cleanupNotificationItems; }
			set 
			{ 
				_cleanupNotificationItems = value;
				Save();
			}
		}

		public bool BlockEnforcementEnabled
		{
			get { return _blockEnforcementEnabled; }
			set 
			{ 
				_blockEnforcementEnabled = value;
				Save();
			}
		}

		public MTObservableCollection<SettingsBlockEnforcementItem> BlockEnforcementItems
		{
			get { return _blockEnforcementItems; }
		}

		public bool GameModeConquestEnabled
		{
			get { return _gameModeConquestEnabled; }
			set 
			{ 
				_gameModeConquestEnabled = value;
				Save();

				if (value)
				{
					//Communication.SendPublicInformation("[CONQUEST]: Conquest game mode has been enabled by the administrator.  To view conquest leaderboard, type /leaderboard conquest.");
					Conquest.Process(true);
				}
			}
		}

		#endregion

		#region Constructor
		public PluginSettings()
		{
			// Default is 12 hours
			_start = DateTime.Now;
			_newUserTransportDistance = 500;
			_backupAsteroids = true;

			_greetingItem = new SettingsGreetingDialogItem();
			_greetingItem.PropertyChanged += OnPropertyChanged;
			_greetingNewUserItem = new SettingsGreetingDialogItem();
			_greetingNewUserItem.PropertyChanged += OnPropertyChanged;

			_informationItems = new MTObservableCollection<InformationItem>();
			_restartNotificationItems = new MTObservableCollection<RestartNotificationItem>();
			_restartTimeItems = new MTObservableCollection<RestartTimeItem>();
			_backupItems = new MTObservableCollection<BackupItem>();
			_protectedItems = new MTObservableCollection<ProtectedItem>();
			_informationItems.CollectionChanged += ItemsCollectionChanged;
			_restartNotificationItems.CollectionChanged += ItemsCollectionChanged;
			_restartTimeItems.CollectionChanged += ItemsCollectionChanged;
			_backupItems.CollectionChanged += ItemsCollectionChanged;
			_protectedItems.CollectionChanged += ItemsCollectionChanged;

			_greetingMessage = "";

			_dynamicConcealDistance = 8000;
			_dynamicShowMessages = false;
			_dynamicTurretTargetDistance = 2000;
			_dynamicTurretManagementEnabled = false;
            _dynamicVoxelManagementEnabled = false;
            _dynamicVoxelDistance = 20000;

			_dockingShipsPerZone = 1;

			_newUserTransportAsteroidDistance = 0;

			_waypointServerItems = new MTObservableCollection<ServerWaypointItem>();
			_waypointServerItems.CollectionChanged += ItemsCollectionChanged;
			_waypointDefaultItems = new MTObservableCollection<ServerWaypointItem>();
			_waypointDefaultItems.CollectionChanged += ItemsCollectionChanged;

			_cleanupTriggerItems = new MTObservableCollection<SettingsCleanupTriggerItem>();
			_cleanupTriggerItems.CollectionChanged += ItemsCollectionChanged;
			_cleanupTimedItems = new MTObservableCollection<SettingsCleanupTimedItem>();
			_cleanupTimedItems.CollectionChanged += ItemsCollectionChanged;
			_cleanupNotificationItems = new MTObservableCollection<SettingsCleanupNotificationItem>();
			_cleanupNotificationItems.CollectionChanged += ItemsCollectionChanged;

			_blockEnforcementItems = new MTObservableCollection<SettingsBlockEnforcementItem>();
			_blockEnforcementItems.CollectionChanged += ItemsCollectionChanged;
		}


		#endregion

		#region Loading and Saving

		/// <summary>
		/// Loads our settings
		/// </summary>
		public void Load()
		{
			_loading = true;

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

							_instance = settings;
						}
					}					
				}
			}
			catch(Exception ex)
			{
				Essentials.Log.Error( ex );
			}
			finally
			{
				_loading = false;
			}
		}

		/// <summary>
		/// Saves our settings
		/// </summary>
		public void Save()
		{
			if (_loading)
				return;

			try
			{
				lock (this)
				{
					String fileName = Essentials.PluginPath + "Essential-Settings.xml";
					using (StreamWriter writer = new StreamWriter(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(PluginSettings));
						x.Serialize(writer, _instance);
						writer.Close();
					}					
				}
			}
			catch (Exception ex)
			{
				Essentials.Log.Error( ex );
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

				result += ReflectObject(_instance, name, null, so, newValue);
			}
			catch (Exception ex)
			{
				Essentials.Log.Error( ex );
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
						Essentials.Log.Info( "Setting Value of {0} to '{1}'", checkType.Name, newValue );
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
				Essentials.Log.Error( ex );
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

	public enum DynamicTurretManagementMode
	{
		All,
		AllButOwner,
		NeutralAndEnemy,
		Enemy
	}
}
