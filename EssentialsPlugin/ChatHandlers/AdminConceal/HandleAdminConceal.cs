using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminConceal : ChatHandlerBase
	{
		private Random m_random = new Random();
		public override string GetHelp()
		{
			return "This command gives you a list of concealed grids.  Usage: /admin conceal";
		}

		public override string GetCommandText()
		{
			return "/admin conceal";
		}

		public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			bool showConcealed = true;
			if (words.Length > 0 && words[0].ToLower() == "revealed")
				showConcealed = false;

			if (showConcealed)
			{
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				Wrapper.GameAction(() =>
				{
					MyAPIGateway.Entities.GetEntities(entities);
				});

				Communication.SendPrivateInformation(userId, "==== Concealed Entities ===");
				int count = 0;
				foreach (IMyEntity entity in entities)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if (entity.InScene)
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					long ownerId = 0;
					string ownerName = "";
					if (grid.BigOwners.Count > 0)
					{
						ownerId = grid.BigOwners.First();
						ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
					}

					if (ownerName == "")
						ownerName = "No one";

					Communication.SendPrivateInformation(userId, string.Format("Id: {0} Display: {1} OwnerId: {2} OwnerName: {3} Position: {4}", entity.EntityId, entity.DisplayName, ownerId, ownerName, General.Vector3DToString(entity.GetPosition())));
					count++;
				}

				Communication.SendPrivateInformation(userId, string.Format("Total concealed entities: {0}", count));
			}
			else
			{
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				Wrapper.GameAction(() =>
				{
					MyAPIGateway.Entities.GetEntities(entities);
				});

				Communication.SendPrivateInformation(userId, "==== Revealed Entities ===");
				Communication.SendPrivateInformation(userId, "==== Unconnected Entities ===");
				HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>();
				CubeGrids.GetGridsUnconnected(entitiesFound, entities);
				int count = 0;
				List<IMySlimBlock> slimBlocks = new List<IMySlimBlock>();
				foreach (IMyEntity entity in entitiesFound)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if (!entity.InScene)
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					long ownerId = 0;
					string ownerName = "";
					if (grid.BigOwners.Count > 0)
					{
						ownerId = grid.BigOwners.First();
						ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
					}

					if (ownerName == "")
						ownerName = "No one";

					grid.GetBlocks(slimBlocks, null);				
					Communication.SendPrivateInformation(userId, string.Format("Id: {0} Display: {1} OwnerId: {2} OwnerName: {3} Position: {4} BlockCount: {5}", entity.EntityId, entity.DisplayName, ownerId, ownerName, General.Vector3DToString(entity.GetPosition()), slimBlocks.Count));
					slimBlocks.Clear();
					count++;
				}

				Communication.SendPrivateInformation(userId, string.Format("Total unconnected revealed entities: {0}", count));

				Communication.SendPrivateInformation(userId, "==== Connected Entities ===");
				HashSet<IMyEntity> connectedFound = new HashSet<IMyEntity>();
				CubeGrids.GetConnectedGrids(connectedFound);
				Console.WriteLine("Here: {0} : {1} {2}", connectedFound.Intersect(entitiesFound).Count(), entitiesFound.Count, connectedFound.Count);
				count = 0;
				slimBlocks.Clear();
				foreach (IMyEntity entity in connectedFound)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if (entitiesFound.Contains(entity))
						continue;

					if (!entity.InScene)
						continue;

					if (CubeGrids.GetRecursiveGridList((IMyCubeGrid)entity).Count < 2)
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					long ownerId = 0;
					string ownerName = "";
					if (grid.BigOwners.Count > 0)
					{
						ownerId = grid.BigOwners.First();
						ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
					}

					if (ownerName == "")
						ownerName = "No one";

					grid.GetBlocks(slimBlocks, null);
					Communication.SendPrivateInformation(userId, string.Format("Id: {0} Display: {1} OwnerId: {2} OwnerName: {3} Position: {4} BlockCount: {5} Connections: {6}", entity.EntityId, entity.DisplayName, ownerId, ownerName, General.Vector3DToString(entity.GetPosition()), slimBlocks.Count, CubeGrids.GetRecursiveGridList(grid).Count));
					//Communication.SendPrivateInformation(userId, string.Format("Id: {0} Display: {1} OwnerId: {2} OwnerName: {3} Position: {4} BlockCount: {5} Connections: {6}", entity.EntityId, entity.DisplayName, ownerId, ownerName, General.Vector3DToString(entity.GetPosition()), slimBlocks.Count));
					slimBlocks.Clear();
					count++;
				}

				Communication.SendPrivateInformation(userId, string.Format("Total connected revealed entities: {0}", count));

			}

			return true;
		}
	}
}
