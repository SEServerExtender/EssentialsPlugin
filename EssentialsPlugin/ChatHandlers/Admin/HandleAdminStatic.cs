namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System.Diagnostics;
    using System.Linq;
    using ParallelTasks;
    using Sandbox.Definitions;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Engine.Voxels;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Character;
    using Sandbox.Game.Replication;
    using Utility;
    using VRage.Game;

    public class HandleAdminStatic : ChatHandlerBase
	{

    public override string GetHelp()
		{
			return "Sets grids as static. Usage: /admin static (all, small, large)";
		}

		public override string GetCommandText()
		{
			return "/admin static";
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
            if (words.Length != 1)
            {
                Communication.SendPrivateInformation( userId, GetHelp( ) );
                return false;
            }
            bool small;
            bool large;

            switch (words[0].ToLower( ))
            {
                case "small":
                    small = true;
                    large = false;
                    break;
                case "large":
                    small = false;
                    large = true;
                    break;
                case "all":
                    small = true;
                    large = true;
                    break;
                default:
                    Communication.SendPrivateInformation( userId, GetHelp( ) );
                    return false;
            }

            var ents = MyEntities.GetEntities( ).ToArray( );
            int count = 0;
            foreach (var ent in ents)
            {
                var grid = ent as MyCubeGrid;
                if (grid == null)
                    continue;

                if (grid.Physics == null || grid.IsStatic)
                    continue;

                if ((grid.GridSizeEnum == MyCubeSize.Large && large) || (grid.GridSizeEnum == MyCubeSize.Small && small))
                {
                    Wrapper.BeginGameAction( ( ) =>
                                             {
                                                 grid.Physics.ClearSpeed( );
                                                 grid.RequestConversionToStation( );
                                             });
                    Essentials.Log.Info( $"Converted {grid.DisplayName} to static" );
                    count++;
                }
            }
            Communication.SendPrivateInformation( userId, $"Converted {count} grids to static" );
            return true;
        }

	}

}

