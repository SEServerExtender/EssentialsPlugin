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
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities);
			});

			Communication.SendPrivateInformation(userId, "==== Concealed Entities ===");
			int count = 0;
			foreach(IMyEntity entity in entities)
			{
				if(!(entity is IMyCubeGrid))
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
			return true;
		}
	}
}
