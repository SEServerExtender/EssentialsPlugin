using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssentialsPlugin.Settings
{
	[Serializable]
	public class BackupItem
	{
		private int hour;
		public int Hour
		{
			get { return hour; }
			set
			{
				hour = Math.Min(Math.Max(-1, value), 23);
			}
		}

		private int minute;
		public int Minute
		{
			get { return minute; }
			set
			{
				minute = Math.Min(Math.Max(0, value), 59);
			}
		}

		private bool enabled;
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}
	}
}
