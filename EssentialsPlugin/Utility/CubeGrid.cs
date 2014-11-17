using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;

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
			List<IMyEntity> entitiesToRemove = new List<IMyEntity>();
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

					entitiesToRemove.Add(entity);
					Communication.SendPrivateInformation(userId, string.Format("Deleting entity '{0}' at {1}", entity.DisplayName, General.Vector3DToString(entity.GetPosition())));
					count++;
				}
			});

			for (int r = entitiesToRemove.Count - 1; r >= 0; r--)
			{
				IMyEntity entity = entitiesToRemove[r];
				//MyAPIGateway.Entities.RemoveEntity(entity);
				CubeGridEntity gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
				gridEntity.Dispose();
			}

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

		public static void GetBlocksUnconnected(HashSet<IMyEntity> connectedList, HashSet<IMyEntity> entitiesToConfirm)
		{
			foreach (IMyEntity entity in entitiesToConfirm)
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;
				MyObjectBuilder_CubeGrid gridBuilder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder();

				bool result = false;
				foreach (MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
				{
					if (block.TypeId == typeof(MyObjectBuilder_ShipConnector))
					{
						MyObjectBuilder_ShipConnector connector = (MyObjectBuilder_ShipConnector)block;
						if (connector.Connected)
						{
							IMyEntity connectedEntity = (IMyEntity)MyAPIGateway.Entities.GetEntityById(connector.ConnectedEntityId);

							if (connectedEntity != null)
							{
								result = true;
								break;
							}
						}
					}

					if (block.TypeId == typeof(MyObjectBuilder_PistonBase))
					{
						result = true;
						break;
					}

					if (block.TypeId == typeof(MyObjectBuilder_ExtendedPistonBase))
					{
						result = true;
						break;
					}

					if (block.TypeId == typeof(MyObjectBuilder_PistonTop))
					{
						result = true;
						break;
					}

					if (block.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator))
					{
						MyObjectBuilder_MotorAdvancedStator stator = (MyObjectBuilder_MotorAdvancedStator)block;
						if (stator.RotorEntityId != 0)
						{
							IMyEntity connectedEntity = (IMyEntity)MyAPIGateway.Entities.GetEntityById(stator.RotorEntityId);

							if (connectedEntity != null)
							{
								result = true;
								break;
							}
						}
					}

					if (block.TypeId == typeof(MyObjectBuilder_MotorAdvancedRotor))
					{
						result = true;
						break;
					}

					if (block.TypeId == typeof(MyObjectBuilder_MotorStator))
					{
						MyObjectBuilder_MotorStator stator = (MyObjectBuilder_MotorStator)block;
						if (stator.RotorEntityId != 0)
						{
							IMyEntity connectedEntity = (IMyEntity)MyAPIGateway.Entities.GetEntityById(stator.RotorEntityId);

							if (connectedEntity != null)
							{
								result = true;
								break;
							}
						}
					}

					if (block.TypeId == typeof(MyObjectBuilder_MotorRotor))
					{
						result = true;
						break;
					}
				}

				if (!result)
					connectedList.Add(entity);
			}
		}
	}
}
