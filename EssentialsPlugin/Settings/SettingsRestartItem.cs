using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Common.Utils;
using System.Windows.Forms.Design;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Reflection;

using EssentialsPlugin.Settings;
using EssentialsPlugin.UtilityClasses;

namespace EssentialsPlugin.Settings
{
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
