using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing.Design;

using EssentialsPlugin.UtilityClasses;

namespace EssentialsPlugin.Settings
{
	public class SettingsCleanupTimedItem
	{
		public DateTime Restart;
		public List<SettingsCleanupNotificationItem> NotificationItemsRan = new List<SettingsCleanupNotificationItem>();
		public DateTime LastRan = DateTime.Now.AddDays(-1);

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

		private string scanCommand;
		public string ScanCommand
		{
			get { return scanCommand; }
			set { scanCommand = value; }
		}

		private string reason;
		public string Reason
		{
			get { return reason; }
			set { reason = value; }
		}
	}
}
