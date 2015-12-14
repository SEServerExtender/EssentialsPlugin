namespace EssentialsPlugin.ChatHandlers.AdminDelete
{
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using VRage.ModAPI;

	public class HandleAdminDeleteNoBeacon : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to delete all stations from an area defined by x, y, z, and radius.  Usage: /admin delete ships area [X] [Y] [Z] [RADIUS]";
		}

		public override string GetCommandText()
		{
			return "/admin delete nobeacon";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\""+GetHelp()+"\" \"close\" ";
            return longMessage;
        }

        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		// admin deletearea x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			HashSet<IMyEntity> entitiesToConfirm = new HashSet<IMyEntity>();
			HashSet<IMyEntity> entitiesConnected = new HashSet<IMyEntity>();
			HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
			});

			foreach (IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);
				if (gridBuilder == null)
					continue;

				bool found = false;
				foreach (MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
				{
					if (block.TypeId == typeof(MyObjectBuilder_Beacon))
					{
						found = true;
						break;
					}
				}

				if (!found)
					entitiesToConfirm.Add(grid);
			}

			CubeGrids.GetGridsUnconnected(entitiesFound, entitiesToConfirm);

			foreach (IMyEntity entity in entitiesFound)
			{
				CubeGridEntity gridEntity = (CubeGridEntity)GameEntityManager.GetEntity(entity.EntityId);
                if (gridEntity == null)
                {
                    Log.Info("A found entity gridEntity was null!");
                    continue;
                }
				Communication.SendPrivateInformation(userId, string.Format("Found entity '{0}' ({1}) at {2} with no beacon.", gridEntity.Name, entity.EntityId, General.Vector3DToString(entity.GetPosition())));
			}

			for(int r = entitiesFound.Count - 1; r >= 0; r--)
			{
				//MyAPIGateway.Entities.RemoveEntity(entity);
				IMyEntity entity = entitiesFound.ElementAt(r);
				CubeGridEntity gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
				gridEntity.Dispose();
			}

			Communication.SendPrivateInformation(userId, string.Format("Removed {0} grids with no beacons", entitiesFound.Count));

			return true;
		}
	}
}
