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
	public class ProtectedItem
	{
		private bool enabled;
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		private long entityId;
		public long EntityId
		{
			get { return entityId; }
			set { entityId = value; }
		}

		private int integrityIncrease;
		public int IntegrityIncrease
		{
			get { return integrityIncrease; }
			set 
			{
				integrityIncrease = Math.Max(value, 1);
			}
		}
	}
}
