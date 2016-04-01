namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using System.Collections.Generic;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.Game.Entities;
	using Sandbox.Game.Entities.Cube;
	using Sandbox.ModAPI;
	using Sandbox.ModAPI.Ingame;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using VRage.Game;
	using VRage.Game.Entity;
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
				HashSet<MyEntity> entitiesToConfirm = new HashSet<MyEntity>();
				HashSet<IMyEntity> entitiesConnected = new HashSet<IMyEntity>();
                HashSet<List<MyCubeGrid>> groupsToConfirm = new HashSet<List<MyCubeGrid>>();
				HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>();
				Wrapper.GameAction(() =>
				{
					MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
				});

				foreach (IMyEntity entity in entities)
				{
					if (!(entity is MyCubeGrid))
						continue;

					MyCubeGrid grid = (MyCubeGrid)entity;

					bool found = false;
					foreach (MySlimBlock slimBlock in grid.CubeBlocks)
					{
					    IMyCubeBlock block = slimBlock?.FatBlock;

						if(block is IMyBeacon)
						{
							found = true;
							break;
						}
					}

					if(!found)
						entitiesToConfirm.Add(grid);
				}

			    groupsToConfirm = CubeGrids.GetGroups( GridLinkTypeEnum.Logical, entitiesToConfirm );
				
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
