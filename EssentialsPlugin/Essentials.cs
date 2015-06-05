namespace EssentialsPlugin
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.Design;
	using System.Drawing.Design;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Windows.Forms.Design;
	using EssentialsPlugin.ChatHandlers;
	using EssentialsPlugin.ChatHandlers.Admin;
	using EssentialsPlugin.ChatHandlers.AdminConceal;
	using EssentialsPlugin.ChatHandlers.AdminDelete;
	using EssentialsPlugin.ChatHandlers.AdminScan;
	using EssentialsPlugin.ChatHandlers.Dock;
	using EssentialsPlugin.ChatHandlers.Waypoints;
	using EssentialsPlugin.ProcessHandlers;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using EssentialsPlugin.UtilityClasses;
	using NLog;
	using Sandbox.ModAPI;
	using SEModAPI.API.Utility;
	using SEModAPIExtensions.API;
	using SEModAPIExtensions.API.Plugin;
	using SEModAPIExtensions.API.Plugin.Events;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
	using VRage.ModAPI;

	public class Essentials : IPlugin, IChatEventHandler, IPlayerEventHandler, ICubeGridHandler, ICubeBlockEventHandler, ISectorEventHandler
	{
		public static Logger Log;
		#region Private Fields
		internal static Essentials Instance;
		private static string _pluginPath;
		//private static ControlForm controlForm;
		private Thread _processThread;
		private List<Thread> _processThreads;
		private List<ProcessHandlerBase> _processHandlers;
		private List<ChatHandlerBase> _chatHandlers;
		private bool _running = true;
		private DateTime m_lastProcessUpdate;

		#endregion

		#region Properties
		public static string PluginPath
		{
			get { return _pluginPath; }
			set { _pluginPath = value; }
		}

		internal static List<ChatHandlerBase> ChatHandlers
		{
			get
			{
				if ( Instance != null )
				{
					return Instance._chatHandlers;
				}

				return null;
			}
		}

		/*
		public static ControlForm PluginControlForm
		{
			get 
			{
				if (controlForm == null)
					controlForm = new ControlForm();

				return controlForm;  
			}
		}
		*/

		[Category( "General" )]
		[Description( "Name of the server used throughout this plugin" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public string ServerName
		{
			get { return PluginSettings.Instance.ServerName; }
			set { PluginSettings.Instance.ServerName = value; }
		}

		[Category( "General" )]
		[Description( "Should we allow users to see coordinates when they type /utility grids list if they own 100% of the ship?" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool ServerUtilityGridsShowCoords
		{
			get { return PluginSettings.Instance.ServerUtilityGridsShowCoords; }
			set { PluginSettings.Instance.ServerUtilityGridsShowCoords = value; }
		}

		[Category( "General" )]
		[Description( "Enable / Disable respawn menu override.  If you're having issues with a very slow respawn menu, this will help, though please note it may cause more problems if your server is running quickly.  (Will give the spawn menu twice)" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool ServerRespawnMenuOverride
		{
			get { return PluginSettings.Instance.ServerRespawnMenuOverride; }
			set
			{
				PluginSettings.Instance.ServerRespawnMenuOverride = value;
			}
		}

		[Category( "Information" )]
		[Description( "Enabled / Disable Information Commands" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool InformationEnabled
		{
			get
			{
				return PluginSettings.Instance.InformationEnabled;
			}

			set
			{
				PluginSettings.Instance.InformationEnabled = value;
			}
		}

		[Category( "Information" )]
		[Description( "Information command items.  These are accessed with the /info command.  Each item you define can be access by typing /info <info command> or they can be scheduled to be sent out at certain intervals." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<InformationItem> InformationItems
		{
			get
			{
				return PluginSettings.Instance.InformationItems;
			}
		}

		[Category( "Automated Restart" )]
		[Description( "Enable / Disable Automated Server Restarts" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool RestartEnabled
		{
			get
			{
				return PluginSettings.Instance.RestartEnabled;
			}

			set
			{
				PluginSettings.Instance.RestartEnabled = value;
			}
		}

		[Category( "Automated Restart" )]
		[Description( "Restart Notification Items.  Each item is a notification sent to the user at specified intervals.  Use this to warn users that a restart is going to occur before it happens." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<RestartNotificationItem> RestartNotificationItems
		{
			get
			{
				return PluginSettings.Instance.RestartNotificationItems;
			}
		}

		[Category( "Automated Restart" )]
		[Description( "Restart Time Items.  Each item is a time of day when a restart will occur." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<RestartTimeItem> RestartTimeItems
		{
			get
			{
				return PluginSettings.Instance.RestartTimeItems;
			}
		}

		[Category( "Automated Restart" )]
		[Description( "Extra processes to run before the server is restarted.  Allows adding custom scripts during restart.  Each line is a seperate process." )]
		[Browsable( true )]
		[ReadOnly( false )]
		[Editor( typeof( MultilineStringEditor ), typeof( UITypeEditor ) )]
		public string RestartAddedProcesses
		{
			get
			{
				return PluginSettings.Instance.RestartAddedProcesses;
			}
			set
			{
				PluginSettings.Instance.RestartAddedProcesses = value;
			}
		}

		[Category( "Automated Restart" )]
		[Description( "Enable / Disable A trigger which automatically restarts the server if it becomes unresponsive for 1 full minute." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool RestartWhenUnresponsive
		{
			get { return PluginSettings.Instance.RestartWhenUnresponsive; }
			set { PluginSettings.Instance.RestartWhenUnresponsive = value; }
		}

		[Category( "Join Greeting" )]
		[Description( "Enable / Disable Join Greetings" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool GreetingEnabled
		{
			get { return PluginSettings.Instance.GreetingEnabled; }
			set { PluginSettings.Instance.GreetingEnabled = value; }
		}

		[Category( "Join Greeting" )]
		[Description( "Greeting Dialog Displayed To User On Join" )]
		[Browsable( true )]
		[ReadOnly( false )]
		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public SettingsGreetingDialogItem GreetingItem
		{
			get { return PluginSettings.Instance.GreetingItem; }
			set { PluginSettings.Instance.GreetingItem = value; }
		}

		[Category( "Join Greeting" )]
		[Description( "Greeting Sent To User On Join" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public string GreetingMessage
		{
			get
			{
				return PluginSettings.Instance.GreetingMessage;
			}

			set
			{
				PluginSettings.Instance.GreetingMessage = value;
			}
		}

		[Category( "Join Greeting" )]
		[Description( "Enable / Disable Sending this message to all users, and not just to the joining user privately" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool GreetingPublic
		{
			get { return PluginSettings.Instance.GreetingPublic; }
			set { PluginSettings.Instance.GreetingPublic = value; }
		}

		[Category( "Join Greeting" )]
		[Description( "Greeting Dialog Displayed To User On Join" )]
		[Browsable( true )]
		[ReadOnly( false )]
		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public SettingsGreetingDialogItem GreetingNewUserItem
		{
			get { return PluginSettings.Instance.GreetingNewUserItem; }
			set { PluginSettings.Instance.GreetingNewUserItem = value; }
		}


		[Category( "Join Greeting" )]
		[Description( "Greeting Sent To New Users On Join" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public string NewUserGreetingMessage
		{
			get
			{
				return PluginSettings.Instance.GreetingNewUserMessage;
			}

			set
			{
				PluginSettings.Instance.GreetingNewUserMessage = value;
			}
		}

		[Category( "Automated Backup" )]
		[Description( "Enable / Disabled Automated Backups" )]
		[Browsable( true )]
		[ReadOnly( false )]
		[DefaultValue( true )]
		public bool BackupEnabled
		{
			get { return PluginSettings.Instance.BackupEnabled; }
			set { PluginSettings.Instance.BackupEnabled = value; }
		}

		[Category( "Automated Backup" )]
		[Description( "Backup Items. These items control when backups occur." )]
		[Browsable( true )]
		[ReadOnly( false )]
		[DisplayName( @"Backup Times" )]
		public MTObservableCollection<BackupItem> BackupItems
		{
			get { return PluginSettings.Instance.BackupItems; }
		}

		[Category( "Automated Backup" )]
		[Description( "Base directory to put backups into" )]
		[Browsable( true )]
		[ReadOnly( false )]
		[Editor( typeof( FolderNameEditor ), typeof( UITypeEditor ) )]
		public string BackupBaseDirectory
		{
			get { return PluginSettings.Instance.BackupBaseDirectory; }
			set { PluginSettings.Instance.BackupBaseDirectory = value; }
		}

		[Category( "Automated Backup" )]
		[Description( "Should each backup go into a dated subdirectory?" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool BackupCreateSubDirectories
		{
			get { return PluginSettings.Instance.BackupCreateSubDirectories; }
			set { PluginSettings.Instance.BackupCreateSubDirectories = value; }
		}

		[Category( "Automated Backup" )]
		[Description( "Should backups be cleaned up after a certain period of time?" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool BackupCleanup
		{
			get { return PluginSettings.Instance.BackupCleanup; }
			set { PluginSettings.Instance.BackupCleanup = value; }
		}

		[Category( "Automated Backup" )]
		[Description( "Time in days to keep a backup" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public int BackupCleanupTime
		{
			get { return PluginSettings.Instance.BackupCleanupTime; }
			set { PluginSettings.Instance.BackupCleanupTime = value; }
		}

		[Category( "Automated Backup" )]
		[Description( "Enable / Disable backing up of asteroids" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool BackupAsteroids
		{
			get { return PluginSettings.Instance.BackupAsteroids; }
			set
			{
				PluginSettings.Instance.BackupAsteroids = value;
			}
		}

		[Category( "Automated Backup" )]
		[Description( "Enable / Disable Backing up of essentials settings" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool BackupEssentials
		{
			get { return PluginSettings.Instance.BackupEssentials; }
			set
			{
				PluginSettings.Instance.BackupEssentials = value;
			}
		}


		[Category( "Automated New User Transport" )]
		[Description( "Enable / Disable New User Transport.  This option will transport new user ships closer to asteroids.  It will randomly choose an appropriate asteroid that has enough resources inside it, and move them closer to that asteroid.  This helps on servers that have asteroids far apart." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool NewUserTransportEnabled
		{
			get { return PluginSettings.Instance.NewUserTransportEnabled; }
			set { PluginSettings.Instance.NewUserTransportEnabled = value; }
		}

		[Category( "Automated New User Transport" )]
		[Description( "New User Transport Distance from asteroid to transport the user." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public int NewUserTransportDistance
		{
			get { return PluginSettings.Instance.NewUserTransportDistance; }
			set { PluginSettings.Instance.NewUserTransportDistance = value; }
		}

		[Category( "Automated New User Transport" )]
		[Description( "Move all spawn ships no matter if the user is new or not (Only works on login for now)" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool NewUserTransportMoveAllSpawnShips
		{
			get { return PluginSettings.Instance.NewUserTransportMoveAllSpawnShips; }
			set { PluginSettings.Instance.NewUserTransportMoveAllSpawnShips = value; }
		}

		[Category( "Automated New User Transport" )]
		[Description( "Spawn only at asteroids that are this distance or less from center (0,0,0) - 0 means asteroids can be anywhere" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public int NewUserTransportAsteroidDistance
		{
			get { return PluginSettings.Instance.NewUserTransportAsteroidDistance; }
			set { PluginSettings.Instance.NewUserTransportAsteroidDistance = value; }
		}

		[Category( "Automated New User Transport" )]
		[Description( "Type of spawn point we use.  Asteroids = we spawn near asteroids.  Origin = we spawn near origin." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public NewUserTransportSpawnPoint NewUserTransportSpawnType
		{
			get { return PluginSettings.Instance.NewUserTransportSpawnType; }
			set { PluginSettings.Instance.NewUserTransportSpawnType = value; }
		}

		[Category( "Automated New User Transport" )]
		[Description( "The names of spawnships.  Used for automated spawn ship stopping." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public string[ ] NewUserTransportSpawnShipNames
		{
			get { return PluginSettings.Instance.NewUserTransportSpawnShipNames; }
			set { PluginSettings.Instance.NewUserTransportSpawnShipNames = value; }
		}

		[Category( "Automated New User Transport" )]
		[Description( "Enable / Disable automatic spawn ship stopping.  The theory is, they cause havok errors if they jump physics worlds." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool NewUserTransportStopRunawaySpawnShips
		{
			get { return PluginSettings.Instance.NewUserTransportStopRunawaySpawnShips; }
			set { PluginSettings.Instance.NewUserTransportStopRunawaySpawnShips = value; }
		}

		[Category( "Player Login Tracking" )]
		[Description( "Enable / Disable Player Login Tracking.  This option tracks users login/logouts.  It also reads old logs to get player history.  It's recommended to enable this." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool LoginEnabled
		{
			get { return PluginSettings.Instance.LoginEnabled; }
			set { PluginSettings.Instance.LoginEnabled = value; }
		}

		[Category( "Player Login Tracking" )]
		[Description( "This is an entity whitelist for player logins.  Any entity in this list will never be considered 'inactive'." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public string[ ] LoginEntityWhitelist
		{
			get { return PluginSettings.Instance.LoginEntityWhitelist; }
			set { PluginSettings.Instance.LoginEntityWhitelist = value; }
		}

		[Category( "Player Login Tracking" )]
		[Description( "This is a player whitelist for player logins.  Any object owned by a player in this list will never be considered 'inactive'" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public string[ ] LoginPlayerIdWhitelist
		{
			get { return PluginSettings.Instance.LoginPlayerIdWhitelist; }
			set { PluginSettings.Instance.LoginPlayerIdWhitelist = value; }
		}

		[Category( "Protected Entities" )]
		[Description( "Enable / Disable Protected Entities.  Protected entities have increased integrity, and also heal repair / fix deformation instantly when damaged." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool ProtectedEnabled
		{
			get { return PluginSettings.Instance.ProtectedEnabled; }
			set { PluginSettings.Instance.ProtectedEnabled = value; }
		}

		[Category( "Protected Entities" )]
		[Description( "This is the list of entities that are protected by the server" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<ProtectedItem> ProtectedItems
		{
			get { return PluginSettings.Instance.ProtectedItems; }
		}

		[Category( "Docking Zones" )]
		[Description( "Enable / Disable docking zones.  This allows players to safely dock in zones created by 4 beacons with the same name.  This removes the entity from the world when docked, and adds it back when undocked." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool DockingEnabled
		{
			get { return PluginSettings.Instance.DockingEnabled; }
			set { PluginSettings.Instance.DockingEnabled = value; }
		}

		[Category( "Docking Zones" )]
		[Description( "Sets the number of ships that can dock in a zone.  Default is 1" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public int DockingShipsPerZone
		{
			get { return PluginSettings.Instance.DockingShipsPerZone; }
			set { PluginSettings.Instance.DockingShipsPerZone = value; }
		}

		[Category( "Dynamic Entity Management" )]
		[DisplayName( "Enabled")]
		[Description( "Enable / Disable dynamic entity concealment.  This option will automatically 'hide' ships that players are not close to, meaning they won't be processed by the physics engine.  This should improve performance." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool DynamicConcealEnabled
		{
			get { return PluginSettings.Instance.DynamicConcealEnabled; }
			set { PluginSettings.Instance.DynamicConcealEnabled = value; }
		}

		[Category( "Dynamic Entity Management" )]
		[DisplayName( "Conceal Distance")]
		[Description( "The distance a player must be from a grid for it to be revealed due to distance.  The smaller this value is, the longer a grid will be hidden from sight.  Default is 8000m (max view distance)" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public float DynamicConcealDistance
		{
			get { return PluginSettings.Instance.DynamicConcealDistance; }
			set { PluginSettings.Instance.DynamicConcealDistance = value; }
		}

		[Category( "Dynamic Entity Management" )]
		[DisplayName( "Include Large Grids")]
		[Description( "Enable / Disable management of large block grids.  Large block grids require a different set of scans due to them having medical bays." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool DynamicConcealIncludeLargeGrids
		{
			get { return PluginSettings.Instance.ConcealIncludeLargeGrids; }
			set { PluginSettings.Instance.ConcealIncludeLargeGrids = value; }
		}

		[Category( "Dynamic Entity Management" )]
		[DisplayName( "Block Subtype Ignore List")]
		[Description( "The list of subtype blocks that will make the entity manager ignore a grid.  If a grid contains any of these block subtypes, it will automatically not include it when deciding whether to conceal the grid or not" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public string[ ] DynamicConcealIgnoreSubTypeList
		{
			get { return PluginSettings.Instance.DynamicConcealIgnoreSubTypeList; }
			set { PluginSettings.Instance.DynamicConcealIgnoreSubTypeList = value; }
		}

		[Category( "Dynamic Entity Management" )]
		[DisplayName( "Include MedBays")]
		[Description( "The list of subtype blocks that will make the entity manager ignore a grid.  If a grid contains any of these block subtypes, it will automatically not include it when deciding whether to conceal the grid or not" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool DynamicConcealIncludeMedBays
		{
			get { return PluginSettings.Instance.DynamicConcealIncludeMedBays; }
			set { PluginSettings.Instance.DynamicConcealIncludeMedBays = value; }
		}

		/*  Experiments not working yet */
		/*
		[Category("Dynamic Entity Management")]
		[Description("")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DynamicConcealServerOnly
		{
			get { return PluginSettings.Instance.DynamicConcealServerOnly; }
			set
			{
				PluginSettings.Instance.DynamicConcealServerOnly = value;
			}
		}

		[Category("Dynamic Entity Management")]
		[Description("")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DynamicClientConcealEnabled
		{
			get { return PluginSettings.Instance.DynamicClientConcealEnabled; }
			set
			{
				PluginSettings.Instance.DynamicClientConcealEnabled = value;
			}
		}

		[Category("Dynamic Entity Management")]
		[Description("")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float DynamicClientConcealDistance
		{
			get { return PluginSettings.Instance.DynamicClientConcealDistance; }
			set
			{
				PluginSettings.Instance.DynamicClientConcealDistance = value;
			}
		}
		/**/
		[Category( "Dynamic Entity Management" )]
		[DisplayName( "Log Actions" )]
		[Description( "Enable / Disable console messages that display whether an entity is concealed or revealed.  Should be off if you don't care about seeing how many entities get revealed/concealed." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool DynamicShowMessages
		{
			get { return PluginSettings.Instance.DynamicShowMessages; }
			set
			{
				PluginSettings.Instance.DynamicShowMessages = value;
			}
		}

		[Category( "Dynamic Turret Management" )]
		[DisplayName( "Enabled" )]
		[Description( "Enable / Disable dynamic management of turrets.  Turrets that do not have a valid target within their target range will be disabled.  This will improve sim speed." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool DynamicTurretManagmentEnabled
		{
			get { return PluginSettings.Instance.DynamicTurretManagmentEnabled; }
			set
			{
				PluginSettings.Instance.DynamicTurretManagmentEnabled = value;
			}
		}

		[Category( "Dynamic Turret Management" )]
		[DisplayName( "Activation Distance" )]
		[Description( "Distance from a grid that has a turret that a valid target must be before enabling.  Should be 2x or more than the scan distance of a turret." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public int DynamicTurretTargetDistance
		{
			get { return PluginSettings.Instance.DynamicTurretTargetDistance; }
			set
			{
				PluginSettings.Instance.DynamicTurretTargetDistance = value;
			}
		}

		[Category( "Dynamic Turret Management" )]
		[DisplayName( "Allow Exemption" )]
		[Description( "Enable / Disable Allowing users to manually exempt their turrets from control with the [ManualControl] tag in custom name of turret." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool DynamicTurretAllowExemption
		{
			get { return PluginSettings.Instance.DynamicTurretAllowExemption; }
			set
			{
				PluginSettings.Instance.DynamicTurretAllowExemption = value;
			}
		}

		[Category( "Dynamic Turret Management" )]
		[Description( "Set the type of management the server does.  All means anything will turn the turrets back on.  All but owner means everything except owner of turret turns them on.  EnemyAndNeutral mean enemy and neutral players/grids turn the turrets back on." )]
		[Browsable( true )]
		[ReadOnly( false )]
		[DisplayName( "Mode" )]
		public DynamicTurretManagementMode DynamicTurretManagementMode
		{
			get { return PluginSettings.Instance.DynamicTurretManagementMode; }
			set
			{
				PluginSettings.Instance.DynamicTurretManagementMode = value;
			}
		}

        [Category("Dynamic Entity Management")]
        [Description("Enable / Disable dynamic voxel management.")]
        [Browsable(true)]
        [ReadOnly(false)]
        public bool DynamicVoxelManagementEnabled
        {
            get { return PluginSettings.Instance.DynamicVoxelManagementEnabled; }
            set
            {
                PluginSettings.Instance.DynamicVoxelManagementEnabled = value;
            }
        }

        [Category("Dynamic Entity Management")]
        [Description("Sets the distance in which voxels are sent to the client")]
        [Browsable(true)]
        [ReadOnly(false)]
        public int DynamicVoxelDistance
        {
            get { return PluginSettings.Instance.DynamicVoxelDistance; }
            set
            {
                PluginSettings.Instance.DynamicVoxelDistance = value;
            }
        }

		/*
		[Category("Dynamic Entity Management")]
		[Description("Enable / Disable dynamic block management.  This manager disables blocks of ships that can't be concealed to further increase gamelogic savings.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DynamicBlockManagementEnabled
		{
			get { return PluginSettings.Instance.DynamicBlockManagementEnabled; }
			set
			{
				PluginSettings.Instance.DynamicBlockManagementEnabled = value;
			}
		}
		*/

		[Category( "Waypoint System" )]
		[Description( "Enable / Disable personal waypoints.  These are hud displayed waypoints that only a user can see." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool WaypointsEnabled
		{
			get { return PluginSettings.Instance.WaypointsEnabled; }
			set
			{
				PluginSettings.Instance.WaypointsEnabled = value;
			}
		}

		[Category( "Waypoint System" )]
		[Description( "Maximum personal waypoints a player can have." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public int WaypointsMaxPerPlayer
		{
			get { return PluginSettings.Instance.WaypointsMaxPerPlayer; }
			set
			{
				PluginSettings.Instance.WaypointsMaxPerPlayer = value;
			}
		}

		[Category( "Waypoint System" )]
		[Description( "Server waypoints that every user will see and can not removed" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<ServerWaypointItem> WaypointServerItems
		{
			get { return PluginSettings.Instance.WaypointServerItems; }
		}

		[Category( "Waypoint System" )]
		[Description( "Default waypoints added for a user if they have none or are new" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<ServerWaypointItem> WaypointDefaultItems
		{
			get { return PluginSettings.Instance.WaypointDefaultItems; }
		}

		[Category( "Waypoint System" )]
		[Description( "Maximum faction waypoints per faction" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public int WaypointsMaxPerFaction
		{
			get { return PluginSettings.Instance.WaypointsMaxPerFaction; }
			set { PluginSettings.Instance.WaypointsMaxPerFaction = value; }
		}

		[Category( "Automated Cleanup System" )]
		[Description( "Enable / Disable automated cleanup" )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool CleanupEnabled
		{
			get { return PluginSettings.Instance.CleanupEnabled; }
			set { PluginSettings.Instance.CleanupEnabled = value; }
		}

		[Category( "Automated Cleanup System" )]
		[Description( "Items that are triggered by a reached capacity.  These cleanup items will execute when a certain limit has been reached." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<SettingsCleanupTriggerItem> CleanupTriggerItems
		{
			get { return PluginSettings.Instance.CleanupTriggerItems; }
		}

		[Category( "Automated Cleanup System" )]
		[Description( "Items that are triggered by time.  These items will execute when a certain time of the day is reached." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<SettingsCleanupTimedItem> CleanupTimedItems
		{
			get { return PluginSettings.Instance.CleanupTimedItems; }
		}

		[Category( "Automated Cleanup System" )]
		[Description( "Notifications to users that a cleanup is about to occur." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<SettingsCleanupNotificationItem> CleanupNotificationItems
		{
			get { return PluginSettings.Instance.CleanupNotificationItems; }
		}

		[Category( "Block Enforcement System" )]
		[Description( "Enable / Disable block type max count enforcement.  This removes blocks that exceed a certain count.  They are removed forcefully from a grid.  When max is reached, a warning is given.  When max is exceeded another warning." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public bool BlockEnforcementEnabled
		{
			get { return PluginSettings.Instance.BlockEnforcementEnabled; }
			set { PluginSettings.Instance.BlockEnforcementEnabled = value; }
		}

		[Category( "Block Enforcement System" )]
		[Description( "Enforcement Items.  These are how block enforcements are defined.  Each item is a block that is scanned for." )]
		[Browsable( true )]
		[ReadOnly( false )]
		public MTObservableCollection<SettingsBlockEnforcementItem> BlockEnforcementItems
		{
			get { return PluginSettings.Instance.BlockEnforcementItems; }
		}

		/*
		[Category("Game Modes")]
		[Description("Conquest Game Mode - This mode tracks asteroid owners by counting owned blocks near an asteroid to determine the owner.  Includes a leaderboard")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool GameModeConquestEnabled
		{
			get { return PluginSettings.Instance.GameModeConquestEnabled; }
			set
			{
				PluginSettings.Instance.GameModeConquestEnabled = value;
			}
		}
		*/
		#endregion

		#region Constructors and Initializers

		private void DoInit( string path )
		{
			Instance = this;
			//controlForm.Text = "Testing";
			_pluginPath = path;

			// Load our settings
			PluginSettings.Instance.Load( );

			// Setup process handlers
			_processHandlers = new List<ProcessHandlerBase>
			                   {
				                   new ProcessNewUserTransport( ),
				                   new ProcessGreeting( ),
				                   new ProcessRestart( ),
				                   new ProcessInfo( ),
				                   new ProcessCommunication( ),
				                   new ProcessBackup( ),
				                   new ProcessLoginTracking( ),
				                   new ProcessProtection( ),
				                   new ProcessDockingZone( ),
				                   new ProcessConceal( ),
				                   new ProcessWaypoints( ),
				                   new ProcessCleanup( ),
				                   new ProcessBlockEnforcement( ),
				                   new ProcessSpawnShipTracking( )
			                   };

			// Setup chat handlers
			_chatHandlers = new List<ChatHandlerBase>
			                {
				                new HandleInfo( ),
				                new HandleTimeleft( ),
				                new HandlePos( ),
				                new HandleMsg( ),
				                new HandleFaction( ),
				                new HandleFactionF( ),
				                new HandleMotd( ),

				                new HandleAdminScanAreaAt( ),
				                new HandleAdminScanAreaTowards( ),
				                new HandleAdminScanNoBeacon( ),
				                new HandleAdminScanInactive( ),
				                new HandleAdminScanEntityId( ),
				                new HandleAdminScanCleanup( ),
				                new HandleAdminScanOverlimit( ),
				                new HandleAdminScanGrids( ),

				                new HandleAdminMoveAreaToPosition( ),
				                new HandleAdminMoveAreaTowards( ),
				                new HandleAdminMovePlayerTo( ),
				                new HandleAdminMovePlayerPosition( ),
				                new HandleAdminMoveGridTo( ),

				                new HandleAdminDeleteGridsArea( ),
				                new HandleAdminDeleteShipsArea( ),
				                new HandleAdminDeleteStationsArea( ),
				                new HandleAdminDeleteNoBeacon( ),
				                new HandleAdminDeleteInactive( ),
				                new HandleAdminDeleteCleanup( ),
				                new HandleAdminDeleteGrids( ),

				                new HandleAdminSettings( ),
				                new HandleAdminNotify( ),
				                new HandleAdminBackup( ),
				                new HandleAdminRestart( ),
				                new HandleAdminMemory( ),

				                new HandleAdminOwnershipChange( ),

				                new HandleAdminPlayerListActive( ),
				                new HandleAdminPlayerListInactive( ),

				                new HandleAdminConceal( ),
				                new HandleAdminReveal( ),

				                new HandleDockValidate( ),
				                new HandleDockDock( ),
				                new HandleDockUndock( ),
				                new HandleDockList( ),

				                new HandleWaypointAdd( ),
				                new HandleWaypointRemove( ),
				                new HandleWaypointList( ),
				                new HandleWaypointGroupAdd( ),
				                new HandleWaypointGroupRemove( ),
				                new HandleWaypointToggle( ),
				                new HandleWaypointRefresh( ),
				                new HandleWaypointFactionAdd( ),
				                new HandleWaypointFactionRemove( ),

				                new HandleUtilityExportServer( ),
				                new HandleUtilityGridsList( ),
				                new HandleUtilityGridsRefresh( ),
				                new HandleUtilityGridsCompare( ),

				                new HandleAdminTest( )
			                };

			_processThreads = new List<Thread>( );
			_processThread = new Thread( PluginProcessing );
			_processThread.Start( );

			MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
			MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
			MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
			MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;

			Log.Info( "Plugin '{0}' initialized. (Version: {1}  ID: {2})", Name, Version, Id );
		}

		#endregion

		#region Processing Loop
		private void PluginProcessing( )
		{
			try
			{
				foreach ( ProcessHandlerBase handler in _processHandlers )
				{
					ProcessHandlerBase currentHandler = handler;
					Thread thread = new Thread( ( ) =>
					{
						while ( _running )
						{
							if ( currentHandler.CanProcess( ) )
							{
								try
								{
									currentHandler.Handle( );
								}
								catch ( Exception ex )
								{
									Log.Warn( "Handler Problems: {0} - {1}", currentHandler.GetUpdateResolution( ), ex );
								}

								// Let's make sure LastUpdate is set to now otherwise we may start processing too quickly
								currentHandler.LastUpdate = DateTime.Now;
							}

							Thread.Sleep( 100 );
						}
					} );

					_processThreads.Add( thread );
					thread.Start( );
				}

				foreach ( Thread thread in _processThreads )
					thread.Join( );

				/*
				while (true)
				{
					if (DateTime.Now - m_lastProcessUpdate > TimeSpan.FromMilliseconds(100))
					{
						Parallel.ForEach(_processHandlers, handler => 
						{
							if (handler.CanProcess())
							{
								try
								{
									handler.Handle();
								}
								catch (Exception ex)
								{
									Log.Info(String.Format("Handler Problems: {0} - {1}", handler.GetUpdateResolution(), ex.ToString()));
								}

								// Let's make sure LastUpdate is set to now otherwise we may start processing too quickly
								handler.LastUpdate = DateTime.Now;
							}
						});

						//foreach (ProcessHandlerBase handler in _processHandlers)
						//{
						//}
						m_lastProcessUpdate = DateTime.Now;
					}
					Thread.Sleep(25);
				}
				*/

			}
			catch ( Exception ex )
			{
				Log.Error( ex );
			}
			finally
			{
				MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
				MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
			}
		}
		#endregion

		#region IPlugin Members
		public void Init( )
		{
			Log.Debug( "Initializing Essentials plugin at path {0}\\", Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).Location ) );
			DoInit( Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).Location ) + "\\" );
		}

		public void InitWithPath( String modPath )
		{
			Log.Debug( "Initializing Essentials plugin at path {0}\\", Path.GetDirectoryName( modPath ) );
			DoInit( Path.GetDirectoryName( modPath ) + "\\" );
		}

		public void Shutdown( )
		{
			Log.Info( "Shutting down plugin: {0} - {1}", Name, Version );

			foreach ( Thread thread in _processThreads )
				thread.Abort( );

			_processThread.Abort( );
		}

		public void Update( )
		{

		}

		#endregion

		#region IChatEventHandler Members

		/// <summary>
		/// Raised when a chat messages are received.
		/// NOTE: This is raised on a different thread
		/// </summary>
		/// <param name="obj"></param>
		public void OnChatReceived( ChatManager.ChatEvent obj )
		{
			if ( obj.Message[ 0 ] != '/' )
				return;

			HandleChatMessage( obj.SourceUserId, obj.Message );
		}

		public void HandleChatMessage( ulong steamId, string message )
		{
			// Parse chat message
			ulong remoteUserId = steamId;
			List<string> commandParts = CommandParser.GetCommandParts( message );

			// User wants some help
			if ( commandParts[ 0 ].ToLower( ) == "/help" )
			{
				HandleHelpCommand( remoteUserId, commandParts );
				return;
			}

			// See if we have any valid handlers
			bool handled = false;
			foreach ( ChatHandlerBase chatHandler in _chatHandlers )
			{
				int commandCount = 0;
				if ( remoteUserId == 0 && !chatHandler.AllowedInConsole( ) )
					continue;

				if ( chatHandler.CanHandle( remoteUserId, commandParts.ToArray( ), ref commandCount ) )
				{
					try
					{
						chatHandler.HandleCommand( remoteUserId, commandParts.Skip( commandCount ).ToArray( ) );
					}
					catch ( Exception ex )
					{
						Log.Info( string.Format( "ChatHandler Error: {0}", ex ) );
					}

					handled = true;
				}
			}

			if ( !handled )
			{
				DisplayAvailableCommands( remoteUserId, message );
			}
		}

		/// <summary>
		/// This function displays available help for all the functionality of this plugin
		/// </summary>
		/// <param name="remoteUserId"></param>
		/// <param name="commandParts"></param>
		private void HandleHelpCommand( ulong remoteUserId, IReadOnlyCollection<string> commandParts )
		{
			if ( commandParts.Count == 1 )
			{
				List<string> commands = new List<string>( );
				foreach ( ChatHandlerBase handler in _chatHandlers )
				{
					// We should replace this to just have the handler return a string[] of base commands
					if ( handler.GetMultipleCommandText( ).Length < 1 )
					{
						string commandBase = handler.GetCommandText( ).Split( new[ ] { " " }, StringSplitOptions.RemoveEmptyEntries ).First( );
						if ( !commands.Contains( commandBase ) && ( !handler.IsClientOnly( ) ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0 ) ) ) )
						{
							commands.Add( commandBase );
						}
					}
					else
					{
						foreach ( string cmd in handler.GetMultipleCommandText( ) )
						{
							string commandBase = cmd.Split( new[ ] { " " }, StringSplitOptions.RemoveEmptyEntries ).First( );
							if ( !commands.Contains( commandBase ) && ( !handler.IsClientOnly( ) ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0 ) ) ) )
							{
								commands.Add( commandBase );
							}
						}
					}
				}

				string commandList = string.Join( ", ", commands );
				string info = string.Format( "Dedicated Server Essentials v{0}.  Available Commands: {1}", Version, commandList );
				Communication.SendPrivateInformation( remoteUserId, info );
			}
			else
			{
				string helpTarget = string.Join( " ", commandParts.Skip( 1 ).ToArray( ) );
				bool found = false;
				foreach ( ChatHandlerBase handler in _chatHandlers )
				{
					// Again, we should get handler to just return string[] of command Text
					if ( handler.GetMultipleCommandText( ).Length < 1 )
					{
						if ( String.Equals( handler.GetCommandText( ), helpTarget, StringComparison.CurrentCultureIgnoreCase ) )
						{
							Communication.SendPrivateInformation( remoteUserId, handler.GetHelp( ) );
							found = true;
						}
					}
					else
					{
						foreach ( string cmd in handler.GetMultipleCommandText( ) )
						{
							if ( String.Equals( cmd, helpTarget, StringComparison.CurrentCultureIgnoreCase ) )
							{
								Communication.SendPrivateInformation( remoteUserId, handler.GetHelp( ) );
								found = true;
							}
						}
					}
				}

				if ( !found )
				{
					List<string> helpTopics = new List<string>( );

					foreach ( ChatHandlerBase handler in _chatHandlers )
					{
						// Again, cleanup to one function
						string[ ] multipleCommandText = handler.GetMultipleCommandText( );
						if ( multipleCommandText.Length == 0 )
						{
							if ( handler.GetCommandText( ).ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ( ( !handler.IsAdminCommand( ) ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0 ) ) ) )
							{
								helpTopics.Add( handler.GetCommandText( ).ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
							}
						}
						else
						{
							foreach ( string cmd in multipleCommandText )
							{
								if ( cmd.ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ( ( !handler.IsAdminCommand( ) ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0 ) ) ) )
								{
									helpTopics.Add( cmd.ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
								}
							}
						}
					}

					if ( helpTopics.Any( ) )
					{
						Communication.SendPrivateInformation( remoteUserId, string.Format( "Help topics for command '{0}': {1}", helpTarget.ToLower( ), string.Join( ",", helpTopics.ToArray( ) ) ) );
						found = true;
					}
				}

				if ( !found )
					Communication.SendPrivateInformation( remoteUserId, "Unknown command" );
			}
		}

		/// <summary>
		/// Displays the available commands for the command entered
		/// </summary>
		/// <param name="remoteUserId"></param>
		/// <param name="recvMessage"></param>
		private void DisplayAvailableCommands( ulong remoteUserId, string recvMessage )
		{
			string message = recvMessage.ToLower( ).Trim( );
			List<string> availableCommands = new List<string>( );
			foreach ( ChatHandlerBase chatHandler in _chatHandlers )
			{
				// Cleanup to one function
				if ( chatHandler.GetMultipleCommandText( ).Length < 1 )
				{
					string command = chatHandler.GetCommandText( );
					if ( command.StartsWith( message ) )
					{
						string[ ] cmdPart = command.Replace( message, string.Empty ).Trim( ).Split( new[ ] { ' ' } );

						if ( !availableCommands.Contains( cmdPart[ 0 ] ) )
							availableCommands.Add( cmdPart[ 0 ] );
					}
				}
				else
				{
					foreach ( string command in chatHandler.GetMultipleCommandText( ) )
					{
						if ( command.StartsWith( message ) )
						{
							string[ ] cmdPart = command.Replace( message, string.Empty ).Trim( ).Split( new[ ] { ' ' } );

							if ( !availableCommands.Contains( cmdPart[ 0 ] ) )
								availableCommands.Add( cmdPart[ 0 ] );
						}
					}
				}
			}

			if ( availableCommands.Any( ) )
			{
				Communication.SendPrivateInformation( remoteUserId, string.Format( "Available subcommands for '{0}' command: {1}", message, string.Join( ", ", availableCommands.ToArray( ) ) ) );
			}
		}

		public void OnChatSent( ChatManager.ChatEvent obj )
		{

		}

		#endregion

		#region ICubeGridHandler Members

		public void OnEntityAdd( IMyEntity obj )
		{
			ThreadPool.QueueUserWorkItem( new WaitCallback( ( object state ) =>
			{
				foreach ( ProcessHandlerBase handler in _processHandlers )
					handler.OnEntityAdd( obj );
			} ) );
		}

		public void OnEntityRemove( IMyEntity obj )
		{
			ThreadPool.QueueUserWorkItem( new WaitCallback( ( object state ) =>
			{
				foreach ( ProcessHandlerBase handler in _processHandlers )
					handler.OnEntityRemove( obj );
			} ) );
		}

		public void OnCubeGridCreated( CubeGridEntity cubeGrid )
		{
			foreach ( ProcessHandlerBase handler in _processHandlers )
				handler.OnCubeGridCreated( cubeGrid );
		}

		public void OnCubeGridDeleted( CubeGridEntity cubeGrid )
		{
			foreach ( ProcessHandlerBase handler in _processHandlers )
				handler.OnCubeGridDeleted( cubeGrid );
		}

		public void OnCubeGridLoaded( CubeGridEntity cubeGrid )
		{

		}

		public void OnCubeGridMoved( CubeGridEntity cubeGrid )
		{

		}

		#endregion

		#region ICubeBlockEventHandler Members

		public void OnCubeBlockCreated( CubeBlockEntity entity )
		{

		}

		public void OnCubeBlockDeleted( CubeBlockEntity entity )
		{
		}

		#endregion

		#region IPlayerEventHandler Members

		public void OnPlayerJoined( ulong remoteUserId )
		{
			foreach ( ProcessHandlerBase handler in _processHandlers )
				handler.OnPlayerJoined( remoteUserId );
		}

		public void OnPlayerLeft( ulong remoteUserId )
		{
			foreach ( ProcessHandlerBase handler in _processHandlers )
				handler.OnPlayerLeft( remoteUserId );
		}

		public void OnPlayerWorldSent( ulong remoteUserId )
		{
			foreach ( ProcessHandlerBase handler in _processHandlers )
				handler.OnPlayerWorldSent( remoteUserId );
		}

		#endregion

		public void OnSectorSaved( object state )
		{
			foreach ( ProcessHandlerBase handler in _processHandlers )
				handler.OnSectorSaved( );
		}

		#region IPlugin Members

		public Guid Id
		{
			get
			{
				GuidAttribute guidAttr = (GuidAttribute)typeof( Essentials ).Assembly.GetCustomAttributes( typeof( GuidAttribute ), true )[ 0 ];
				return new Guid( guidAttr.Value );
			}
		}

		public string Name
		{
			get
			{
				return "Dedicated Server Essentials";
			}
		}

		public Version Version
		{
			get
			{
				return typeof( Essentials ).Assembly.GetName( ).Version;
			}
		}

		#endregion
	}
}
