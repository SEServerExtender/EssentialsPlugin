using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminScanNoPower : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to scan all grids that do not have power.  Takes into account if a grid is connected to other grids.  Usage: /admin nobeacon [scan]|[delete]";
		}
		public override string GetCommandText()
		{
			return "/admin scan nobeacon";
		}

		public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		// admin nobeacon scan
		public override bool HandleCommand(ulong userId, string[] words)
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			int count = 0;
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);

				foreach (IMyEntity entity in entities)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					MyObjectBuilder_CubeGrid cubeGrid = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder();


				}

				/*
				for(int r = entitiesToDelete.Count - 1; r >= 0; r--)
				{
					IMyEntity entity = entitiesToDelete[r];
					MyAPIGateway.Entities.RemoveEntity(entity);
				}
				 */ 

			});

			Communication.SendPrivateInformation(userId, string.Format("Found {0} grids with no beacons", count));
			//Communication.SendPrivateInformation(userId, string.Format("Removed {0} grids with no beacons", entitiesToDelete.Count));

			return true;
		}
	}
}
