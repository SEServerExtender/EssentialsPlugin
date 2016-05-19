namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using EntityManagers;
    using EssentialsPlugin.Utility;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public class HandleAdminMovePlayerTo : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to move a player to a location near another player.  Usage: /admin move player to [SOURCE_USERNAME] [TARGET_USERNAME|TARGET_SHIPNAME] (DISTANCE)";
		}
		public override string GetCommandText()
		{
			return "/admin move player to";
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
			if (words.Count() < 2)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}
			
			string sourceName = words[0];
			float distance = 50f;

			// TODO Allow quotes so we can do distance?
			bool parse = false;
			if (words.Count() > 2)
			{
				parse = float.TryParse(words[words.Count() - 1], out distance);
			}

            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, x => x.DisplayName.Contains(sourceName, StringComparison.CurrentCultureIgnoreCase));
            if (players[0] == null)
            {
                Communication.SendPrivateInformation(userId, $"Couldn't find player with name {sourceName}.");
                return true;
            }
            var controlledEntity = players[0].Controller.ControlledEntity;

            //kick the player out of their cockpit if they're in one
		    if ( controlledEntity is MyShipController )
		    {
		        Wrapper.GameAction(()=>((MyShipController)controlledEntity).RemoveUsers( false ));
		    }

            string targetName;
			if(parse)
				targetName = string.Join(" ", words.Skip(1).Take(words.Count() - 2).ToArray());
			else
				targetName = string.Join(" ", words.Skip(1).ToArray());

			IMyEntity entity = Player.FindControlledEntity(targetName);
			if (entity == null)
			{
				entity = CubeGrids.Find(targetName);
				if(entity == null)
				{
					Communication.SendPrivateInformation(userId, $"Can not find user or grid with the name: {targetName}" );
					return true;
				}
			}            

            Vector3D position = entity.GetPosition();

			Communication.SendPrivateInformation(userId, $"Trying to move {sourceName} to within {distance}m of {targetName}." );
			Vector3D startPosition = MathUtility.RandomPositionFromPoint((Vector3)position, distance);

            //make sure we aren't moving the player inside a planet or something
            Vector3D? testPos = null;

            Wrapper.GameAction(() => testPos = MyEntities.FindFreePlace(startPosition, 2.5f + distance));

            if (testPos == null)
            {
                Communication.SendPrivateInformation( userId, $"Could not find valid location to move player: {sourceName}. Try increasing distance." );
                return true;
            }
            
            //server controls movement now
            Wrapper.GameAction(() => ((MyEntity)players[0].Controller.ControlledEntity).PositionComp.SetPosition(testPos.Value));

            Communication.SendPrivateInformation(userId, string.Format("Moved {0} to within {1}m of {2}", sourceName, (int)Math.Round(Vector3D.Distance(startPosition, position)), targetName));
			return true;
		}
	}
}
