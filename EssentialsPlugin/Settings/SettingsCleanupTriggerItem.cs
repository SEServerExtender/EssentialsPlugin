using System;
using System.Collections.Generic;

namespace EssentialsPlugin.Settings
{
	public class SettingsCleanupTriggerItem
	{
		public DateTime LastRan = DateTime.Now;
		public List<SettingsCleanupNotificationItem> NotificationItemsRan = new List<SettingsCleanupNotificationItem>();

		private bool enabled;
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		private string scanCommand;
		public string ScanCommand
		{
			get { return scanCommand; }
			set { scanCommand = value; }
		}

		private int maxCapacity;
		public int MaxCapacity
		{
			get { return maxCapacity; }
			set { maxCapacity = Math.Max(1, value); }
		}

		private int minutesAfterCapacity;
		public int MinutesAfterCapacity
		{
			get { return minutesAfterCapacity; }
			set { minutesAfterCapacity = Math.Max(1, value); }
		}

		private string reason;
		public string Reason
		{
			get { return reason; }
			set { reason = value; }
		}
	}
}
