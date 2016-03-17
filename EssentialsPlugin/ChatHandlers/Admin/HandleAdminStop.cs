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
    using Sandbox.Common.ObjectBuilders;
    using VRageMath;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using VRage.Game.ModAPI;

    public class HandleAdminStop : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "This command allows you to stop objects in the world. Usage: /admin stop (all) (floating) (ships)";
        }
        public override string GetCommandText( )
        {
            return "/admin stop";
        }

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "Admin Stop";
            DialogItem.content = "This command allows you to stop objects in the world.||" +
                "You must use at least one argument in the command. available arguments are (all), (floating), (ships)|" +
                "\"all\" will stop all floating objects (ore, components, backpacks), as well as unpiloted ships.|" +
                "\"floating\" will stop only floating objects.|" +
                "\"ships\" will stop only unpiloted ships.";
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

        private static int count = 0;

        public override bool HandleCommand( ulong userId, string[ ] words )
        {
            bool arg_floating = false;
            bool arg_ships = false;
            bool arg_piloted = false;

            if ( words.Length < 1 )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }
            if ( words.Contains( "all" ) )
            {
                arg_ships = true;
                arg_floating = true;
            }
            if ( words.Contains( "floating" ) )
                arg_floating = true;
            if ( words.Contains( "ships" ) )
                arg_ships = true;
            //if ( words.Contains( "piloted" ) )
            //{
            //    arg_ships = true;
            //    arg_piloted = true;
            //}
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
            Wrapper.GameAction( ( ) =>
             {
                 MyAPIGateway.Entities.GetEntities( entities );
             } );

            foreach ( IMyEntity entity in entities )
            {
                bool found = false;
                if ( entity == null )
                    continue;

                if ( entity.Physics == null )
                    continue;

                //if ( !(entity is IMyCubeGrid) || !(entity is IMyFloatingObject) || !(entity is MyInventoryBagEntity) )
                //    continue;

                if ( arg_ships && entity is IMyCubeGrid )
                {
                    //TODO: find a way to stop piloted ships
                    //even if we try to stop the grid entity, it won't work if there's a pilot
                    //probably should be handled in the mod on client end.
                    /*
                    MyCubeGrid grid = (MyCubeGrid)entity;
                    foreach ( MySlimBlock block in grid.CubeBlocks )
                    {
                        if ( !(block.FatBlock is MyCockpit) )
                            continue;

                        if ( ((MyCockpit)block.FatBlock).Pilot != null )
                        {
                            found = true;
                            break;
                        }
                    }

                    if ( found && !arg_piloted )
                        continue;
                    */
                    Stop( entity.GetTopMostParent( ) );
                }
                else if ( arg_floating && (entity is IMyFloatingObject || entity is MyInventoryBagEntity) )
                    Stop( entity );
            }
            Communication.SendPrivateInformation( userId, count.ToString( ) + " entities have been stopped." );
            count = 0;
            return true;
        }

        private static void Stop( IMyEntity entity )
        {
            //don't bother with anything that isn't already moving
            if ( entity.Physics.LinearVelocity == Vector3D.Zero && entity.Physics.AngularVelocity == Vector3D.Zero )
                return;

            Wrapper.GameAction( ( ) =>
            {
                entity.Physics.LinearVelocity = Vector3D.Zero;
                entity.Physics.AngularVelocity = Vector3D.Zero;
            } );
            ++count;
        }

    }
}
