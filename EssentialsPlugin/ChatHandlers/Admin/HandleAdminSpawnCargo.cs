namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System;
    using Sandbox.Game.World;
    using Utility;
    using VRage.Game;

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
            if ( words.Length == 1 && words[0].ToLower( ) == "atmosphere" )
            {
                CargoShips.SpawnCargoShip( false );
                return true;
            }

            var cargoShipEvent = MyGlobalEvents.GetEventById(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), "SpawnCargoShip"));
            if (cargoShipEvent == null )
            {
                //we can't force the game to spawn a ship if the option is off, so use our own method
                CargoShips.SpawnCargoShip( true );
            }
            else
            {
                MyGlobalEvents.RemoveGlobalEvent(cargoShipEvent);
                cargoShipEvent.SetActivationTime(TimeSpan.Zero);
                MyGlobalEvents.AddGlobalEvent(cargoShipEvent);
            }
            return true;
        }

	}

}

