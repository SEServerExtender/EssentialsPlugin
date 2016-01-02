namespace EssentialsPlugin.ChatHandlers.Admin
{
    using Utility;
    public class HandleAdminTest : ChatHandlerBase
	{

    public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin test";
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
            Communication.ServerMessageItem message = new Communication.ServerMessageItem( );
            message.From = "Server";
            message.Message = "Hello world2!";

            //string messageString = MyAPIGateway.Utilities.SerializeToXML<Communication.ServerMessageItem>( message );
            string messageString = "Hello world1!";
            byte[ ] data = new byte[messageString.Length];

            for ( int r = 0; r < messageString.Length; r++ )
            {
                data[r] = (byte)messageString[r];
            }

            Communication.SendDataMessage( userId, Communication.DataMessageType.Test, data );
            /*
            try
            {
                Essentials.Log.Info( "test" );
                IMyEntity entity = MyAPIGateway.Entities.GetEntityById( PlayerMap.Instance.GetPlayerEntityId( userId ) );
                Vector3D position = new Vector3D( 100000,100000,100000 );
                Wrapper.GameAction( ( ) =>
                {
                    entity.SetPosition( position );
                    MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( entity ) );
                } );
            }
            catch ( Exception ex )
            {
                Essentials.Log.Info( ex, "test fail" );
            }
           */

            return true;
        }

	}

}

