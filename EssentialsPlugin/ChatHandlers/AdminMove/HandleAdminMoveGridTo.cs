namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using System.Linq;
    using System.Threading;
    using EssentialsPlugin.Utility;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Replication;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Entity;
    using SEModAPIInternal.API.Entity.Sector.SectorObject;
    using VRage;
    using VRage.Game.Entity;
    using VRage.ModAPI;
    using VRageMath;

    public class HandleAdminMoveGridTo : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to move a grid to a location near another player or grid.  Usage: /admin move grid to [SOURCE_SHIP|STATION] [TARGET_USERNAME] (DISTANCE)";
		}
		public override string GetCommandText()
		{
			return "/admin move grid to";
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

			string targetName;
			if(parse)
				targetName = string.Join(" ", words.Skip(1).Take(words.Count() - 2).ToArray());
			else
				targetName = string.Join(" ", words.Skip(1).ToArray());

			Communication.SendPrivateInformation(userId, $"Moving {sourceName} to within {distance}m of {targetName}." );

			Vector3D position;
            MyEntity entity = (MyEntity)Player.FindControlledEntity( targetName );
            if (entity == null)
			{
                entity = CubeGrids.Find( targetName );
                if ( entity == null )
                {
                    Communication.SendPrivateInformation( userId, $"Can not find user or grid with the name: {targetName}" );
                    return true;
                }
                position = entity.PositionComp.GetPosition();
			}
			else
				position = entity.PositionComp.GetPosition();

            MyEntity gridToMove = CubeGrids.Find( sourceName );
            if (gridToMove == null)
			{
				Communication.SendPrivateInformation(userId, $"Unable to find: {sourceName}" );
				return true;
			}
            Vector3D? testPos = null;

            Wrapper.GameAction(() => testPos = MyEntities.FindFreePlace(position, (float)gridToMove.PositionComp.WorldAABB.Extents.Max(  ) + distance));

            if (testPos == null)
            {
                Communication.SendPrivateInformation(userId, $"Could not find valid location to move: {sourceName}. Try increasing distance.");
                return true;
            }
            
            Wrapper.GameAction( ()=>gridToMove.PositionComp.SetPosition( testPos.Value ) );
            
            Communication.SendPrivateInformation(userId, $"Moved {sourceName} to within {(int)Math.Round( Vector3D.Distance(testPos.Value, position ) )}m of {targetName}" );
			return true;
		}
	}
}
