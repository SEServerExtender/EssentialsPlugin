namespace EssentialsPlugin.ChatHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using EssentialsPlugin.Utility;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Entity.Sector.SectorObject;
    using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
    using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
    using VRage.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;

    public class HandleAdminOwnershipChange : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "This command allows you to change the ownership of a ship.  Usage: /admin ownership change <entityId/entityName> <PlayerName>";
        }
        public override string GetCommandText( )
        {
            return "/admin ownership change";
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

        public override bool IsAdminCommand( )
        {
            return true;
        }

        public override bool AllowedInConsole( )
        {
            return true;
        }

        // /admin ownership change name gridId
        public override bool HandleCommand( ulong userId, string[ ] words )
        {
            if ( words.Length < 2 )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }

            string name = words[0].ToLower( );
            long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId( PlayerMap.Instance.GetSteamIdFromPlayerName( name, true ) ).First( );
            string gridId = words[1].ToLower( );
            long gridEntityId = 0;
            IMyCubeGrid grid = null;
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
            bool found = false;

            if ( !long.TryParse( gridId, out gridEntityId ) )
            {
                Wrapper.GameAction( ( ) =>
                {
                    MyAPIGateway.Entities.GetEntities( entities );
                } );
                foreach ( IMyEntity entity in entities )
                {
                    if ( entity == null )
                        continue;

                    if ( !(entity is IMyCubeGrid) )
                        continue;

                    if ( entity.DisplayName.ToLower( ) == gridId.ToLower( ) )
                    {
                        found = true;
                        grid = (IMyCubeGrid)entity;
                        break;
                    }
                }
                if ( !found )
                {
                    Communication.SendPrivateInformation( userId, string.Format( "Could not find entity with name {0}", gridId ) );
                    return true;
                }
            }

            if ( !found )
            {
                IMyEntity tmpEntity;

                if ( !MyAPIGateway.Entities.TryGetEntityById( gridEntityId, out tmpEntity ) )
                {
                    Communication.SendPrivateInformation( userId, string.Format( "Could not find entity with id {0}", gridId ) );
                    return true;
                }
                grid = (IMyCubeGrid)tmpEntity;
            }

            if ( grid == null )
            {
                Communication.SendPrivateInformation( userId, string.Format( "Could not find entity with id {0}.", gridId ) );
                return true;
            }
            Wrapper.GameAction( ( ) =>
                                {
                                    grid.ChangeGridOwnership( playerId, MyOwnershipShareModeEnum.Faction );
                                } );
            Communication.SendPrivateInformation( userId, string.Format( "Ownership changed to player {0}", name ) );
            return true;
        }
    }
}
