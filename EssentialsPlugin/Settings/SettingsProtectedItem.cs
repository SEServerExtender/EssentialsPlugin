using System;

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
