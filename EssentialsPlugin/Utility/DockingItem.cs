namespace EssentialsPlugin.Utility
{
	using System;
	using System.Runtime.Serialization;
	using VRageMath;

	[DataContract]
	[Serializable]
	public class DockingItem
	{
		private long _playerId;
		public long PlayerId
		{
			get { return _playerId; }
			set { _playerId = value; }
		}

		private long _dockedEntityId;
		public long DockedEntityId
		{
			get { return _dockedEntityId; }
			set { _dockedEntityId = value; }
		}

		private long _targetEntityId;
		public long TargetEntityId
		{
			get { return _targetEntityId; }
			set { _targetEntityId = value; }
		}

		private long[] _dockingBeaconIds;
		public long[] DockingBeaconIds
		{
			get { return _dockingBeaconIds; }
			set { _dockingBeaconIds = value; }
		}

		private Vector3 _savePos;
		public Vector3 SavePos
		{
			get { return _savePos; }
			set { _savePos = value; }
		}

		private Quaternion _saveQuat;
		public Quaternion SaveQuat
		{
			get { return _saveQuat; }
			set { _saveQuat = value; }
		}

		private string _dockedName;
		public string DockedName
		{
			get { return _dockedName; }
			set { _dockedName = value; }
		}
	}
}