namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EssentialsPlugin.Utility;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using VRage.ModAPI;
    using EntityManagers;
    using Sandbox.Game.Entities;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;

    public class HandleAdminConceal : ChatHandlerBase
	{
		private Random m_random = new Random();
		public override string GetHelp()
		{
			return "This command gives you a list of concealed grids.  Usage: /admin conceal (force) - Using 'force' forces Essentials to check grids to conceal immediately.";
		}

		public override string GetCommandText()
		{
			return "/admin conceal";
		}

        public override Communication.ServerDialogItem GetHelpDialog()
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "Admin Conceal";
            DialogItem.content=
                "This command gives you a list of concealed grids.|" +
                "Use the force argument to force Essentials to process grids to conceal immediately.||" +
                "Usage: /admin conceal <force>";
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

		public override bool HandleCommand(ulong userId, string[] words)
		{
			bool showConcealed = true;
			if (words.Length > 0 && words[0].ToLower() == "revealed")
				showConcealed = false;

            if ( words.Length > 0 && words[0].ToLower( ) == "force" )
            {
                EntityManagement.CheckAndConcealEntities( );
                return true;
            }

            /*
            if ( words.Length > 0 && words[0].ToLower( ) == "dialog" )
            {
                //TODO: present entity list in a dialog
                return true;
            }
            */

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
				//CubeGrids.GetGridsUnconnected(entitiesFound, entities);
				int count = 0;

				foreach (IMyEntity entity in entitiesFound)
				{
					if (!(entity is MyCubeGrid))
						continue;

					if (!entity.InScene)
						continue;

					MyCubeGrid grid = (MyCubeGrid)entity;
					long ownerId = 0;
					string ownerName = "";
					if (grid.BigOwners.Count > 0)
					{
						ownerId = grid.BigOwners.First();
						ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
					}

					if (ownerName == "")
						ownerName = "No one";
				
					Communication.SendPrivateInformation(userId, string.Format("Id: {0} Display: {1} OwnerId: {2} OwnerName: {3} Position: {4} BlockCount: {5}", entity.EntityId, entity.DisplayName, ownerId, ownerName, General.Vector3DToString(entity.GetPosition()), grid.BlocksCount));
					count++;
				}

				Communication.SendPrivateInformation(userId, string.Format("Total unconnected revealed entities: {0}", count));

				Communication.SendPrivateInformation(userId, "==== Connected Entities ===");
			    var groups = CubeGrids.GetGroups( GridLinkTypeEnum.Logical );
				//Console.WriteLine("Here: {0} : {1} {2}", connectedFound.Intersect(entitiesFound).Count(), entitiesFound.Count, connectedFound.Count);
				count = 0;

			    foreach ( var group in groups )
			    {
			        foreach ( MyCubeGrid grid in group )
			        {
			            MyEntity entity = (MyEntity)grid;

			            if ( entitiesFound.Contains( entity ) )
			                continue;

			            if ( !entity.InScene )
			                continue;

			            if ( group.Count < 2 )
			                continue;

			            long ownerId = 0;
			            string ownerName = "";
			            if ( grid.BigOwners.Count > 0 )
			            {
			                ownerId = grid.BigOwners.First( );
			                ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId( ownerId ).Name;
			            }

			            if ( ownerName == "" )
			                ownerName = "No one";

			            Communication.SendPrivateInformation( userId, $"Id: {entity.EntityId} Display: {entity.DisplayName} OwnerId: {ownerId} OwnerName: {ownerName} Position: {grid.PositionComp.GetPosition( )} BlockCount: {grid.BlocksCount} Connections: {@group.Count}" );
			            //Communication.SendPrivateInformation(userId, string.Format("Id: {0} Display: {1} OwnerId: {2} OwnerName: {3} Position: {4} BlockCount: {5} Connections: {6}", entity.EntityId, entity.DisplayName, ownerId, ownerName, General.Vector3DToString(entity.GetPosition()), slimBlocks.Count));
			            count++;
			        }
			    }

				Communication.SendPrivateInformation(userId, string.Format("Total connected revealed entities: {0}", count));

			}

			return true;
		}
	}
}
