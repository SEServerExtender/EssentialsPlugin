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
    public class HandleAdminStop : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "This command allows you to stop objects in the world. Usage: /admin stop (all) (floating) (ships) (piloted) (ship \"name\")";
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
                "You must use at least one argument in the command. available arguments are (all), (floating), (ships), (piloted), (ship \"name\")|" +
                "\"all\" will stop all floating objects (ore, components, backpacks), as well as unpiloted ships.|" +
                "\"floating\" will stop only floating objects.|" +
                "\"ships\" will stop only unpiloted ships.|" +
                "\"piloted\" can be combined with \"all\" or \"ships\" to include piloted ships.|" +
                "\"ship\" lets you stop an individual ship by its name. Be sure to enclose the name in quotation marks.";
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
            bool floating = false;
            bool ships = false;
            bool piloted = false;
            bool named = false;
            string shipName = "";

            if ( words.Length < 1 )
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return true;
            }
            for ( int i = 0; i < words.Length; ++i )
            {
                switch ( words[i] )
                {
                    case "all":
                        ships = true;
                        floating = true;
                        break;
                    case "floating":
                        floating = true;
                        break;
                    case "ships":
                        ships = true;
                        break;
                    case "piloted":
                        ships = true;
                        piloted = true;
                        break;
                    case "ship":
                        named = true;
                        shipName = words[i + 1];
                        break;
                    default:
                        Communication.SendPrivateInformation( userId, GetHelp( ) );
                        return true;
                }
            }
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

                if ( !(entity is IMyCubeGrid) || !(entity is IMyFloatingObject) || !(entity is MyInventoryBagEntity) )
                    continue;

                if ( ships && entity is IMyCubeGrid )
                {
                    MyCubeGrid grid = (MyCubeGrid)entity;
                    foreach ( MySlimBlock block in grid.CubeBlocks )
                    {
                        if ( !(block is Sandbox.ModAPI.Ingame.IMyShipController) )
                            continue;

                        Sandbox.ModAPI.Ingame.IMyShipController control = (Sandbox.ModAPI.Ingame.IMyShipController)block;
                        if ( control.IsUnderControl )
                        {
                            found = true;
                            break;
                        }
                    }

                    if ( found && !piloted )
                        continue;

                    Stop( entity );
                }

                else if ( floating && (entity is IMyFloatingObject || entity is MyInventoryBagEntity) )
                    Stop( entity );
            }
            if ( named )
            {
                IMyEntity entity;
                if ( !MyAPIGateway.Entities.TryGetEntityByName( shipName, out entity ) )
                {
                    Communication.SendPrivateInformation( userId, "Couldn't find a ship with name " + shipName );
                    return true;
                }
                Stop( entity );
            }
            Communication.SendPrivateInformation( userId, count.ToString( ) + " entities have been stopped." );
            return true;
        }

        private static void Stop( IMyEntity entity )
        {
            //if ( entity.Physics.LinearVelocity != Vector3D.Zero || entity.Physics.AngularVelocity != Vector3D.Zero )
             //   return;
                    
            Wrapper.GameAction( ( ) =>
            {                
                entity.Physics.LinearVelocity = Vector3D.Zero;
                entity.Physics.AngularVelocity = Vector3D.Zero;
            } );
            ++count;
        }

    }
}
