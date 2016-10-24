namespace EssentialsPlugin.Settings
{
	using System;
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
            BlockSettings,
        }

        [Serializable]
        public class ProtectionSettings
        {
            public bool AllExempt;
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
        
		private bool _enabled = true;
		public bool Enabled
		{
			get { return _enabled; }
			set { _enabled = value; }
		}

        private long _entityId;
        public long EntityId
		{
			get { return _entityId; }
			set { _entityId = value; }
		}

        private bool _logOnly;
        public bool LogOnly
        {
            get { return _logOnly; }
            set { _logOnly = value; }
        }

        private bool _protectDamage = true;
        public bool ProtectDamage
        {
            get { return _protectDamage; }
            set { _protectDamage = value; }
        }
    }
}
