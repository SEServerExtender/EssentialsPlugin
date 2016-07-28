namespace EssentialsPlugin.Settings
{
	using System;

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

	    private bool protectBlockAdd;
	    public bool ProtectBockAdd
	    {
	        get { return protectBlockAdd; }
            set { protectBlockAdd = value; }
	    }

	    private string protectBlockWarning;
	    public string ProtectBlockWarning
	    {
	        get {return protectBlockWarning;}
            set { protectBlockWarning = value; }
	    }
	}
}
