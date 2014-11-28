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

namespace EssentialsPlugin.Settings
{
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
