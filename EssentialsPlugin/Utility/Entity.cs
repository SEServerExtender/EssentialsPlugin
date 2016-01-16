namespace EssentialsPlugin.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EssentialsPlugin.UtilityClasses;
    using NLog;
    using Sandbox.Common;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.ModAPI;
    using VRageMath;

    class Entity
	{
		private static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
		static public Boolean GreaterThan( Vector3D v, Vector3D container )
		{
			if (v.X > container.X || v.Y > container.Y || v.Z > container.Z)
				return false;

			return true;
		}

		/*
		static public Boolean CheckCoolDown(String name, out int timeLeft)
		{
			timeLeft = 0;
			DockingCooldownItem item = null;
			var list = PluginDocking.CooldownList.FindAll(n => n.name.ToLower() == name.ToLower()); // Change to FirstOrDefault
			foreach (DockingCooldownItem i in list)
			{
				item = i;
				break;
			}

			if (item != null)
			{
				timeLeft = (int)(20.0 - (DateTime.Now - item.startTime).TotalSeconds);
				if (timeLeft <= 0)
				{
					PluginDocking.CooldownList.Remove(item);
					item = null;
				}
			}

			return item != null;
		}
		 */ 

		static public bool CheckForIntersection(Dictionary<String, List<IMyCubeBlock>> testList, List<IMyCubeBlock> beaconList, out int intersectElement)
		{
			Boolean intersects = false;
			intersectElement = 0;
			OrientedBoundingBoxD targetTestBounding = GetBoundingBox(beaconList);
			if (testList.Count > 0)
			{
				for (int r = 0; r < testList.Count; r++)
				{
					List<IMyCubeBlock> beaconTestList = (List<IMyCubeBlock>)testList.ElementAt(r).Value;
					if (beaconTestList.Count != 4)
						continue;

					OrientedBoundingBoxD testBounding = GetBoundingBox(beaconTestList);
					if (testBounding.Contains(ref targetTestBounding) != ContainmentType.Disjoint)
					{
						intersectElement = r;
						intersects = true;
						break;
					}
				}
			}

			return intersects;
		}

		static public OrientedBoundingBoxD GetBoundingBox(IMyCubeGrid entity)
		{
			var min = new Vector3D(int.MaxValue, int.MaxValue, int.MaxValue);
			var max = new Vector3D(int.MinValue, int.MinValue, int.MinValue);

			float multiplier = 2.5f;
			if (entity.GridSizeEnum == MyCubeSize.Small)
				multiplier = 0.5f;

			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			entity.GetBlocks(blocks, null);

			foreach (IMySlimBlock block in blocks)
			{
				//Vector3 pos = Entity.GetBlockEntityPosition(block);
				min = Vector3D.Min(block.Position * multiplier, min);
				max = Vector3D.Max(block.Position * multiplier, max);
			}

			Vector3D size = max - min;
			BoundingBoxD bb = new BoundingBoxD(new Vector3D(0, 0, 0), size).Translate(entity.GetPosition() - (size / 2));
			return new OrientedBoundingBoxD(bb.Center, bb.HalfExtents, Quaternion.CreateFromRotationMatrix(entity.WorldMatrix.GetOrientation()));
		}

		static public OrientedBoundingBoxD GetBoundingBox(List<IMyCubeBlock> cubeList)
		{
			Vector3D min = new Vector3D(int.MaxValue, int.MaxValue, int.MaxValue);
			Vector3D max = new Vector3D(int.MinValue, int.MinValue, int.MinValue);
			Vector3D pos = new Vector3D(0, 0, 0);

			IMyCubeGrid parent = (IMyCubeGrid)cubeList[0].Parent;

			foreach (IMyCubeBlock block in cubeList)
			{
				min.X = Math.Min(min.X, block.Position.X);
				min.Y = Math.Min(min.Y, block.Position.Y);
				min.Z = Math.Min(min.Z, block.Position.Z);
				max.X = Math.Max(max.X, block.Position.X);
				max.Y = Math.Max(max.Y, block.Position.Y);
				max.Z = Math.Max(max.Z, block.Position.Z);
				pos += parent.GridIntegerToWorld(block.Position);
			}

			pos = pos / cubeList.Count;

			// scale box to GridSize
			var size = max - min;
			double smallestNotZero = Math.Max(Math.Min(size.X, size.Y), Math.Min(Math.Max(size.X, size.Y), size.Z));

			Vector3D rawSize = new Vector3D(size.X * 2.5f, size.Y * 2.5f, size.Z * 2.5f);
			Vector3D expandedSize = new Vector3D(Math.Max(size.X, smallestNotZero) * 2.5f, Math.Max(size.Y, smallestNotZero) * 2.5f, Math.Max(size.Z, smallestNotZero) * 2.5f);

			BoundingBoxD bb = new BoundingBoxD(new Vector3D(0, 0, 0), expandedSize).Translate(pos - (expandedSize / 2));
			return new OrientedBoundingBoxD(bb.Center, bb.HalfExtents, Quaternion.CreateFromRotationMatrix(parent.WorldMatrix.GetOrientation()));
		}

		static public Boolean CheckOwnership(IMyCubeBlock entity, long playerId)
		{
			MyRelationsBetweenPlayerAndBlock relation = entity.GetUserRelationToOwner(playerId);

			if (relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Owner)
				return true;

			return false;
		}

		static public float CalculateMass(IMyEntity entity)
		{
			if (!(entity is IMyCubeGrid))
				return 0f;

			float mass = 0f;

			IMyCubeGrid grid = (IMyCubeGrid)entity;
			MyObjectBuilder_CubeGrid gridObject = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder();
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			grid.GetBlocks(blocks, null);
			foreach (IMySlimBlock block in blocks)
			{
				MyObjectBuilder_CubeBlock blockObject = block.GetCopyObjectBuilder();
				MyCubeBlockDefinition cubeDef = MyDefinitionManager.Static.GetCubeBlockDefinition(blockObject);
				mass += cubeDef.Mass;
			}

			return mass;
		}

		static public Vector3D GetBlockEntityPosition(IMyCubeBlock entity)
		{
			return ((IMyCubeGrid)entity.Parent).GridIntegerToWorld(entity.Position);
		}

		public static bool GetDistanceBetweenGridAndPlayer(IMyCubeGrid grid, IMyPlayer player, out double distance)
		{
			distance = 0d;

			try
			{
				if (player.Controller == null || player.Controller.ControlledEntity == null || player.Controller.ControlledEntity.Entity == null)
					return false;

				Vector3D playerPosition = player.Controller.ControlledEntity.Entity.GetPosition();
				distance = Vector3D.Distance(grid.GetPosition(), playerPosition);
			}
			catch (Exception ex)
			{
				Log.Info("GetDistanceBetweenGridAndPlayer(): {0}", ex.ToString());
				return false;
			}

			return true;
		}

		public static bool GetDistanceBetweenPointAndPlayer(Vector3D point, IMyPlayer player, out double distance)
		{
			distance = 0d;

			try
			{
				if (player.Controller == null || player.Controller.ControlledEntity == null || player.Controller.ControlledEntity.Entity == null)
					return false;

				Vector3D playerPosition = player.Controller.ControlledEntity.Entity.GetPosition();
				distance = Vector3D.Distance(point, playerPosition);
			}
			catch (Exception ex)
			{
				Log.Info("GetDistanceBetweenPointAndPlayer(): {0}", ex.ToString());
				return false;
			}

			return true;
		}
	}
}
