namespace EssentialsPlugin.Settings
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Drawing.Design;
	using System.Windows.Forms;
	using EssentialsPlugin.UtilityClasses;

	public class SettingsCleanupTimedItem
	{
		public DateTime Restart;
		public List<SettingsCleanupNotificationItem> NotificationItemsRan = new List<SettingsCleanupNotificationItem>();
		public DateTime LastRan = DateTime.Now.AddDays(-1);
	    public bool Interval;

		private bool enabled;
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		private string restartTime;
        //HACK: we can't rename this because loading the saved config will fail and blank the property
        //use the DisplayName attribute instead
		[Editor(typeof(TimePickerEditor), typeof(UITypeEditor))]
        [DisplayName(@"RunTime")]
		public string RestartTime
		{
			get { return restartTime; }
			set
			{
				restartTime = value;
				//Restart = DateTime.Parse(restartTime);
			    if ( restartTime.StartsWith( "-" ) )
			    {
			        if ( restartTime[1] != '1' )
			            throw new FormatException( "Hours must be between -1 and 23" );

			        Interval = true;
			        Restart = DateTime.Parse( restartTime.TrimStart( '-' ) );
			    }
			    else
			    {
			        Interval = false;
			        Restart = DateTime.Parse( value );
			    }

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
