using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.ComponentModel.Design;

using SEModAPIExtensions.API;
using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;

using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Common;

using EssentialsPlugin.Utility;
using EssentialsPlugin.ProcessHandler;
using EssentialsPlugin.ChatHandlers;
using EssentialsPlugin.UtilityClasses;
using EssentialsPlugin.Settings;

namespace EssentialsPlugin
{
	public class Essentials : IPlugin, IChatEventHandler, IPlayerEventHandler, ICubeGridHandler, ICubeBlockEventHandler
	{
		#region Private Fields
		internal static Essentials m_instance;
		private static string m_pluginPath;
		//private static ControlForm controlForm;
		private DateTime m_lastProcessUpdate;
		private Thread m_processThread;
		private List<Thread> m_processThreads;
		private List<ProcessHandlerBase> m_processHandlers;
		private List<ChatHandlerBase> m_chatHandlers;
		#endregion

		#region Properties
		public static string PluginPath
		{
			get { return m_pluginPath; }
			set { m_pluginPath = value; }
		}

		internal static Essentials Instance
		{
			get { return m_instance;  }
		}

		internal static List<ChatHandlerBase> ChatHandlers
		{
			get
			{
				if (m_instance != null)
				{
					return m_instance.m_chatHandlers;
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

		[Category("General")]
		[Description("Name of the server used throughout this plugin")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string ServerName
		{
			get { return PluginSettings.Instance.ServerName; }
			set { PluginSettings.Instance.ServerName = value; }
		}

		[Category("Information")]
		[Description("Enabled / Disable Information Commands")]
		[Browsable(true)]
		[ReadOnly(false)]
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

		[Category("Information")]
		[Description("Information command items.  These are accessed with the /info command.  Each item you define can be access by typing /info <info command> or they can be scheduled to be sent out at certain intervals.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public MTObservableCollection<InformationItem> InformationItems
		{ 
			get 
			{ 
				return PluginSettings.Instance.InformationItems; 
			} 
		}

		[Category("Automated Restart")]
		[Description("Enable / Disable Automated Server Restarts")]
		[Browsable(true)]
		[ReadOnly(false)]
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

		[Category("Automated Restart")]
		[Description("Restart Notification Items.  Each item is a notification sent to the user at specified intervals.  Use this to warn users that a restart is going to occur before it happens.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public MTObservableCollection<RestartNotificationItem> RestartNotificationItems
		{
			get
			{
				return PluginSettings.Instance.RestartNotificationItems;
			}
		}

		[Category("Automated Restart")]
		[Description("Restart Time Items.  Each item is a time of day when a restart will occur.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public MTObservableCollection<RestartTimeItem> RestartTimeItems
		{
			get
			{
				return PluginSettings.Instance.RestartTimeItems;
			}
		}

		[Category("Automated Restart")]
		[Description("Extra processes to run before the server is restarted.  Allows adding custom scripts during restart.  Each line is a seperate process.")]
		[Browsable(true)]
		[ReadOnly(false)]
		[EditorAttribute(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
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

		[Category("Automated Restart")]
		[Description("Enable / Disable A trigger which automatically restarts the server if it becomes unresponsive for 1 full minute.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool RestartWhenUnresponsive
		{
			get { return PluginSettings.Instance.RestartWhenUnresponsive; }
			set { PluginSettings.Instance.RestartWhenUnresponsive = value; }
		}

		[Category("Join Greeting")]
		[Description("Enable / Disable Join Greetings")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool GreetingEnabled
		{
			get { return PluginSettings.Instance.GreetingEnabled; }
			set { PluginSettings.Instance.GreetingEnabled = value; }
		}

		[Category("Join Greeting")]
		[Description("Greeting Dialog Displayed To User On Join")]
		[Browsable(true)]		
		[ReadOnly(false)]
		[TypeConverter(typeof(ExpandableObjectConverter))]
		public SettingsGreetingDialogItem GreetingItem
		{
			get { return PluginSettings.Instance.GreetingItem; }
			set { PluginSettings.Instance.GreetingItem = value; }
		}

		[Category("Join Greeting")]
		[Description("Greeting Sent To User On Join")]
		[Browsable(true)]
		[ReadOnly(false)]
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

		[Category("Join Greeting")]
		[Description("Greeting Dialog Displayed To User On Join")]
		[Browsable(true)]
		[ReadOnly(false)]
		[TypeConverter(typeof(ExpandableObjectConverter))]
		public SettingsGreetingDialogItem GreetingNewUserItem
		{
			get { return PluginSettings.Instance.GreetingNewUserItem; }
			set { PluginSettings.Instance.GreetingNewUserItem = value; }
		}


		[Category("Join Greeting")]
		[Description("Greeting Sent To New Users On Join")]
		[Browsable(true)]
		[ReadOnly(false)]
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

		[Category("Automated Backup")]
		[Description("Enable / Disabled Automated Backups")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool BackupEnabled
		{
			get { return PluginSettings.Instance.BackupEnabled;  }
			set { PluginSettings.Instance.BackupEnabled = value; }
		}

		[Category("Automated Backup")]
		[Description("Backup Items. These items control when backups occur.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public MTObservableCollection<BackupItem> BackupItems
		{
			get { return PluginSettings.Instance.BackupItems; }
		}

		[Category("Automated Backup")]
		[Description("Base directory to put backups into")]
		[Browsable(true)]
		[ReadOnly(false)]
		[EditorAttribute(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(System.Drawing.Design.UITypeEditor))]		
		public string BackupBaseDirectory
		{
			get { return PluginSettings.Instance.BackupBaseDirectory; }
			set { PluginSettings.Instance.BackupBaseDirectory = value; }
		}

		[Category("Automated Backup")]
		[Description("Should each backup go into a dated subdirectory?")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool BackupCreateSubDirectories
		{
			get { return PluginSettings.Instance.BackupCreateSubDirectories; }
			set { PluginSettings.Instance.BackupCreateSubDirectories = value; }
		}

		[Category("Automated Backup")]
		[Description("Should backups be cleaned up after a certain period of time?")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool BackupCleanup
		{
			get { return PluginSettings.Instance.BackupCleanup; }
			set { PluginSettings.Instance.BackupCleanup = value; }
		}

		[Category("Automated Backup")]
		[Description("Time in days to keep a backup")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int BackupCleanupTime
		{
			get { return PluginSettings.Instance.BackupCleanupTime; }
			set { PluginSettings.Instance.BackupCleanupTime = value; }
		}

		[Category("Automated Backup")]
		[Description("Enable / Disable backing up of asteroids")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool BackupAsteroids
		{
			get { return PluginSettings.Instance.BackupAsteroids; }
			set 
			{
				PluginSettings.Instance.BackupAsteroids = value; 
			}
		}

		[Category("Automated New User Transport")]
		[Description("Enable / Disable New User Transport.  This option will transport new user ships closer to asteroids.  It will randomly choose an appropriate asteroid that has enough resources inside it, and move them closer to that asteroid.  This helps on servers that have asteroids far apart.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool NewUserTransportEnabled
		{
			get { return PluginSettings.Instance.NewUserTransportEnabled; }
			set { PluginSettings.Instance.NewUserTransportEnabled = value; }
		}

		[Category("Automated New User Transport")]
		[Description("New User Transport Distance from asteroid to transport the user.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int NewUserTransportDistance
		{
			get { return PluginSettings.Instance.NewUserTransportDistance; }
			set { PluginSettings.Instance.NewUserTransportDistance = value; }
		}
		/*
		[Category("Automated New User Transport")]
		[Description("Move all spawn ships no matter if the user is new or not")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool NewUserTransportMoveAllSpawnShips
		{
			get { return PluginSettings.Instance.NewUserTransportMoveAllSpawnShips;  }
			set { PluginSettings.Instance.NewUserTransportMoveAllSpawnShips = value; }
		}
		*/
		[Category("Player Login Tracking")]
		[Description("Enable / Disable Player Login Tracking.  This option tracks users login/logouts.  It also reads old logs to get player history.  It's recommended to enable this.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool LoginEnabled
		{
			get { return PluginSettings.Instance.LoginEnabled; }
			set { PluginSettings.Instance.LoginEnabled = value; }
		}

		[Category("Player Login Tracking")]
		[Description("This is an entity whitelist for player logins.  Any entity in this list will never be considered 'inactive'.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string[] LoginEntityWhitelist
		{
			get { return PluginSettings.Instance.LoginEntityWhitelist; }
			set { PluginSettings.Instance.LoginEntityWhitelist = value; }
		}

		[Category("Player Login Tracking")]
		[Description("This is a player whitelist for player logins.  Any object owned by a player in this list will never be considered 'inactive'")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string[] LoginPlayerIdWhitelist
		{
			get { return PluginSettings.Instance.LoginPlayerIdWhitelist; }
			set { PluginSettings.Instance.LoginPlayerIdWhitelist = value; }
		}

		[Category("Protected Entities")]
		[Description("Enable / Disable Protected Entities.  Protected entities have increased integrity, and also heal repair / fix deformation instantly when damaged.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool ProtectedEnabled
		{
			get { return PluginSettings.Instance.ProtectedEnabled; }
			set { PluginSettings.Instance.ProtectedEnabled = value; }
		}

		[Category("Protected Entities")]
		[Description("This is the list of entities that are protected by the server")]
		[Browsable(true)]
		[ReadOnly(false)]
		public MTObservableCollection<ProtectedItem> ProtectedItems
		{
			get { return PluginSettings.Instance.ProtectedItems; }
		}

		[Category("Docking Zones")]
		[Description("Enable / Disable docking zones.  This allows players to safely dock in zones created by 4 beacons with the same name.  This removes the entity from the world when docked, and adds it back when undocked.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DockingEnabled
		{
			get { return PluginSettings.Instance.DockingEnabled;  }
			set { PluginSettings.Instance.DockingEnabled = value; }
		}

		[Category("Docking Zones")]
		[Description("Sets the number of ships that can dock in a zone.  Default is 1")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int DockingShipsPerZone
		{
			get { return PluginSettings.Instance.DockingShipsPerZone; }
			set { PluginSettings.Instance.DockingShipsPerZone = value; }
		}

		[Category("Dynamic Entity Concealment")]
		[Description("Enable / Disable dynamic entity concealment.  This option will automatically 'hide' ships that players are not close to, meaning they won't be processed by the physics engine.  This should improve performance.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DynamicConcealEnabled
		{
			get { return PluginSettings.Instance.DynamicConcealEnabled; }
			set { PluginSettings.Instance.DynamicConcealEnabled = value; }
		}

		[Category("Dynamic Entity Concealment")]
		[Description("The distance a player must be from a grid for it to be revealed due to distance.  The smaller this value is, the longer a grid will be hidden from sight.  Default is 8000m (max view distance)")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float DynamicConcealDistance
		{
			get { return PluginSettings.Instance.DynamicConcealDistance; }
			set { PluginSettings.Instance.DynamicConcealDistance = value; }
		}

		[Category("Dynamic Entity Concealment")]
		[Description("Enable / Disable management of large block grids.  Large block grids require a different set of scans due to them having medical bays.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DynamicConcealIncludeLargeGrids
		{
			get { return PluginSettings.Instance.ConcealIncludeLargeGrids; }
			set { PluginSettings.Instance.ConcealIncludeLargeGrids = value; }
		}

		[Category("Dynamic Entity Concealment")]
		[Description("The list of subtype blocks that will make the entity manager ignore a grid.  If a grid contains any of these block subtypes, it will automatically not include it when deciding whether to conceal the grid or not")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string[] DynamicConcealIgnoreSubTypeList
		{
			get { return PluginSettings.Instance.DynamicConcealIgnoreSubTypeList; }
			set { PluginSettings.Instance.DynamicConcealIgnoreSubTypeList = value; }
		}

		[Category("Dynamic Entity Concealment")]
		[Description("The list of subtype blocks that will make the entity manager ignore a grid.  If a grid contains any of these block subtypes, it will automatically not include it when deciding whether to conceal the grid or not")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DynamicConcealIncludeMedBays
		{
			get { return PluginSettings.Instance.DynamicConcealIncludeMedBays; }
			set { PluginSettings.Instance.DynamicConcealIncludeMedBays = value; }
		}

		[Category("Dynamic Entity Concealment")]
		[Description("Enable / Disable console messages that display whether an entity is concealed or revealed.  Should be off if you don't care about seeing how many entities get revealed/concealed.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DynamicShowMessages
		{
			get { return PluginSettings.Instance.DynamicShowMessages; }
			set
			{
				PluginSettings.Instance.DynamicShowMessages = value;
			}
		}

		[Category("Waypoint System")]
		[Description("Enable / Disable personal waypoints.  These are hud displayed waypoints that only a user can see.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool WaypointsEnabled
		{
			get { return PluginSettings.Instance.WaypointsEnabled; }
			set
			{
				PluginSettings.Instance.WaypointsEnabled = value;
			}
		}

		[Category("Waypoint System")]
		[Description("Maximum personal waypoints a player can have.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int WaypointsMaxPerPlayer
		{
			get { return PluginSettings.Instance.WaypointsMaxPerPlayer; }
			set
			{
				PluginSettings.Instance.WaypointsMaxPerPlayer = value;
			}
		}

		#endregion

		#region Constructors and Initializers

		private void DoInit(string path)
		{
			m_instance = this;
			//controlForm.Text = "Testing";
			m_pluginPath = path;

			// Load our settings
			PluginSettings.Instance.Load();

			// Setup process handlers
			m_processHandlers = new List<ProcessHandlerBase>();
			m_processHandlers.Add(new ProcessNewUserTransport());
			m_processHandlers.Add(new ProcessGreeting());
			m_processHandlers.Add(new ProcessRestart());
			m_processHandlers.Add(new ProcessInfo());
			m_processHandlers.Add(new ProcessCommunication());
			m_processHandlers.Add(new ProcessBackup());
			m_processHandlers.Add(new ProcessLoginTracking());
			m_processHandlers.Add(new ProcessProtection());
			m_processHandlers.Add(new ProcessDockingZone());
			m_processHandlers.Add(new ProcessConceal());
			m_processHandlers.Add(new ProcessWaypoints());
			
			// Setup chat handlers
			m_chatHandlers = new List<ChatHandlerBase>();
			m_chatHandlers.Add(new HandleInfo());
			m_chatHandlers.Add(new HandleTimeleft());
			m_chatHandlers.Add(new HandlePos());
			m_chatHandlers.Add(new HandleMsg());
			m_chatHandlers.Add(new HandleFaction());
			m_chatHandlers.Add(new HandleFactionF());

			m_chatHandlers.Add(new HandleAdminScanAreaAt());          //
			m_chatHandlers.Add(new HandleAdminScanAreaTowards());     //
			m_chatHandlers.Add(new HandleAdminScanNoBeacon());        //
			m_chatHandlers.Add(new HandleAdminScanInactive());        //
			m_chatHandlers.Add(new HandleAdminScanEntityId());        //
			m_chatHandlers.Add(new HandleAdminScanCleanup());
			m_chatHandlers.Add(new HandleAdminScanOverlimit());
			m_chatHandlers.Add(new HandleAdminScanGrids());

			m_chatHandlers.Add(new HandleAdminMoveAreaToPosition());  //
			m_chatHandlers.Add(new HandleAdminMoveAreaTowards());     //
			m_chatHandlers.Add(new HandleAdminMovePlayerTo());        // 
			m_chatHandlers.Add(new HandleAdminMovePlayerPosition());  //
			m_chatHandlers.Add(new HandleAdminMoveGridTo());          //

			m_chatHandlers.Add(new HandleAdminDeleteGridsArea());     //
			m_chatHandlers.Add(new HandleAdminDeleteShipsArea());     //
			m_chatHandlers.Add(new HandleAdminDeleteStationsArea());  //
			m_chatHandlers.Add(new HandleAdminDeleteNoBeacon());      //
			m_chatHandlers.Add(new HandleAdminDeleteInactive());      //
			m_chatHandlers.Add(new HandleAdminDeleteCleanup());

			m_chatHandlers.Add(new HandleAdminSettings());
			m_chatHandlers.Add(new HandleAdminNotify());

			m_chatHandlers.Add(new HandleAdminOwnershipChange());     //

			m_chatHandlers.Add(new HandleAdminPlayerListActive());    //
			m_chatHandlers.Add(new HandleAdminPlayerListInactive());  //

			m_chatHandlers.Add(new HandleAdminConceal());
			m_chatHandlers.Add(new HandleAdminReveal());

			m_chatHandlers.Add(new HandleDockValidate());
			m_chatHandlers.Add(new HandleDockDock());
			m_chatHandlers.Add(new HandleDockUndock());
			m_chatHandlers.Add(new HandleDockList());

			m_chatHandlers.Add(new HandleWaypointAdd());
			m_chatHandlers.Add(new HandleWaypointRemove());
			m_chatHandlers.Add(new HandleWaypointList());

			m_chatHandlers.Add(new HandleUtilityExportServer());
			m_chatHandlers.Add(new HandleUtilityGridsList());

//			m_chatHandlers.Add(new HandleAdminPlayer());

			m_chatHandlers.Add(new HandleAdminTest());
//			m_chatHandlers.Add(new HandleAdmin());

			m_lastProcessUpdate = DateTime.Now;
			m_processThreads = new List<Thread>();
			m_processThread = new Thread(new ThreadStart(PluginProcessing));
			m_processThread.Start();

			Logging.WriteLineAndConsole(string.Format("Plugin '{0}' initialized. (Version: {1}  ID: {2})", Name, Version, Id));
		}

		#endregion

		#region Processing Loop
		private void PluginProcessing()
		{
			try
			{
				foreach (ProcessHandlerBase handler in m_processHandlers)
				{
					Thread thread = new Thread(() =>
					{
						while (true)
						{
							if (handler.CanProcess())
							{
								try
								{
									handler.Handle();
								}
								catch (Exception ex)
								{
									Logging.WriteLineAndConsole(String.Format("Handler Problems: {0} - {1}", handler.GetUpdateResolution(), ex.ToString()));
								}

								// Let's make sure LastUpdate is set to now otherwise we may start processing too quickly
								handler.LastUpdate = DateTime.Now;
							}

							Thread.Sleep(100);
						}
					});

					m_processThreads.Add(thread);
					thread.Start();
				}

				foreach (Thread thread in m_processThreads)
					thread.Join();

				/*
				while (true)
				{
					if (DateTime.Now - m_lastProcessUpdate > TimeSpan.FromMilliseconds(100))
					{
						Parallel.ForEach(m_processHandlers, handler => 
						{
							if (handler.CanProcess())
							{
								try
								{
									handler.Handle();
								}
								catch (Exception ex)
								{
									Logging.WriteLineAndConsole(String.Format("Handler Problems: {0} - {1}", handler.GetUpdateResolution(), ex.ToString()));
								}

								// Let's make sure LastUpdate is set to now otherwise we may start processing too quickly
								handler.LastUpdate = DateTime.Now;
							}
						});

						//foreach (ProcessHandlerBase handler in m_processHandlers)
						//{
						//}
						m_lastProcessUpdate = DateTime.Now;
					}
					Thread.Sleep(25);
				}
				*/

			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("PluginProcessing(): {0}", ex.ToString()));
			}
		}
		#endregion

		#region IPlugin Members
		public void Init()
		{
			DoInit(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\");
		}

		public void InitWithPath(String modPath)
		{
			DoInit(Path.GetDirectoryName(modPath) + "\\");
		}

		public void Shutdown()
		{
			Logging.WriteLineAndConsole(string.Format("Shutting down plugin: {0} - {1}", Name, Version));

			foreach (Thread thread in m_processThreads)
				thread.Abort();

			m_processThread.Abort();
		}

		public void Update()
		{
			
		}

		#endregion

		#region IChatEventHandler Members

		/// <summary>
		/// Raised when a chat messages are received.
		/// NOTE: This is raised on a different thread
		/// </summary>
		/// <param name="obj"></param>
		public void OnChatReceived(ChatManager.ChatEvent obj)
		{
			if (obj.message[0] != '/')
				return;

			HandleChatMessage(obj.sourceUserId, obj.message);
		}

		public void HandleChatMessage(ulong steamId, string message)
		{
			// Parse chat message
			ulong remoteUserId = steamId;
			string[] commandParts = message.Split(' ');
			int paramCount = commandParts.Length - 1;

			// User wants some help
			if (commandParts[0].ToLower() == "/help")
			{
				HandleHelpCommand(remoteUserId, commandParts);
				return;
			}

			// See if we have any valid handlers
			bool handled = false;
			foreach (ChatHandlerBase chatHandler in m_chatHandlers)
			{
				int commandCount = 0;
				if (remoteUserId == 0 && !chatHandler.AllowedInConsole())
					continue;

				if (chatHandler.CanHandle(remoteUserId, commandParts.ToArray(), ref commandCount))
				{
					try
					{
						chatHandler.HandleCommand(remoteUserId, commandParts.Skip(commandCount).ToArray());
					}
					catch (Exception ex)
					{
						Logging.WriteLineAndConsole(string.Format("ChatHandler Error: {0}", ex.ToString()));
					}

					handled = true;
				}
			}

			if (!handled)
			{
				DisplayAvailableCommands(remoteUserId, message);
			}
		}

		/// <summary>
		/// This function displays available help for all the functionality of this plugin
		/// </summary>
		/// <param name="remoteUserId"></param>
		/// <param name="commandParts"></param>
		private void HandleHelpCommand(ulong remoteUserId, string[] commandParts)
		{
			if (commandParts.Count() == 1)
			{
				List<string> commands = new List<string>();
				foreach (ChatHandlerBase handler in m_chatHandlers)
				{
					string commandBase = handler.GetCommandText().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).First();
					if (!commands.Contains(commandBase) && (!handler.IsClientOnly()) && (!handler.IsAdminCommand() || (handler.IsAdminCommand() && (PlayerManager.Instance.IsUserAdmin(remoteUserId) || remoteUserId == 0))))
					{
						commands.Add(commandBase);
					}
				}

				string commandList = string.Join(", ", commands);
				string info = string.Format("Dedicated Server Essentials v{0}.  Available Commands: {1}", Version, commandList);
				Communication.SendPrivateInformation(remoteUserId, info);
			}
			else
			{
				string helpTarget = string.Join(" ", commandParts.Skip(1).ToArray());
				bool found = false;
				foreach (ChatHandlerBase handler in m_chatHandlers)
				{
					if (handler.GetCommandText().ToLower() == helpTarget.ToLower())
					{
						Communication.SendPrivateInformation(remoteUserId, handler.GetHelp());
						found = true;
					}
				}

				if(!found)
				{
					List<string> helpTopics = new List<string>();

					foreach (ChatHandlerBase handler in m_chatHandlers)
					{
						if (handler.GetCommandText().ToLower().StartsWith(helpTarget.ToLower()) && ((!handler.IsAdminCommand()) || (handler.IsAdminCommand() && (PlayerManager.Instance.IsUserAdmin(remoteUserId) || remoteUserId == 0))))
						{
							helpTopics.Add(handler.GetCommandText().ToLower().Replace(helpTarget.ToLower(), ""));
						}
					}

					if (helpTopics.Count() > 0)
					{
						Communication.SendPrivateInformation(remoteUserId, string.Format("Help topics for command '{0}': {1}", helpTarget.ToLower(), string.Join(",", helpTopics.ToArray())));
						found = true;
					}
				}

				if (!found)
					Communication.SendPrivateInformation(remoteUserId, "Unknown command");
			}
		}

		/// <summary>
		/// Displays the available commands for the command entered
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="remoteUserId"></param>
		private void DisplayAvailableCommands(ulong remoteUserId, string recvMessage)
		{
			string message = recvMessage.ToLower().Trim();
			List<string> availableCommands = new List<string>();
			foreach (ChatHandlerBase chatHandler in m_chatHandlers)
			{
				string command = chatHandler.GetCommandText();
				if (command.StartsWith(message))
				{
					string[] cmdPart = command.Replace(message, "").Trim().Split(new char[] { ' ' });

					if (!availableCommands.Contains(cmdPart[0]))
						availableCommands.Add(cmdPart[0]);
				}
			}

			if (availableCommands.Count() > 0)
			{
				Communication.SendPrivateInformation(remoteUserId, string.Format("Available subcommands for '{0}' command: {1}", message, string.Join(", ", availableCommands.ToArray())));
			}
		}

		public void OnChatSent(ChatManager.ChatEvent obj)
		{

		}

		#endregion

		#region ICubeGridHandler Members

		public void OnCubeGridCreated(CubeGridEntity cubeGrid)
		{

		}

		public void OnCubeGridDeleted(CubeGridEntity cubeGrid)
		{

		}

		public void OnCubeGridLoaded(CubeGridEntity cubeGrid)
		{

		}

		public void OnCubeGridMoved(CubeGridEntity cubeGrid)
		{

		}

		#endregion

		#region ICubeBlockEventHandler Members

		public void OnCubeBlockCreated(CubeBlockEntity entity)
		{

		}

		public void OnCubeBlockDeleted(CubeBlockEntity entity)
		{
		}

		#endregion

		#region IPlayerEventHandler Members

		public void OnPlayerJoined(ulong remoteUserId)
		{
			foreach (ProcessHandlerBase handler in m_processHandlers)
				handler.OnPlayerJoined(remoteUserId);
		}

		public void OnPlayerLeft(ulong remoteUserId)
		{
			foreach (ProcessHandlerBase handler in m_processHandlers)
				handler.OnPlayerLeft(remoteUserId);
		}

		#endregion

		#region IPlugin Members

		public Guid Id
		{
			get
			{
				GuidAttribute guidAttr = (GuidAttribute)typeof(Essentials).Assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
				return new Guid(guidAttr.Value);
			}
		}

		public string Name
		{
			get
			{
				return "Dedicated Server Essentials";
			}
		}

		public string Version
		{
			get 
			{
				return typeof(Essentials).Assembly.GetName().Version.ToString();
			}
		}

		#endregion
	}
}
