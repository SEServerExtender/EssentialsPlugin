using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;

namespace EssentialsPlugin.ChatHandlers
{
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

			List<MyObjectBuilder_CubeGrid> gridsToMove = new List<MyObjectBuilder_CubeGrid>();
			BoundingSphere sphere = new BoundingSphere(startPosition, radius);
			List<IMyEntity> entitiesToMove = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

			Wrapper.GameAction(() =>
			{
				foreach (IMyEntity entity in entitiesToMove)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					gridsToMove.Add((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder());
					MyAPIGateway.Entities.RemoveEntity(entity);

					Logging.WriteLineAndConsole(string.Format("Moving '{0}' from {1} to {2}", entity.DisplayName, General.Vector3DToString(entity.GetPosition()), General.Vector3DToString(entity.GetPosition() + difference)));
				}
			});

			Thread.Sleep(5000);

			Wrapper.GameAction(() =>
			{
				foreach(IMyEntity entity in entitiesToMove)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					MyAPIGateway.Entities.RemoveFromClosedEntities(entity);
					Logging.WriteLineAndConsole(string.Format("Removing '{0}' for move", entity.DisplayName));
				}
			});

			Thread.Sleep(10000);

			Wrapper.GameAction(() =>
			{
				foreach(MyObjectBuilder_CubeGrid grid in gridsToMove)
				{
					grid.PositionAndOrientation = new MyPositionAndOrientation(grid.PositionAndOrientation.Value.Position + difference, grid.PositionAndOrientation.Value.Forward, grid.PositionAndOrientation.Value.Up);
					Logging.WriteLineAndConsole(string.Format("Adding '{0}' for move", grid.DisplayName));
					SectorObjectManager.Instance.AddEntity(new CubeGridEntity(grid));					
				}
			});

			return true;
		}
	}
}
