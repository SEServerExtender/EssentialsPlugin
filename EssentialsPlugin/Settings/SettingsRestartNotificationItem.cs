using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssentialsPlugin.Settings
{
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
