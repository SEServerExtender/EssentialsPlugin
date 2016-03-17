namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using System.Collections.Generic;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using VRage.Game;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;

	public class HandleAdminScanNoBeacon : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to scan all grids that do not have beacons.  Takes into account if a grid is connected to other grids.  Usage: /admin scan nobeacon";
		}
		public override string GetCommandText()
		{
			return "/admin scan nobeacon";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "";
            DialogItem.content = GetHelp( );
            DialogItem.buttonText = "close";
            return DialogItem;
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
			try
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
						if(block.TypeId == typeof(MyObjectBuilder_Beacon))
						{
							found = true;
							break;
						}
					}

					if(!found)
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
                    Communication.SendPrivateInformation(userId, string.Format("Found entity '{0}' ({1}) at {2} with no beacon.", gridEntity.Name, gridEntity.EntityId, General.Vector3DToString(entity.GetPosition())));
				}

				Communication.SendPrivateInformation(userId, string.Format("Found {0} grids with no beacons", entitiesFound.Count));
			}
			catch (Exception ex)
			{
				Log.Info(string.Format("Scan error: {0}", ex.ToString()));
			}

			return true;
		}
	}
}
