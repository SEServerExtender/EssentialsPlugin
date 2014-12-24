using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssentialsPlugin.Settings
{
	[Serializable]
	public class ServerWaypointItem
	{
		private bool enabled;
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		private string name;
		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		private double x;
		public double X
		{
			get { return x; }
			set { x = value; }
		}

		private double y;
		public double Y
		{
			get { return y; }
			set { y = value; }
		}

		private double z;
		public double Z
		{
			get { return z; }
			set { z = value; }
		}
	}
}
