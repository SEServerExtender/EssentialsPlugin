namespace EssentialsPlugin.ChatHandlers.Admin
{
    using Utility;
    public class HandleAdminSpawnCargo : ChatHandlerBase
	{

    public override string GetHelp()
		{
			return "Spawns a cargo ship.";
		}

		public override string GetCommandText()
		{
			return "/admin spawn cargo";
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

        public override bool HandleCommand( ulong userId, string[ ] words )
        {
            CargoShips.SpawnCargoShip( );
            return true;
        }

	}

}

