namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using VRage.ModAPI;

	public class HandleAdminScanOverlimit : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "T";
		}
		public override string GetCommandText()
		{
			return "/admin scan overlimit";
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
			if(words.Count() != 2)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			string blockType = words[0].ToLower();
			int blockCount = 0;
			if(!int.TryParse(words[1], out blockCount))
			{
				Communication.SendPrivateInformation(userId, string.Format("Invalid block count: {0}", words[1]));
				return true;
			}

			blockCount = Math.Max(blockCount, 1);

			Communication.SendPrivateInformation(userId, string.Format("Looking for grids that contain more than {0} of type {1}", blockCount, blockType));

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
				MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);
				if (gridBuilder == null)
					continue;

				if (PluginSettings.Instance.LoginEntityWhitelist.Contains(entity.EntityId.ToString()))
					continue;

				entitiesToConfirm.Add(entity);
			}

			CubeGrids.GetGridsUnconnected(entitiesUnconnected, entitiesToConfirm);
			foreach(IMyEntity entity in entitiesUnconnected)
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;
				MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entity);
				if (gridBuilder == null)
					continue;

				int count = 0;
				foreach(MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
				{
					if (block.GetId().SubtypeName.ToLower().Contains(blockType))
					{
						count++;						
					}
				}

				if (count >= blockCount)
				{
					Communication.SendPrivateInformation(userId, string.Format("Found grid '{0}' ({1}) owned by '{5}' which has more than {3} blocks of type {4}.  BlockCount={2}", entity.DisplayName, entity.EntityId, ((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder()).CubeBlocks.Count, blockCount, blockType, grid.BigOwners.Count > 0 ? PlayerMap.Instance.GetPlayerItemFromPlayerId(grid.BigOwners.First()).Name : "No one"));
					entitiesFound.Add(entity);
					continue;
				}
			}

			/*
			foreach(IMyEntity entity in entitiesFound)
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;
			}
			 */ 

			Communication.SendPrivateInformation(userId, string.Format("Found {0} grids considered to be overlimit", entitiesFound.Count));
			return true;
		}
	}
}
