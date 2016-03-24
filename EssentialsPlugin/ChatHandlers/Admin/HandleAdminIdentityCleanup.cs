namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sandbox.Game.Multiplayer;
    using Sandbox.Game.World;
    using Sandbox.ModAPI;
    using Utility;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;

    public class HandleAdminIdentityCleanup : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "Cleans up trash identities. Usage: /admin identity cleanup (verbose)";
        }

        public override string GetCommandText( )
        {
            return "/admin identity cleanup";
        }

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem dialogItem = new Communication.ServerDialogItem
                                                        {
                                                            title = "Help",
                                                            header = "",
                                                            content = GetHelp( ),
                                                            buttonText = "close"
                                                        };
            return dialogItem;
        }

        public override bool IsAdminCommand( )
        {
            return true;
        }

        public override bool AllowedInConsole( )
        {
            return true;
        }

        public override bool HandleCommand( ulong userId, string[] words )
        {
            MyPlayerCollection playerCollection = MyAPIGateway.Players as MyPlayerCollection;
            bool verbose = words.Any( ) && words[0].ToLower( ) == "verbose";

            if ( playerCollection == null )
                return true;

            HashSet<long> owners = new HashSet<long>( );
            HashSet<MyIdentity> toRemove = new HashSet<MyIdentity>( );

            HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );

            Wrapper.GameAction( ( ) => MyAPIGateway.Entities.GetEntities( entities ) );

            foreach ( IMyEntity entity in entities )
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if ( grid == null )
                    continue;

                foreach ( long owner in grid.SmallOwners )
                    owners.Add( owner );
            }

            Dictionary<long, MyIdentity>.ValueCollection myIdentities = playerCollection.GetAllIdentities( );

            foreach ( MyIdentity identity in myIdentities )
            {
                if ( !identity.IsDead )
                    continue;

                if ( !owners.Contains( identity.IdentityId ) )
                    toRemove.Add( identity );
            }

            int count = toRemove.Count;
            int removedCount = 0;
            Essentials.Log.Info( count );

            foreach ( MyIdentity identity in toRemove )
            {
                //make extra sure the player isn't online
                if ( !identity.IsDead )
                    continue;

                //make sure the identity still exists
                if ( !playerCollection.HasIdentity( identity.IdentityId ) )
                    continue;

                removedCount++;
                if ( verbose )
                    Communication.SendPrivateInformation( userId, $"Removed identity {identity.DisplayName}: {identity.IdentityId}" );

                if ( DateTime.Now.Millisecond <10 )
                    Communication.SendPrivateInformation( userId, $"Removed {removedCount} of {count} identities." );


                Wrapper.GameAction( ( ) => playerCollection.RemoveIdentity( identity.IdentityId ) );
            }

            Communication.SendPrivateInformation( userId, $"Removed {count} identities." );

            return true;
        }
    }
}

