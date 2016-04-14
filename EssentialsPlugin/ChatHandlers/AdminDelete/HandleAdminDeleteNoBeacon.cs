namespace EssentialsPlugin.ChatHandlers.AdminDelete
{
    using System.Collections.Generic;
    using System.Linq;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI.Ingame;
    using Utility;

    public class HandleAdminDeleteNoBeacon : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "This command allows you to delete all grids without a beacon. Run the command with 'physical' to include grids connected by landing gear.";
        }

        public override string GetCommandText( )
        {
            return "/admin delete nobeacon";
        }

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            return new Communication.ServerDialogItem
            {
                title = "Help",
                header = "/admin delete nobeacon",
                buttonText = "close",
                content = "This command will delete all grids without a beacon. ||" +
                "If you run this command with the 'physical' argument, grids without a beacon attached" +
                "by landing gear to another grid wich does have a beacon will not be deleted. ||" +
                "Usage: /admin delete nobeacon (physical)"
            };
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
            GridLinkTypeEnum connectionType = GridLinkTypeEnum.Logical;
            if ( words.FirstOrDefault( x => x.ToLower( ) == "physical" ) != null )
                connectionType = GridLinkTypeEnum.Physical;
            
            HashSet<GridGroup> groups = GridGroup.GetAllGroups( connectionType );
            int groupsCount = 0;
            int gridsCount = 0;

            foreach ( GridGroup group in groups )
            {
                if ( !group.CubeBlocks.Any( x => x?.FatBlock is IMyBeacon ) )
                {
                    groupsCount++;
                    gridsCount += group.Grids.Count;
                    group.Close( );
                    Communication.SendPrivateInformation( userId, $"Found group with parent {group.Parent.DisplayName} ({group.Parent.EntityId}) at {group.Parent.PositionComp.GetPosition( )} with no beacon." );
                }
            }

            Communication.SendPrivateInformation( userId, $"Removed {gridsCount} grids in {groupsCount} groups with no beacon." );

            return true;
        }
    }
}
