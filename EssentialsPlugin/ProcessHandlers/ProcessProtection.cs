namespace EssentialsPlugin.ProcessHandlers
{
	using System.Collections.Generic;
	using System.Threading;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;

	public class ProcessProtection : ProcessHandlerBase
	{
		public ProcessProtection()
		{
		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.ProtectedEnabled)
				return;

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities);
			});

			foreach(IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				foreach(ProtectedItem item in PluginSettings.Instance.ProtectedItems)
				{
					if (!item.Enabled)
						continue;

					if (entity.EntityId == item.EntityId)
					{
						ProtectedEntity(entity, item);
					}
				}
			}

			base.Handle();
		}

		private void ProtectedEntity(IMyEntity entity, ProtectedItem item)
		{
			//Log.Info(string.Format("Protecting: {0}", entity.EntityId));
			//CubeGridEntity gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
			CubeGridEntity gridEntity = (CubeGridEntity)GameEntityManager.GetEntity(entity.EntityId);
			MyObjectBuilder_CubeGrid grid = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder();

			int count = 0;
			while (gridEntity.IsLoading)
			{
				if (count >= 20)
					return;

				Thread.Sleep(100);
				count++;
			}

			bool found = false;
			/*
			foreach(CubeBlockEntity block in gridEntity.CubeBlocks)
			{
				if (block.IntegrityPercent != item.IntegrityIncrease || block.BuildPercent != item.IntegrityIncrease || block.BoneDamage > 0f)
				{
					found = true;
					block.FixBones(0, 100);
					block.IntegrityPercent = item.IntegrityIncrease;
					block.BuildPercent = item.IntegrityIncrease;
				}
			}
			*/
			if(found)
     			Log.Info(string.Format("Repaired Grid: {0}", gridEntity.EntityId));
		}
	}
}

