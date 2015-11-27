namespace EssentialsPlugin.ChatHandlers.AdminConceal
{
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity;
	using VRage.ModAPI;
	using VRage.ObjectBuilders;

	public class HandleAdminReveal : ChatHandlerBase
	{
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
			bool force = words.FirstOrDefault(x => x.ToLower() == "force") != null;

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() => MyAPIGateway.Entities.GetEntities(entities) );

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

					MyObjectBuilder_CubeGrid builder = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entity);
					if (builder == null)
						continue;

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
                    //Log.Info("Conceal", string.Format("Force Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, builder.EntityId));
                    Log.Info("Revealing");
					/*
					entity.InScene = true;
					entity.CastShadows = true;
					entity.Visible = true;
					*/
				
					//CubeGridEntity newEntity = new CubeGridEntity(builder);
					//SectorObjectManager.Instance.AddEntity(newEntity);

					//BaseEntityNetworkManager.BroadcastRemoveEntity(entity);
					MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
					addList.Add(builder);
					//MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
					addList.Clear();
				}
			});

			Log.Info( !force ? string.Format( "Command would Reveal {0} grids.  Type /admin reveal force to reveal them.", count ) : string.Format( "Command Revealed {0} grids", count ) );

			return true;
		}
	}
}
