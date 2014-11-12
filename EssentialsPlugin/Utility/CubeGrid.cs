using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

namespace EssentialsPlugin.Utility
{
	public enum RemoveGridTypes
	{
		All,
		Ships,
		Stations
	}

	public static class CubeGrids
	{
		public static Vector3D RemoveGridsInSphere(ulong userId, Vector3D startPosition, float radius, RemoveGridTypes removeType)
		{
			List<MyObjectBuilder_CubeGrid> gridsToMove = new List<MyObjectBuilder_CubeGrid>();
			BoundingSphere sphere = new BoundingSphere(startPosition, radius);
			List<IMyEntity> entitiesToMove = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
			int count = 0;

			Wrapper.GameAction(() =>
			{
				foreach (IMyEntity entity in entitiesToMove)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					MyObjectBuilder_CubeGrid cubeGrid = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder();
					if (removeType == RemoveGridTypes.Ships && cubeGrid.IsStatic)
						continue;

					if (removeType == RemoveGridTypes.Stations && !cubeGrid.IsStatic)
						continue;

					Communication.SendPrivateInformation(userId, string.Format("Deleting entity '{0}' at {1}", entity.DisplayName, General.Vector3DToString(entity.GetPosition())));
					count++;
				}

				for (int r = entitiesToMove.Count - 1; r >= 0; r--)
				{
					IMyEntity entity = entitiesToMove[r];
					MyAPIGateway.Entities.RemoveEntity(entity);
				}
			});

			Communication.SendPrivateInformation(userId, string.Format("Total entities removed: {0}", count));
			return startPosition;
		}

		public static MyPositionAndOrientation CreatePositionAndOrientation(Vector3 position, Vector3 lookAt)
		{
            Vector3 newForward = Vector3.Normalize(position - lookAt);
            Quaternion rotate = MathUtility.GetRotation(Vector3.Forward, newForward, Vector3.Up);
            Matrix rot = Matrix.CreateFromQuaternion(rotate);
            return new MyPositionAndOrientation(position, rot.Forward, rot.Up);
		}
	}
}
