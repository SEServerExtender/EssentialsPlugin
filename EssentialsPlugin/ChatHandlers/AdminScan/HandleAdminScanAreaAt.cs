using System.Collections.Generic;
using System.Linq;
using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminScanAreaAt : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to scan a section of space for grids.  Usage: /admin scan area at [X] [Y] [Z] [RADIUS]";
		}

		public override string GetCommandText()
		{
			return "/admin scan area at";
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
			if (words.Count() != 4 && words.Count() != 0)
				return false;
			
			if (words.Count() == 0)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;			
			}

			// Test Input
			float test = 0;
			for(int r = 0; r < 4; r++)
			{
				if(!float.TryParse(words[r], out test))
				{
					Communication.SendPrivateInformation(userId, string.Format("The value at position {0} - '{1}' is invalid.  Please try the command again.", r + 1, words[r]));
					return true;
				}
			}

			Vector3D startPosition = new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2]));
			float radius = float.Parse(words[3]);

			List<MyObjectBuilder_CubeGrid> gridsToMove = new List<MyObjectBuilder_CubeGrid>();
			BoundingSphereD sphere = new BoundingSphereD(startPosition, radius);
			List<IMyEntity> entitiesToMove = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
			int count = 0;

			Wrapper.GameAction(() =>
			{
				foreach (IMyEntity entity in entitiesToMove)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					Communication.SendPrivateInformation(userId, string.Format("Found ship '{0}' ({1}) at {2}", entity.DisplayName, entity.EntityId, General.Vector3DToString(entity.GetPosition())));
					count++;
				}
			});

			Communication.SendPrivateInformation(userId, string.Format("Total ships found: {0}", count));
			return true;
		}
	}
}
