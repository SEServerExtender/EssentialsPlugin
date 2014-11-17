using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms.Design;
using System.Drawing.Design;

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

namespace EssentialsPlugin
{
	public class Essentials : IPlugin, IChatEventHandler, IPlayerEventHandler, ICubeGridHandler, ICubeBlockEventHandler
	{
		#region Private Fields
		private static string m_pluginPath;
		//private static ControlForm controlForm;
		private DateTime m_lastProcessUpdate;
		private Thread m_processThread;
		private List<ProcessHandlerBase> m_processHandlers;
		private List<ChatHandlerBase> m_chatHandlers;
		#endregion

		#region Properties
		public static string PluginPath
		{
			get { return m_pluginPath; }
			set { m_pluginPath = value; }
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

		#endregion

		#region Constructors and Initializers

		private void DoInit(string path)
		{			
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
			
			// Setup chat handlers
			m_chatHandlers = new List<ChatHandlerBase>();
			m_chatHandlers.Add(new HandleInfo());
			m_chatHandlers.Add(new HandleTimeleft());
			m_chatHandlers.Add(new HandlePos());

			m_chatHandlers.Add(new HandleAdminScanAreaAt());          //
			m_chatHandlers.Add(new HandleAdminScanAreaTowards());     //
			m_chatHandlers.Add(new HandleAdminScanNoBeacon());        //

			m_chatHandlers.Add(new HandleAdminMoveAreaToPosition());
			m_chatHandlers.Add(new HandleAdminMoveAreaTowards());
			m_chatHandlers.Add(new HandleAdminMovePlayerTo());
			m_chatHandlers.Add(new HandleAdminMovePlayerPosition());
			m_chatHandlers.Add(new HandleAdminMoveGridTo());

			m_chatHandlers.Add(new HandleAdminDeleteGridsArea());
			m_chatHandlers.Add(new HandleAdminDeleteShipsArea());
			m_chatHandlers.Add(new HandleAdminDeleteStationsArea());
			m_chatHandlers.Add(new HandleAdminDeleteNoBeacon());

			m_chatHandlers.Add(new HandleAdminOwnershipChange());

//			m_chatHandlers.Add(new HandleAdminTest());
//			m_chatHandlers.Add(new HandleAdmin());

			m_lastProcessUpdate = DateTime.Now;
			m_processThread = new Thread(new ThreadStart(PluginProcessing));
			m_processThread.Start();

			Logging.WriteLineAndConsole(string.Format("Plugin '{0}' initialzed. (Version: {1}  ID: {2})", Name, Version, Id));
		}

		#endregion

		#region Processing Loop
		private void PluginProcessing()
		{
			try
			{
				while (true)
				{
					if (DateTime.Now - m_lastProcessUpdate > TimeSpan.FromMilliseconds(100))
					{
						foreach (ProcessHandlerBase handler in m_processHandlers)
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
						}

						m_lastProcessUpdate = DateTime.Now;
					}
				}
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

			// Parse chat message
			ulong remoteUserId = obj.sourceUserId;
			string[] commandParts = obj.message.Split(' ');
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
					chatHandler.HandleCommand(remoteUserId, commandParts.Skip(commandCount).ToArray());
					handled = true;
				}
			}

			if (!handled)
			{
				DisplayAvailableCommands(obj, remoteUserId);
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
					if (!commands.Contains(commandBase) && (!handler.IsAdminCommand() || (handler.IsAdminCommand() && (PlayerManager.Instance.IsUserAdmin(remoteUserId) || remoteUserId == 0))))
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
		private void DisplayAvailableCommands(ChatManager.ChatEvent obj, ulong remoteUserId)
		{
			string message = obj.message.ToLower().Trim();
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
