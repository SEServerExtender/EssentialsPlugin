namespace EssentialsPlugin.Settings
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows.Forms;
	using System.Xml.Serialization;
	using VRage.Serialization;

    [Serializable]
    [XmlInclude(typeof(ProtectionSettings))]
	public class ProtectedItem
	{
	    public enum PunishmentEnum
	    {
            None,
	        Kick,
            Ban,
            Speed,
	    }

        public enum ProtectionModeEnum
        {
            BlockAdd,
            BlockRemove,
            BlockPaint,
            BlockOwn,
            BlockRename,
            GridRename,
            GridSetStation,
            GridSetDynamic,
            GridDelete,
        }

        [Serializable]
        public class ProtectionSettings
        {
            public bool Enabled = false;
            public bool AdminExempt;
            public bool BigOwnerExempt;
            public bool SmallOwnerExempt;
            public bool FactionExempt;
            public string[] ExemptSteamIds;
            public string[] ExemptGroupIds;
            public long[] Factions;
            public string PrivateWarningMessage;
            public string PublicWarningMessage;
            public bool BroadcastGPS;
            public PunishmentEnum PunishmentType;
            public double SpeedLimit;
            public double SpeedTime;
        }

        public SerializableDictionary<ProtectionModeEnum,ProtectionSettings> ProtectionSettingsDict = new SerializableDictionary<ProtectionModeEnum, ProtectionSettings>();
        
		private bool enabled = true;
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

        private bool logOnly;
        public bool LogOnly
        {
            get { return logOnly; }
            set { logOnly = value; }
        }

        private bool protectDamage = true;
        public bool ProtectDamage
        {
            get { return protectDamage; }
            set
            {protectDamage = value;}
        }
    }
}
