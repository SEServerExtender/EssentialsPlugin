using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssentialsPlugin.Utility;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRageMath;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using System.IO;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ProcessHandler
{
	public class ProcessCommunication : ProcessHandlerBase
	{
		#region Private Fields
		private Dictionary<ulong, long> m_commGrids;
		private Random m_random;
		private bool m_init;
		#endregion

		public ProcessCommunication()
		{
			m_commGrids = new Dictionary<ulong, long>();
			m_random = new Random();
		}

		public override int GetUpdateResolution()
		{
			return 250;
		}

		public override void Handle()
		{
			if (!m_init && MyAPIGateway.Entities != null)
			{
				m_init = true;
				CleanupRelays();
			}

			base.Handle();
		}

		// Events and Overrides

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			if (PlayerMap.Instance.GetPlayerIdsFromSteamId(remoteUserId).Count() < 1)
				return;

			CubeGridEntity entity = new CubeGridEntity(new FileInfo(Essentials.PluginPath + "CommRelay.sbc"));
			entity.EntityId = BaseEntity.GenerateEntityId();			
			entity.DisplayName = string.Format("CommRelay{0}", PlayerMap.Instance.GetPlayerIdsFromSteamId(remoteUserId).First());

			float halfExtent = MyAPIGateway.Entities.WorldSafeHalfExtent();
			if(halfExtent == 0f)
				halfExtent = 900000f;

			entity.PositionAndOrientation = new MyPositionAndOrientation(new Vector3(GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent)), Vector3.Forward, Vector3.Up);
			//entity.PositionAndOrientation = new MyPositionAndOrientation(new Vector3(100, 100, 100), Vector3.Forward, Vector3.Up);
			m_commGrids.Add(remoteUserId, entity.EntityId);
			SectorObjectManager.Instance.AddEntity(entity);

			/*
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(entity.Export());
			});
			*/

			base.OnPlayerJoined(remoteUserId);
		}

		private float GenerateRandomCoord(float halfExtent)
		{
			float result = (m_random.Next(200) + halfExtent) * (m_random.Next(2) == 0 ? -1 : 1);
			return result;
		}

		public override void OnPlayerLeft(ulong remoteUserId)
		{
			if (!m_commGrids.ContainsKey(remoteUserId))
				return;

			Wrapper.GameAction(() =>
			{
				IMyEntity entity;

				if (MyAPIGateway.Entities.TryGetEntityById(m_commGrids[remoteUserId], out entity))				
				{
					MyAPIGateway.Entities.RemoveEntity(entity);
				}
			});

			m_commGrids.Remove(remoteUserId);

			base.OnPlayerLeft(remoteUserId);
		}

		// Private functions
		private void CleanupRelays()
		{
			Wrapper.GameAction(() =>
			{
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
				List<IMyEntity> entitiesToRemove = new List<IMyEntity>();

				foreach (IMyEntity entity in entities)
				{
					if (entity.DisplayName.StartsWith("CommRelay"))
					{
						entitiesToRemove.Add(entity);
					}
				}

				for (int r = entitiesToRemove.Count - 1; r >= 0; r--)
				{
					IMyEntity entity = entitiesToRemove[r];
					MyAPIGateway.Entities.RemoveEntity(entity);
				}
			});
		}
	}
}

