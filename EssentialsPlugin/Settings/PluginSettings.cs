namespace EssentialsPlugin
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Xml.Serialization;
	using EssentialsPlugin.GameModes;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using EssentialsPlugin.UtilityClasses;
	using Sandbox.Game;
	using VRage.FileSystem;

    [Serializable]
    public class PluginSettings
    {
        #region Private Fields
        private static PluginSettings _instance;
        private static bool _loading = false;
        public static DateTime Start;

        private string _serverName;
        private bool _serverUtilityGridsShowCoords;
        private bool _serverRespawnMenuOverride;
        private bool _stopShipsOnStart;
        private bool _promotedAdminCommands;

        private string _serverChatName;
        private bool _factionChatPrefix;
        private bool _whisperChatPrefix;

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
        private string _backupDateFormat;
        private string _backupDateFormatSubDirectory;
        private bool _backupCleanup;
        private int _backupCleanupTime;
        private bool _backupAsteroids;
        private bool _backupEssentials;
        
        private bool _loginEnabled;
        private string[] _loginEntityWhitelist = { };
        private string[] _loginPlayerIdWhitelist = { };

        private bool _protectedEnabled;
        private List<ProtectedItem> _protectedItems;

        private bool _dockingEnabled;
        private int _dockingShipsPerZone;

        private bool _dynamicConcealEnabled;
        private float _dynamicConcealDistance;
        private bool _dynamicConcealIncludeLargeGrids;
        private bool _dynamicConcealIncludeStations;
        private string[] _dynamicConcealIgnoreSubTypeList = { };
        private bool _dynamicConcealIncludeMedBays;
        private bool _dynamicShowMessages;
        private bool _dynamicConcealPirates;
        private bool _dynamicConcealPhysics;
        private bool _dynamicConcealProduction;
        private bool _dynamicTurretManagementEnabled;
        private int _dynamicTurretTargetDistance;
        private bool _dynamicTurretAllowExemption;
        private bool _dynamicBlockManagementEnabled;
        private DynamicTurretManagementMode _mDynamicTurretManagementMode;
        
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

        private bool _playerBlockEnforcementEnabled;
        private MTObservableCollection<SettingsBlockEnforcementItem> _playerBlockEnforcementItems;
        private bool _changeBlockOwnerNearest;

        private bool _gameModeConquestEnabled;

        private bool _reservedSlotsEnabled;
        private int _reservedSlotsCount;
        private ulong _reservedSlotsGroup;
        private string[] _reservedSlotsPlayers = {};
        private bool _reservedSlotsAdmins;

	    private bool _timedCommandsEnabled;
	    private MTObservableCollection<TimedCommandItem> _timedCommandsItem;

	    private bool _atmosphericCargoShipsEnabled;
	    private float _atmosphericCargoShipSpawnTime;

        private List<TicketPlayerItem> _ticketPlayers;

        private MTObservableCollection<BlacklistItem> _blacklistItems;

        private bool _disableRagdoll;
        private int _drillSpeed;

        #endregion

		#region Static Properties

		public static PluginSettings Instance => _instance ?? ( _instance = new PluginSettings( ) );

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

        public bool StopShipsOnStart
        {
            get { return _stopShipsOnStart; }
            set
            {
                _stopShipsOnStart = value;
                Save();
            }
        }

        public bool PromotedAdminCommands
        {
            get { return _promotedAdminCommands; }
            set
            {
                _promotedAdminCommands = value;
                Save( );
            }
        }

        // Chat
        public string ServerChatName
        {
            get
            {
                return _serverChatName;
            }
            set
            {
                _serverChatName = value;
                Save( );
            }
        }

        public bool FactionChatPrefix
        {
            get
            {
                return _factionChatPrefix;
            }
            set
            {
                _factionChatPrefix = value;
                Save( );
            }
        }

        public bool WhisperChatPrefix
        {
            get
            {
                return _whisperChatPrefix;
            }
            set
            {
                _whisperChatPrefix = value;
                Save( );
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

		public MTObservableCollection<InformationItem> InformationItems => _informationItems;

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

		public string BackupDateFormat
		{
			get { return _backupDateFormat; }
			set
			{
				_backupDateFormat = value;
				Save();
			}
		}

		public string BackupDateFormatSubDirectory
		{
			get { return _backupDateFormatSubDirectory; }
			set
			{
				_backupDateFormatSubDirectory = value;
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

		public List<ProtectedItem> ProtectedItems
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
            get
            {
                return _dynamicConcealEnabled;
            }
            set
            {
                _dynamicConcealEnabled = value;
                Save( );
            }
        }
        
        public bool DynamicConcealPirates
        {
            get
            {
                return _dynamicConcealPirates;
            }
            set
            {
                _dynamicConcealPirates = value;
                Save( );
            }
        }

        public bool DynamicConcealPhysics
        {
            get {return _dynamicConcealPhysics;}
            set
            {
                _dynamicConcealPhysics = value;
                Save();
            }
        }

        public bool DynamicConcealProduction 
        {
            get { return _dynamicConcealProduction; }
            set
            {
                _dynamicConcealProduction = value;
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

        public bool ConcealIncludeStations
        {
            get { return _dynamicConcealIncludeStations; }
            set
            {
                _dynamicConcealIncludeStations = value;
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
            set
            {
                _blockEnforcementItems = value;
                Save();
            }
        }

        public bool PlayerBlockEnforcementEnabled
        {
            get { return _playerBlockEnforcementEnabled; }
            set
            {
                _playerBlockEnforcementEnabled = value;
                if(value)
                    PlayerBlockEnforcement.Init();
                Save();
            }
        }

        public bool PlayerBlockEnforcementChangeOwner
        {
            get { return _changeBlockOwnerNearest; }
            set
            {
                _changeBlockOwnerNearest = value;
                Save();
            }
        }

        public MTObservableCollection<SettingsBlockEnforcementItem> PlayerBlockEnforcementItems
        {
            get { return _playerBlockEnforcementItems; }
            set
            {
                _playerBlockEnforcementItems = value;
                Save();
            }
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

        public bool ReservedSlotsEnabled
        {
            get
            {
                return _reservedSlotsEnabled;
            }
            set
            {
                _reservedSlotsEnabled = value;
                Save( );
            }
        }

        //public int ReservedSlotsCount
        //{
        //    get
        //    {
        //        return _reservedSlotsCount;
        //    }
        //    set
        //    {
        //        _reservedSlotsCount = value;
        //        Save( );
        //    }
        //}

        public ulong ReservedSlotsGroup
        {
            get
            {
                return _reservedSlotsGroup;
            }
            set
            {
                _reservedSlotsGroup = value;
                Save( );
            }
        }

        public string[] ReservedSlotsPlayers
        {
            get
            {
                return _reservedSlotsPlayers;
            }
            set
            {
                _reservedSlotsPlayers = value;
                Save( );
            }
        }

        public bool ReservedSlotsAdmins
        {
            get
            {
                return _reservedSlotsAdmins;
            }
            set
            {
                _reservedSlotsAdmins = value;
                Save( );
            }
	    } 

	    public bool TimedCommandsEnabled
	    {
	        get {return _timedCommandsEnabled;}
	        set
	        {
	            _timedCommandsEnabled = value; 
	            Save(  );
	        }
	    }

	    public MTObservableCollection<TimedCommandItem> TimedCommandsItems
	    {
	        get { return _timedCommandsItem; }
	        set
	        {
	            _timedCommandsItem = value;
	            Save(  );
	        }
	    }

	    public bool AtmosphericCargoShipsEnabled
	    {
	        get { return _atmosphericCargoShipsEnabled; }
	        set
	        {
	            _atmosphericCargoShipsEnabled = value;
	            Save( );
	        }
	    }

	    public float AtmosphericCargoShipSpawnTime
	    {
	        get { return _atmosphericCargoShipSpawnTime; }
	        set
	        {
	            _atmosphericCargoShipSpawnTime = value;
	            Save( );
	        }
	    }

        public List<TicketPlayerItem> TicketPlayers
        {
            get
                {
                    return _ticketPlayers;
                }
            set
            {
                _ticketPlayers = value;
                Save(  );
            }
        }

        public MTObservableCollection<BlacklistItem> BlacklistItems
        {
            get { return _blacklistItems; }
            set
            {
                _blacklistItems = value;
                BlacklistManager.Instance.UpdateBlacklist();
                Save();
            }
        }

        public bool DisableRagdoll
        {
            get { return _disableRagdoll; }
            set
            {
                _disableRagdoll = value;
                Save( );
            }
        }

        public int DrillSpeed
        {
            get { return _drillSpeed; }
            set
            {
                _drillSpeed = value;
                Save( );
            }
        }
        #endregion

        #region Constructor
        public PluginSettings()
		{
			// Default is 12 hours
			Start = DateTime.Now;
			_backupAsteroids = true;

			_greetingItem = new SettingsGreetingDialogItem();
			_greetingItem.PropertyChanged += OnPropertyChanged;
			_greetingNewUserItem = new SettingsGreetingDialogItem();
			_greetingNewUserItem.PropertyChanged += OnPropertyChanged;

			_informationItems = new MTObservableCollection<InformationItem>();
			_restartNotificationItems = new MTObservableCollection<RestartNotificationItem>();
			_restartTimeItems = new MTObservableCollection<RestartTimeItem>();
			_backupItems = new MTObservableCollection<BackupItem>();
			_protectedItems = new List<ProtectedItem>();
			_informationItems.CollectionChanged += ItemsCollectionChanged;
			_restartNotificationItems.CollectionChanged += ItemsCollectionChanged;
			_restartTimeItems.CollectionChanged += ItemsCollectionChanged;
			_backupItems.CollectionChanged += ItemsCollectionChanged;

            _reservedSlotsEnabled = false;
            _reservedSlotsCount = 5;
            _reservedSlotsGroup = 0;
            _reservedSlotsAdmins = false;

            _stopShipsOnStart = false;
            _promotedAdminCommands = false;

		    _loginEnabled = true;

            _serverChatName = "Server";
            _factionChatPrefix = true;
            _whisperChatPrefix = true;

            _backupDateFormat = "MM-dd-yyyy_HH-mm";
			_backupDateFormatSubDirectory = "MM-dd-yyyy";

			_greetingMessage = "";
            
			_dynamicConcealDistance = 8000;
            _dynamicConcealPirates = false;
            _dynamicConcealPhysics = false;
            _dynamicConcealProduction = true;
            _dynamicShowMessages = false;
			_dynamicTurretTargetDistance = 2000;
			_dynamicTurretManagementEnabled = false;

			_dockingShipsPerZone = 1;

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

            _playerBlockEnforcementItems = new MTObservableCollection<SettingsBlockEnforcementItem>();
            _playerBlockEnforcementItems.CollectionChanged += ItemsCollectionChanged;

            _timedCommandsEnabled = false;
            _timedCommandsItem = new MTObservableCollection<TimedCommandItem>(  );
            _timedCommandsItem.CollectionChanged += ItemsCollectionChanged;

            _blacklistItems = new MTObservableCollection<BlacklistItem>();
            _blacklistItems.CollectionChanged += ItemsCollectionChanged;
            _blacklistItems.CollectionChanged += BlacklistManager.Instance._blacklistItems_CollectionChanged;

            _atmosphericCargoShipsEnabled = false;
            _atmosphericCargoShipSpawnTime = 10.0f;
            
            _ticketPlayers = new List<TicketPlayerItem>();
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
					string fileName = Essentials.PluginPath + "Essential-Settings.xml";
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
					string fileName = Essentials.PluginPath + "Essential-Settings.xml";
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
				string[] words = line.Split( ' ' );
				SettingsOperators so = SettingsOperators.List;
				string newValue = "";
				string name = "";

				if (words.Length > 0)
					name = words[0];

				if (words.Length == 2)
				{
					so = words[1].ToLower() == "add" ? SettingsOperators.Add : SettingsOperators.Remove;
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
					result += $"Getting Value(s) For Setting: {( name == "" ? "root" : name )}\r\n";
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
				
				string[] names = name.Split(new[] { '.' }, 2);
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
								value += $"{info.Name}: {ReflectObject( obj, recurseName, info, so, newValue )}";
							}
							else if (info.PropertyType.IsArray)
							{								
								if(!recurse)
									value += $"{info.Name}: (array)";
								else
									value += ReflectObject(obj, recurseName, info, so, newValue);
							}
							else if(info.GetValue(obj) != null && info.GetValue(obj).GetType().InheritsOrImplements(typeof(IList<>)))
							{							
								if (!recurse)
									value += $"{info.Name}: (list)";
								else
									value += ReflectObject(obj, recurseName, info, so, newValue);
							}

							if (result != "" && value != "")
									result += "\r\n";

							if (value != "")
								result += value;
						}
					}

					if(!found)
					{
						result += $"Unabled to find setting: {name}";
					}
				}
			}
			catch(Exception ex)
			{
				Essentials.Log.Error( ex );
			}

			return result;
		}

		/// <exception cref="OverflowException">The array is multidimensional and contains more than <see cref="F:System.Int32.MaxValue" /> elements.</exception>
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
				result += $"{{ Added New Array Element - New size: {arrayToReflect.Length + 1} }}";
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
				result += $"{{ Removed Array Element At: {item} }}";
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
						result += ( $"Setting Value of {info.Name} item #{r} to '{newValue}'" );
						arrayToReflect.SetValue(Convert.ChangeType(newValue, arrayToReflect.GetType().GetElementType()), r);
						Save();
					}
					else
					{
						object itemToReflect = arrayToReflect.GetValue(r);
						result += "{" + $"{r}: {itemToReflect}" + "}";
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
				result += $"{{ Added New List Element - New size: {listToReflect.Count} }}";
				Save();
				return;
			}
			else if (so == SettingsOperators.Remove)
			{
				item = int.Parse(newValue);
				listToReflect.RemoveAt(item);
				result += $"{{ Removed List Element At: {item} - New Size: {listToReflect.Count} }}";
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
					string value = $"{r} : {ReflectObject( itemToReflect, recurseName, listToReflect.GetType( ).GetProperty( "Item" ), so, newValue )}";
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
