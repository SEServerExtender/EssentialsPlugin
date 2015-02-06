namespace EssentialsPlugin.Utility
{
	using System;
	using VRageMath;

	[Serializable]
	public class DockingItem
	{
		private long playerId;
		public long PlayerId
		{
			get { return playerId; }
			set { playerId = value; }
		}

		private long dockedEntityId;
		public long DockedEntityId
		{
			get { return dockedEntityId; }
			set { dockedEntityId = value; }
		}

		private long targetEntityId;
		public long TargetEntityId
		{
			get { return targetEntityId; }
			set { targetEntityId = value; }
		}

		private long[] dockingBeaconIds;
		public long[] DockingBeaconIds
		{
			get { return dockingBeaconIds; }
			set { dockingBeaconIds = value; }
		}

		private Vector3 savePos;
		public Vector3 SavePos
		{
			get { return savePos; }
			set { savePos = value; }
		}

		private Quaternion saveQuat;
		public Quaternion SaveQuat
		{
			get { return saveQuat; }
			set { saveQuat = value; }
		}

		private String dockedName;
		public String DockedName
		{
			get { return dockedName; }
			set { dockedName = value; }
		}
	}
}