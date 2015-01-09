using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

using Sandbox.ModAPI;

using SEModAPIExtensions.API;

using EssentialsPlugin.Settings;
using EssentialsPlugin.Utility;

namespace EssentialsPlugin.ProcessHandler
{
	class ProcessCleanup : ProcessHandlerBase
	{
		private SettingsCleanupTriggerItem triggerdItem = null;
		private DateTime m_start;

		public ProcessCleanup()
		{
			m_start = DateTime.Now;
		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			try
			{
				if(!PluginSettings.Instance.CleanupEnabled)
					return;

				foreach (SettingsCleanupTimedItem item in PluginSettings.Instance.CleanupTimedItems)
				{
					if (!item.Enabled)
						continue;

					ProcessTimedItem(item);
				}

				foreach (SettingsCleanupTriggerItem item in PluginSettings.Instance.CleanupTriggerItems)
				{
					if (!item.Enabled)
						continue;

					ProcessTriggerItem(item);
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("ProcessCleanup.Handle(): {0}", ex.ToString()));
			} 
			
			base.Handle();
		}

		private void ProcessTimedItem(SettingsCleanupTimedItem item)
		{
			DateTime time = new DateTime(m_start.Year, m_start.Month, m_start.Day, item.Restart.Hour, item.Restart.Minute, 0);
			if (time - DateTime.Now < TimeSpan.Zero)
				return;

			if (DateTime.Now - item.LastRan < TimeSpan.FromMinutes(1))
				return;

			if (time - DateTime.Now < TimeSpan.FromSeconds(1) && DateTime.Now - item.LastRan > TimeSpan.FromMinutes(1))
			{
				string command = item.ScanCommand + " quiet";
				HashSet<IMyEntity> entities = CubeGrids.ScanGrids(0, command.Split(new char[] { ' ' }));
				CubeGrids.DeleteGrids(entities);
				Communication.SendPublicInformation(string.Format("[NOTICE]: Timed cleanup has run.  {0} entities removed.  Have a nice day.", entities.Count));
				item.LastRan = DateTime.Now;
				item.NotificationItemsRan.Clear();
				return;
			}

			foreach (SettingsCleanupNotificationItem notifyItem in PluginSettings.Instance.CleanupNotificationItems)
			{
				if (item.NotificationItemsRan.Contains(notifyItem))
					continue;

				if (time - DateTime.Now < TimeSpan.FromMinutes(notifyItem.MinutesBeforeCleanup))
				{
					item.NotificationItemsRan.Add(notifyItem);

					if (DateTime.Now - notifyItem.lastRan > TimeSpan.FromMinutes(1))
					{
						notifyItem.lastRan = DateTime.Now;
						string notification = notifyItem.Message.Replace("%cleanup_reason%", item.Reason);
						Communication.SendPublicInformation(notification);						
					}

				}
			}
		}

		private void ProcessTriggerItem(SettingsCleanupTriggerItem item)
		{
			if (triggerdItem != null && triggerdItem != item)
				return;

			if (triggerdItem == null)
			{
				// Increase to 5 minutes
				if (DateTime.Now - item.LastRan > TimeSpan.FromSeconds(300))
				{
					item.LastRan = DateTime.Now;
					string command = item.ScanCommand + " quiet";
					HashSet<IMyEntity> entities = CubeGrids.ScanGrids(0, command.Split(new char[] { ' ' }));
					if (entities.Count >= item.MaxCapacity)
					{
						Communication.SendPublicInformation(string.Format("[NOTICE]: Cleanup triggered.  ({0} of {1}) triggered grids found.  Cleanup will run in {2} minutes.  Reason: {3}", entities.Count, item.MaxCapacity, item.MinutesAfterCapacity, item.Reason));
						item.NotificationItemsRan.Clear();
						triggerdItem = item;
						return;
					}
				}
			}
			else
			{
				if (DateTime.Now - item.LastRan > TimeSpan.FromMinutes(item.MinutesAfterCapacity))
				{
					string command = item.ScanCommand + " quiet";
					HashSet<IMyEntity> entities = CubeGrids.ScanGrids(0, command.Split(new char[] { ' ' }));
					CubeGrids.DeleteGrids(entities);
					Communication.SendPublicInformation(string.Format("[NOTICE]: Triggered cleanup has run.  {0} entities removed.  Have a nice day.", entities.Count));
					triggerdItem = null;
					return;
				}

				foreach (SettingsCleanupNotificationItem notifyItem in PluginSettings.Instance.CleanupNotificationItems)
				{
					if (item.NotificationItemsRan.Contains(notifyItem))
						continue;

					if (notifyItem.MinutesBeforeCleanup > item.MinutesAfterCapacity)
						continue;

					if (DateTime.Now - item.LastRan > TimeSpan.FromMinutes(item.MinutesAfterCapacity - notifyItem.MinutesBeforeCleanup))
					{
						item.NotificationItemsRan.Add(notifyItem);
						string notification = notifyItem.Message.Replace("%cleanup_reason%", item.Reason);
						Communication.SendPublicInformation(notification);						
					}
				}
			}
		}
	}
}
