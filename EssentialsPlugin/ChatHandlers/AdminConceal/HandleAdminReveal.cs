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
	public class HandleAdminReveal : ChatHandlerBase
	{
		private Random m_random = new Random();
		public override string GetHelp()
		{
			return "This command allows you to reveal concealed grids.  Usage: /admin reveal (force) - This command without 'force' only shows you how many would be revealed.";
		}

		public override string GetCommandText()
		{
			return "/admin reveal";
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
			bool force = false;
			if (words.FirstOrDefault(x => x.ToLower() == "force") != null)
				force = true;

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities);
			});

			List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>();
			int count = 0;
			Wrapper.GameAction(() =>
			{
				foreach (IMyEntity entity in entities)
				{
					if (entity.InScene)
						continue;

					if (!(entity is IMyCubeGrid))
						continue;

					MyObjectBuilder_CubeGrid builder = null;
					try
					{
						builder = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(true);
					}
					catch
					{
						continue;
					}

					count++;
					if (!force)
					{
						continue;
					}

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					long ownerId = 0;
					string ownerName = "";
					if (grid.BigOwners.Count > 0)
					{
						ownerId = grid.BigOwners.First();
						ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
					}

					grid.PersistentFlags = (MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.CastShadows);
					grid.InScene = true;
					grid.CastShadows = true;
					builder.PersistentFlags = (MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.CastShadows);
					MyAPIGateway.Entities.RemapObjectBuilder(builder);
					Logging.WriteLineAndConsole("Conceal", string.Format("Force Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, builder.EntityId));

					/*
					entity.InScene = true;
					entity.CastShadows = true;
					entity.Visible = true;
					*/
				
					//CubeGridEntity newEntity = new CubeGridEntity(builder);
					//SectorObjectManager.Instance.AddEntity(newEntity);

					BaseEntityNetworkManager.BroadcastRemoveEntity(entity);
					MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
					addList.Add(builder);
					MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
					addList.Clear();
				}
			});

			if (!force)
				Logging.WriteLineAndConsole(string.Format("Command would Reveal {0} grids.  Type /admin reveal force to reveal them.", count));
			else
				Logging.WriteLineAndConsole(string.Format("Command Revealed {0} grids", count));

			return true;
		}
	}
}
