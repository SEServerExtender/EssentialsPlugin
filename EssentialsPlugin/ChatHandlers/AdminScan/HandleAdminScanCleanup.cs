namespace EssentialsPlugin.ChatHandlers
{
	using EssentialsPlugin.Utility;

	public class HandleAdminScanCleanup : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to scan for grids that are considered trash.  Usage: /admin scan trash";
		}
		public override string GetCommandText()
		{
			return "/admin scan cleanup";
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
			CubeGrids.ScanCleanup(userId, words);
			return true;
		}
	}
}
