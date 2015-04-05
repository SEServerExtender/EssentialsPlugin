using System;

namespace EssentialsPlugin.Settings
{
	[Serializable]
	public class BackupItem
	{
		private int _hour;
		public int Hour
		{
			get { return _hour; }
			set
			{
				_hour = Math.Min(Math.Max(-1, value), 23);
			}
		}

		private int _minute;
		public int Minute
		{
			get { return _minute; }
			set
			{
				_minute = Math.Min(Math.Max(0, value), 59);
			}
		}

		private bool _enabled;
		public bool Enabled
		{
			get { return _enabled; }
			set { _enabled = value; }
		}
	}
}
