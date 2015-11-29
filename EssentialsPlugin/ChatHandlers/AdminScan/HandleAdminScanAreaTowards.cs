namespace EssentialsPlugin.ChatHandlers
{
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using VRage.ModAPI;
	using VRageMath;

	public class HandleAdminScanAreaTowards : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to scan a section of space for grids in relation to another spot in space.  It will start at (SX, SY, SZ) and move towards (TX, TY, TZ) at a set distance of (DISTANCE) and then scan a radius of (RADIUS).  Usage: /admin scan area towards [SX] [SY] [SZ] [TX] [TY] [TZ] [DISTANCE] [RADIUS]";
		}

		public override string GetCommandText()
		{
			return "/admin scan area towards";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\"Sorry, there's nothing here yet :(\" \"close\" ";
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

		// admin scan x y z radius
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
			for(int r = 0; r < 8; r++)
			{
				if(!float.TryParse(words[r], out test))
				{
					Communication.SendPrivateInformation(userId, string.Format("The value at position {0} - '{1}' is invalid.  Please try the command again.", r + 1, words[r]));
					return true;
				}
			}

			Vector3D startPosition = new Vector3D(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2]));
			Vector3D targetPosition = new Vector3D(float.Parse(words[3]), float.Parse(words[4]), float.Parse(words[5]));
			float distance = float.Parse(words[6]);
			float radius = float.Parse(words[7]);

			Vector3D targetMovement = targetPosition - startPosition;
			targetMovement.Normalize();
			Vector3D finalPosition = targetMovement * distance;
			finalPosition += startPosition;

			List<MyObjectBuilder_CubeGrid> gridsToMove = new List<MyObjectBuilder_CubeGrid>();
			BoundingSphereD sphere = new BoundingSphereD(finalPosition, radius);
			List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
			int count = 0;

			Communication.SendPrivateInformation(userId, string.Format("Scanning {0} meters around {1}", radius, General.Vector3DToString(finalPosition)));
			Wrapper.GameAction(() =>
			{
				foreach (IMyEntity entity in entities)
				{
					if (!(entity is IMyCubeGrid))
						continue;
					
					Communication.SendPrivateInformation(userId, string.Format("Found ship {0} at {1}", entity.DisplayName, General.Vector3DToString(entity.GetPosition())));
					count++;
				}
			});

			Communication.SendPrivateInformation(userId, string.Format("Total ships found: {0}", count));
			return true;
		}
	}
}
