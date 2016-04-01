namespace EssentialsPlugin.ChatHandlers.AdminDelete
{
	using System.Collections.Generic;
	using System.Linq;
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

		// admin deletearea x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			HashSet<MyEntity> entities = new HashSet<MyEntity>();
			HashSet<MyEntity> entitiesToConfirm = new HashSet<MyEntity>();
			HashSet<MyEntity> entitiesConnected = new HashSet<MyEntity>();
			HashSet<MyEntity> entitiesFound = new HashSet<MyEntity>();
            HashSet<List<MyCubeGrid>> groupsFound = new HashSet<List<MyCubeGrid>>();

			Wrapper.GameAction(() =>entities = MyEntities.GetEntities(  ));

			foreach (MyEntity entity in entities)
			{
				if (!(entity is MyCubeGrid))
					continue;

				MyCubeGrid grid = (MyCubeGrid)entity;

				bool found = false;
				foreach (MySlimBlock block in grid.CubeBlocks)
				{
					if (block.FatBlock is IMyBeacon)
					{
						found = true;
						break;
					}
				}

				if (!found)
					entitiesToConfirm.Add(grid);
			}

			//CubeGrids.GetGridsUnconnected(entitiesFound, entitiesToConfirm);
		    groupsFound = CubeGrids.GetGroups( GridLinkTypeEnum.Logical, entitiesToConfirm );

		    foreach ( var group in groupsFound )
		    {
		        foreach ( MyCubeGrid grid in group )
		        {
		            Communication.SendPrivateInformation( userId, $"Found entity '{grid.DisplayName}' ({grid.EntityId}) at {grid.PositionComp.GetPosition( ).ToString( )} with no beacon." );
		        Wrapper.GameAction( ()=>grid.Close(  ) );
		        }
		    }
		    Communication.SendPrivateInformation(userId, $"Removed {entitiesFound.Count} grids with no beacons" );

			return true;
		}
	}
}
