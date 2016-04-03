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
	    public override bool HandleCommand( ulong userId, string[] words )
	    {
	        HashSet<GridGroup> groups = CubeGrids.ScanGrids( userId, words );

	        bool confirm = true;
	        /*
			if (words.FirstOrDefault(x => x.ToLower() == "confirm") != null)
			{
				confirm = true;
			}
			*/
	        int gridCount = 0;
	        int groupCount = 0;
	        foreach (GridGroup group in groups)
            {
	                long ownerId = 0;
	                string ownerName = "";
	                if (group.BigOwners.Count > 0)
	                {
	                    ownerId = group.BigOwners.First( );
                    //TODO
	                    ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId( ownerId ).Name;
	                }

	                if (confirm)
	                    Log.Info( "Cleanup removed group with parent - Id: {0} Display: {1} OwnerId: {2} OwnerName: {3}",
	                              group.Parent.EntityId, group.Parent.DisplayName, ownerId, ownerName );

                gridCount += group.Grids.Count;
                groupCount++;

	                if (confirm)
	                    group.Close(  );
            }

	    Communication.SendPrivateInformation(userId, $"Operation deletes {gridCount} grids in {groupCount} groups." );
			return true;
		}
	}
}
