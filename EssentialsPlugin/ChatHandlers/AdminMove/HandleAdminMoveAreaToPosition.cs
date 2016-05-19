namespace EssentialsPlugin.ChatHandlers
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Entity;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using VRage;
	using VRage.Game;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;
	using VRageMath;

	public class HandleAdminMoveAreaToPosition : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to move ships and stations from one area to another.  Usage: /admin move area to position [SX] [SY] [SZ] [MX] [MY] [MZ] [RADIUS] where S is source and M is move amount which can positive or negative.";
		}
		public override string GetCommandText()
		{
			return "/admin move area to position";
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
			if (words.Count() != 7 && words.Count() != 0)
				return false;

			if (words.Count() != 7)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			// Test Input
			float test = 0;
			for(int r = 0; r < 7; r++)
			{
				if(!float.TryParse(words[r], out test))
				{
					Communication.SendPrivateInformation(userId, string.Format("The value at position {0} - '{1}' is invalid.  Please try the command again.", r + 1, words[r]));
					return true;
				}
			}

			Vector3D startPosition = new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2]));
			Vector3D movePosition = new Vector3(float.Parse(words[3]), float.Parse(words[4]), float.Parse(words[5]));
			Vector3D difference = startPosition - movePosition;
			float radius = float.Parse(words[6]);

			Communication.SendPrivateInformation(userId, $"Moving all grids in a radius of {radius} near {startPosition} to {movePosition}" );
            
			BoundingSphereD sphere = new BoundingSphereD(startPosition, radius);
			List<IMyEntity> entitiesToMove = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
		    int moveCount = 0;
		    foreach ( IMyEntity entity in entitiesToMove )
		    {
		        if ( !( entity is IMyCubeGrid ) )
		            continue;

		        Vector3D target = entity.GetPosition( ) + difference;

		        Communication.SendPrivateInformation( userId, $"Moving '{entity.DisplayName}' from {entity.GetPosition( )} to {target}" );

		        //position can be set directly on the server
		        Wrapper.GameAction( ( ) => entity.SetPosition( target ) );
		        moveCount++;
		    }

		    Communication.SendPrivateInformation(userId, $"Moved {moveCount} grids" );

			return true;
		}
	}
}
