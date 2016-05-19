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

	public class HandleAdminMoveAreaTowards : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to move ships and stations from one area towards another area.  Usage: /admin move area towards [SX] [SY] [SZ] [TX] [TY] [TZ] [DISTANCE] [RADIUS] where S is source and T is the target area you want to move towards.  Distance is the amount of meters you'd like to move towards the target point.";
		}
		public override string GetCommandText()
		{
			return "/admin move area towards";
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
			if (words.Count() != 8 && words.Count() != 0)
				return false;

			if (words.Count() != 8)
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
			Vector3D targetPosition = new Vector3(float.Parse(words[3]), float.Parse(words[4]), float.Parse(words[5]));
			float distance = float.Parse(words[6]);
			float radius = float.Parse(words[7]);

			Vector3D movementPosition = targetPosition - startPosition;
			movementPosition.Normalize();
			Vector3D finalPosition = movementPosition * distance;
			//finalPosition += startPosition;
            
			BoundingSphereD sphere = new BoundingSphereD(startPosition, radius);
			List<IMyEntity> entitiesToMove = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

			Communication.SendPrivateInformation(userId, $"Moving all grids in a radius of {radius} near {General.Vector3DToString( startPosition )} towards {General.Vector3DToString( targetPosition )} by {distance} meters" );
		    int moveCount = 0;
		    foreach ( IMyEntity entity in entitiesToMove )
		    {
		        if ( !( entity is IMyCubeGrid ) )
		            continue;

		        Vector3D target = entity.GetPosition( ) + finalPosition;

		        Communication.SendPrivateInformation( userId, $"Moving '{entity.DisplayName}' from {entity.GetPosition( )} to {target}" );

		        //position can be set directly on the server
		        Wrapper.GameAction( ( ) => entity.SetPosition( target ) );

		        moveCount++;
		    }
            
			Communication.SendPrivateInformation(userId, $"Finished moving {moveCount} grids" );

			return true;
		}
	}
}
