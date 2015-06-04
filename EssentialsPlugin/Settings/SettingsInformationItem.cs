namespace EssentialsPlugin.Settings
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.Design;
	using System.Drawing.Design;

	[Serializable]
	public class InformationItem
	{
		internal DateTime lastUpdate = DateTime.Now;
		internal int position = 0;

		private bool enabled;
		public bool Enabled { get { return enabled; } set { enabled = value; } }

		private string subCommand;
		public string SubCommand { get { return subCommand; } set { subCommand = value; } }

		private string subText;
		[Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
		public string SubText { get { return subText; } set { subText = value; } }

		private int intervalSeconds;
		public int IntervalSeconds { get { return intervalSeconds; } set { intervalSeconds = value; } }
	}
}
