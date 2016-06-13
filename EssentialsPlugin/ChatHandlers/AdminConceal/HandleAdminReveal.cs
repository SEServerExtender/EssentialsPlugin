namespace EssentialsPlugin.ChatHandlers.AdminConceal
{
    using System.Collections.Generic;
    using System.Linq;
    using EntityManagers;
    using EssentialsPlugin.Utility;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Replication;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Entity;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;

    public class HandleAdminReveal : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to reveal concealed grids.  Usage: /admin reveal (force) - This command without 'force' only shows you how many would be revealed.";
		}

		public override string GetCommandText()
		{
			return "/admin reveal";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "Admin Reveal";
            DialogItem.content =
                " This command allows you to reveal concealed grids.|" +
                "Usage: /admin reveal (force) - this command without 'force' only show you how many grids would be revealed.||" +
                "This command will run when concealment is disabled, and respects the update time setting.";
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

		public override bool HandleCommand(ulong userId, string[] words)
        {
            bool force = words.FirstOrDefault( x => x.ToLower( ) == "force" ) != null;

            if ( force)
                EntityManagement.RevealAll( );

            else
            {
                   Communication.SendPrivateInformation( userId,
                                                         $"Command would reveal {EntityManagement.UnregisteredEntities.Count} grids.  Type /admin reveal force to reveal them." );
            }
            return true;
        }
	}
}
