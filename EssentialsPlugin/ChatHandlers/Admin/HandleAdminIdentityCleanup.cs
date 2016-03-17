namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System.Collections.Generic;
    using System.Linq;
    using Sandbox.Game.Multiplayer;
    using Sandbox.Game.World;
    using Sandbox.ModAPI;
    using Utility;
    public class HandleAdminIdentityCleanup : ChatHandlerBase
	{

    public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin identity cleanup";
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
        {/*
            MyPlayerCollection playerCollection = MyAPIGateway.Players as MyPlayerCollection;

            if ( playerCollection == null )
                return true;

            var identities = playerCollection.GetAllIdentities( ).GroupBy( x => x.DisplayName ).Where( y => y.Count( ) > 1 ).ToList( );

            while ( identities.Count > 0 )
            {
                string compareName = identities[0].Key;
                var toDelete = identities.Where(x => x.  )
            }

            int count = identitiesToDelete.Count;
            foreach(MyIdentity toDelete in identitiesToDelete)
            {
                Essentials.Log.Info( $"Deleted dead identity {toDelete.DisplayName}" );
              playerCollection.RemoveIdentity( toDelete.IdentityId );
            }

            if ( count != 0 )
            {
                Essentials.Log.Info( $"Deleted {count} dead identities." );
            }
            */
            return true;
        }

	}

}

