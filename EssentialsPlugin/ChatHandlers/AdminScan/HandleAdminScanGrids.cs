namespace EssentialsPlugin.ChatHandlers.AdminScan
{
    using System;
    using EssentialsPlugin.Utility;
    using Sandbox.Game.Entities.Cube;

    public class HandleAdminScanGrids : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to scan for grids that meet criteria.  Usage: /admin scan grids";
		}
		public override string GetCommandText()
		{
			return "/admin scan grids";
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
                CubeGrids.ScanGrids( userId, words );
            }
            catch ( Exception ex )
            {
                Log.Info( ex, "scan grid" );
                return false;
            }
			return true;
		}
	}
}
