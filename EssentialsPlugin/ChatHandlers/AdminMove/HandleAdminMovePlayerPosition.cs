namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EssentialsPlugin.Utility;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using VRage.Audio;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class HandleAdminMovePlayerPosition : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to move a player to another location.  Usage: /admin move player position [USERNAME] [X] [Y] [Z]";
		}
		public override string GetCommandText()
		{
			return "/admin move player position";
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

		// /admin movefrom x y z x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			if (words.Count() != 4 && words.Count() != 0)
				return false;

			if (words.Count() != 4)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			// Test Input
			float test = 0;
			for(int r = 1; r < 4; r++)
			{
				if(!float.TryParse(words[r], out test))
				{
					Communication.SendPrivateInformation(userId, string.Format("The value at position {0} - '{1}' is invalid.  Please try the command again.", r + 1, words[r]));
					return true;
				}
			}

			string userName = words[0];
			Vector3D startPosition = new Vector3D(double.Parse(words[1]), double.Parse(words[2]), double.Parse(words[3]));

            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers( players, x=>x.DisplayName.Contains( userName, StringComparison.CurrentCultureIgnoreCase ) );
		    if ( players[0] == null )
		    {
		        Communication.SendPrivateInformation( userId, $"Couldn't find player with name {userName}." );
		        return true;
		    }
		    MyEntity controlledEntity = ((MyEntity)players[0].Controller.ControlledEntity).GetTopMostParent( );

		    float size = 2.5f;
		    if ( controlledEntity is MyCubeGrid )
		    {
		        size = (float)( (MyCubeGrid)controlledEntity ).PositionComp.WorldAABB.Extents.Max(  );
		    }
            //make sure we aren't moving the player inside a planet or something
		    Vector3D? testPos = null;
            
            Wrapper.GameAction( ()=> testPos = MyEntities.FindFreePlace( startPosition, size ));

            if ( testPos == null )
            {
                Communication.SendPrivateInformation( userId, $"Could not move player: {userName}. Position is not empty, try another." );
                return true;
            }

            //server controls movement now
            Wrapper.GameAction(()=>controlledEntity.PositionComp.SetPosition( testPos.Value ));

            return true;
		}
	}
}
