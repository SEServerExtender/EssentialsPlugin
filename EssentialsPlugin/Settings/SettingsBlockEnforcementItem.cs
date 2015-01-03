using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssentialsPlugin.Settings
{
	public class SettingsBlockEnforcementItem
	{
		private bool enabled;
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		private string blockType;
		public string BlockType
		{
			get { return blockType; }
			set { blockType = value; }
		}

		private int maxPerGrid;
		public int MaxPerGrid
		{
			get { return maxPerGrid; }
			set { maxPerGrid = value; }
		}

		private string maxReachWarning;
		public string MaxReachWarning
		{
			get { return maxReachWarning; }
			set { maxReachWarning = value; }
		}

		private string maxExceedWarning;
		public string MaxExceedWarning
		{
			get { return maxExceedWarning; }
			set { maxExceedWarning = value; }
		}
	}
}
