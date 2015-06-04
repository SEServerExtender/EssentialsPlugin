namespace EssentialsPlugin.Settings
{
	using System;
	using System.ComponentModel;
	using System.Drawing.Design;
	using EssentialsPlugin.UtilityClasses;

	[Serializable]
	public class RestartTimeItem
	{
		public DateTime Restart;

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

		public RestartTimeItem()
		{
			enabled = false;
			restartTime = DateTime.Now.AddHours(1).ToString("HH:mm");
			Restart = DateTime.Now.AddHours(1);
		}
	}
}
