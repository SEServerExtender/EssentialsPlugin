namespace EssentialsPlugin.ChatHandlers.AdminConceal
{
    using System.Collections.Generic;
    using System.Linq;
    using EntityManagers;
    using EssentialsPlugin.Utility;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

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

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\" This command allows you to reveal concealed grids.|" +
                "Usage: /admin reveal (force) - this command without 'force' only show you how many grids would be revealed.||" +
                "This command will run when concealment is disabled, and respects the update time setting." +
                "\"\" \"close\" ";
            return longMessage;
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
			bool force = words.FirstOrDefault(x => x.ToLower() == "force") != null;

            if ( force )
                EntityManagement.RevealAll( );
            else
            {
                HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
                Wrapper.GameAction( ( ) => MyAPIGateway.Entities.GetEntities( entities ) );

                List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>( );
                int count = 0;
                Wrapper.GameAction( ( ) =>
                 {
                     foreach ( IMyEntity entity in entities )
                     {
                         if ( entity.InScene )
                             continue;

                         if ( !(entity is IMyCubeGrid) )
                             continue;

                         MyObjectBuilder_CubeGrid builder = CubeGrids.SafeGetObjectBuilder( (IMyCubeGrid)entity );
                         if ( builder == null )
                             continue;

                         count++;
                     }
                 } );

                Log.Info( string.Format( "Command would Reveal {0} grids.  Type /admin reveal force to reveal them.", count ) );
            }
            return true;
		}
	}
}
