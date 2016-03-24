namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sandbox.Game.Multiplayer;
    using Sandbox.Game.World;
    using Sandbox.ModAPI;
    using Utility;

    public class HandleAdminFactionCleanup : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "Cleans up factions with 1 or fewer members. Usage: /admin faction cleanup (verbose)";
        }

        public override string GetCommandText( )
        {
            return "/admin faction cleanup";
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
            Essentials.Log.Info( "Faction cleanup" );
            MyFactionCollection factionCollection = MySession.Static.Factions;
            if ( factionCollection == null )
            {
                Essentials.Log.Info( "Fail" );
                return true;
            }
            HashSet<MyFaction> toDelete = new HashSet<MyFaction>( );
            bool verbose = words.Any( ) && words[0].ToLower( ) == "verbose";

            foreach ( KeyValuePair<long, MyFaction> item in factionCollection )
            {
                MyFaction faction = item.Value;
                if ( faction == null )
                    continue;

                //get factions with one or fewer members
                if ( faction.Members.Count( ) <= 1 )
                {
                    MyPlayer.PlayerId playerId;

                    //check if the member is a valid player
                    MyPlayer member;
                    if ( !MySession.Static.Players.TryGetPlayerId( faction.Members.First( ).Value.PlayerId, out playerId ) )
                    {
                        toDelete.Add( faction );
                        continue;
                    }
                    //check if the player is online
                    //I'm not sure what happens if we delete a faction with a member logged in
                    //probably nothing, but maybe Clang
                    member = MySession.Static.Players.GetPlayerById( playerId );
                    if ( member==null || member.Identity.IsDead )
                        toDelete.Add( faction );
                }
            }
            
            Essentials.Log.Info( $"Found {toDelete.Count} factions to delete." );
            if(verbose)
                Communication.SendPrivateInformation( userId, $"Found {toDelete.Count} factions to delete." );
            int count = 0;
            foreach ( MyFaction faction in toDelete )
            {
                if ( faction == null )
                    continue;

                //make sure the faction still exists
                if ( !factionCollection.Contains( faction.FactionId ) )
                    continue;

                //NPC factions
                if ( faction.IsEveryoneNpc(  ) )
                    continue;

                count++;
                if ( verbose )
                    Communication.SendPrivateInformation( userId, $"Removing faction {faction.Tag}: {faction.Name}" );

                if(DateTime.Now.Millisecond<10)
                    Communication.SendPrivateInformation( userId, $"Removed {count} of {toDelete.Count} factions." );
                
                //delete the faction
                Wrapper.GameAction( ( ) => MyFactionCollection.RemoveFaction( faction.FactionId ) );
            }

            Communication.SendPrivateInformation( userId, $"Removed {count} factions with one or fewer members." );

            return true;
        }
    }
}

