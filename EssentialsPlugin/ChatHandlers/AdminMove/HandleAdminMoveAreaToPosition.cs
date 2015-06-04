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

			Communication.SendPrivateInformation(userId, string.Format("Moving all grids in a radius of {0} near {1} to {2}", radius, General.Vector3DToString(startPosition), General.Vector3DToString(movePosition)));

			List<MyObjectBuilder_CubeGrid> gridsToMove = new List<MyObjectBuilder_CubeGrid>();
			BoundingSphereD sphere = new BoundingSphereD(startPosition, radius);
			List<IMyEntity> entitiesToMove = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

//			Wrapper.GameAction(() =>
//			{
				foreach (IMyEntity entity in entitiesToMove)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					Communication.SendPrivateInformation(userId, string.Format("Moving '{0}' from {1} to {2}", entity.DisplayName, General.Vector3DToString(entity.GetPosition()), General.Vector3DToString(entity.GetPosition() + difference)));

					gridsToMove.Add((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder());
					//MyAPIGateway.Entities.RemoveEntity(entity);
					CubeGridEntity gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
					gridEntity.Dispose();
				}
//			});

			Log.Info("Entities Removed ... pausing");
			Thread.Sleep(5000);
			Log.Info("Removing entities from closed entities");

			Wrapper.GameAction(() =>
			{
				foreach(IMyEntity entity in entitiesToMove)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					Log.Info(string.Format("Removing '{0}' for move", entity.DisplayName));
					MyAPIGateway.Entities.RemoveFromClosedEntities(entity);
				}
			});

			Thread.Sleep(10000);

			Wrapper.GameAction(() =>
			{
				foreach(MyObjectBuilder_CubeGrid grid in gridsToMove)
				{
					grid.PositionAndOrientation = new MyPositionAndOrientation(grid.PositionAndOrientation.Value.Position + difference, grid.PositionAndOrientation.Value.Forward, grid.PositionAndOrientation.Value.Up);
					//Log.Info(string.Format("Adding '{0}' for move", grid.DisplayName));
					Communication.SendPrivateInformation(userId, string.Format("Adding grid '{0}' back to world.", grid.DisplayName));
					SectorObjectManager.Instance.AddEntity(new CubeGridEntity(grid));
					Thread.Sleep(1000);
				}
			});

			Communication.SendPrivateInformation(userId, string.Format("Moved {0} grids", gridsToMove.Count));

			return true;
		}
	}
}
