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
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminScanTrash : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to scan for grids that are considered trash.  Usage: /admin scan trash";
		}
		public override string GetCommandText()
		{
			return "/admin scan trash";
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
			bool requiresFunctional = true;
			bool requiresTerminal = true;
			if (words.Count() > 0)
			{
				if(words.FirstOrDefault(x => x.ToLower() == "functional") != null)
					requiresFunctional = false;

				if (words.FirstOrDefault(x => x.ToLower() == "terminal") != null)
					requiresTerminal = false;
			}

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
			});

			HashSet<IMyEntity> entitiesToConfirm = new HashSet<IMyEntity>();
			HashSet<IMyEntity> entitiesUnconnected = new HashSet<IMyEntity>();
			HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>();
			foreach (IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				CubeGridEntity gridEntity = (CubeGridEntity)GameEntityManager.GetEntity(grid.EntityId);

				if (PluginSettings.Instance.LoginEntityWhitelist.Contains(entity.EntityId.ToString()))
					continue;

				if (grid.BigOwners.Count < 1)
				{
					entitiesToConfirm.Add(entity);
				}			
			}

			CubeGrids.GetBlocksUnconnected(entitiesUnconnected, entitiesToConfirm);
			foreach(IMyEntity entity in entitiesUnconnected)
			{
				MyObjectBuilder_CubeGrid grid = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder();
				bool found = false;
				foreach(MyObjectBuilder_CubeBlock block in grid.CubeBlocks)
				{
					if (requiresFunctional)
					{
						if (block is MyObjectBuilder_FunctionalBlock)
						{
							found = true;
							break;
						}
					}

					if (requiresTerminal)
					{
						if(block is MyObjectBuilder_TerminalBlock)
						{
							found = true;
							break;
						}
					}
				}

				if (!found)
					entitiesFound.Add(entity);
			}

			foreach(IMyEntity entity in entitiesFound)
			{
				Communication.SendPrivateInformation(userId, string.Format("Found grid '{0}' ({1}) which has no owner and is unconnected.  BlockCount={2}", entity.DisplayName, entity.EntityId, ((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder()).CubeBlocks.Count));
			}

			Communication.SendPrivateInformation(userId, string.Format("Found {0} grids considered to be trash", entitiesFound.Count));
			return true;
		}
	}
}
