namespace EssentialsPlugin.ChatHandlers.AdminDelete
{
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.Game.Entities;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity;
	using VRage.Game;
	using VRage.Game.Entity;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;

	public class HandleAdminDeleteGrids : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to delete grids that meet criteria.  Usage: /admin delete grids";
		}
		public override string GetCommandText()
		{
			return "/admin delete grids";
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
			HashSet<MyEntity> grids = CubeGrids.ScanGrids(userId, words);

			bool confirm = true;
			/*
			if (words.FirstOrDefault(x => x.ToLower() == "confirm") != null)
			{
				confirm = true;
			}
			*/
			int count = 0;
			foreach (MyEntity entity in grids)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				MyCubeGrid grid = (MyCubeGrid)entity;

				if(confirm)
					BaseEntityNetworkManager.BroadcastRemoveEntity(entity, true);

				long ownerId = 0;
				string ownerName = "";
				if (grid.BigOwners.Count > 0)
				{
					ownerId = grid.BigOwners.First();
					ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
				}

				if(confirm)
					Log.Info( "Cleanup Removed Grid - Id: {0} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName, ownerId, ownerName );

				count++;
			}

			Communication.SendPrivateInformation(userId, string.Format("Operation deletes {0} grids", count));
			return true;
		}
	}
}
