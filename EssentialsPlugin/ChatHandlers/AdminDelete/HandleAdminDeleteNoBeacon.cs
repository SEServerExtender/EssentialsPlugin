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
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminDeleteNoBeacon : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to delete all stations from an area defined by x, y, z, and radius.  Usage: /admin delete ships area [X] [Y] [Z] [RADIUS]";
		}

		public override string GetCommandText()
		{
			return "/admin delete nobeacon";
		}

		public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		// admin deletearea x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			HashSet<IMyEntity> entitiesToConfirm = new HashSet<IMyEntity>();
			HashSet<IMyEntity> entitiesConnected = new HashSet<IMyEntity>();
			List<IMyEntity> entitiesFound = new List<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
			});

			foreach (IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				List<IMySlimBlock> blocks = new List<IMySlimBlock>();
				grid.GetBlocks(blocks, x => x.FatBlock != null && x.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon));

				if (blocks.Count > 0)
					continue;

				entitiesToConfirm.Add(grid);
			}

			foreach (IMyEntity entity in entitiesToConfirm)
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;
				List<IMySlimBlock> blocks = new List<IMySlimBlock>();
				grid.GetBlocks(blocks, null);
				foreach (IMySlimBlock block in blocks)
				{
					if (block.FatBlock != null)
					{
						IMyCubeBlock cubeBlock = block.FatBlock;

						if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_ShipConnector))
						{
							MyObjectBuilder_ShipConnector connector = (MyObjectBuilder_ShipConnector)cubeBlock.GetObjectBuilderCubeBlock();
							if (connector.Connected)
							{
								IMyEntity connectedEntity = (IMyEntity)MyAPIGateway.Entities.GetEntityById(connector.ConnectedEntityId);

								if (connectedEntity != null)
									entitiesConnected.Add(entity);

								break;
							}
						}

						if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_PistonBase))
						{
							entitiesConnected.Add(entity);
							break;
						}

						if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_ExtendedPistonBase))
						{
							entitiesConnected.Add(entity);
							break;
							/*
							MyObjectBuilder_PistonBase pistonBase = (MyObjectBuilder_PistonBase)cubeBlock.GetObjectBuilderCubeBlock();
							if (pistonBase.TopBlockId != 0)
							{
								IMyEntity connectedEntity = (IMyEntity)MyAPIGateway.Entities.GetEntityById(pistonBase.TopBlockId);

								if (connectedEntity != null)
									entitiesConnected.Add(entity);

								break;
							}
							 */
						}

						if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_PistonTop))
						{
							entitiesConnected.Add(entity);
							break;

							/*
							MyObjectBuilder_PistonTop pistonTop = (MyObjectBuilder_PistonTop)cubeBlock.GetObjectBuilderCubeBlock();
							if (pistonTop.PistonBlockId != 0)
							{
								IMyEntity connectedEntity = (IMyEntity)MyAPIGateway.Entities.GetEntityById(pistonTop.PistonBlockId);

								if (connectedEntity != null)
									entitiesConnected.Add(entity);

								break;
							}
								*/
						}

						if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator))
						{
							MyObjectBuilder_MotorAdvancedStator stator = (MyObjectBuilder_MotorAdvancedStator)cubeBlock.GetObjectBuilderCubeBlock();
							if (stator.RotorEntityId != 0)
							{
								IMyEntity connectedEntity = (IMyEntity)MyAPIGateway.Entities.GetEntityById(stator.RotorEntityId);

								if (connectedEntity != null)
									entitiesConnected.Add(entity);

								break;
							}
						}

						if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedRotor))
						{
							entitiesConnected.Add(entity);
							break;
						}

						if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorStator))
						{
							MyObjectBuilder_MotorStator stator = (MyObjectBuilder_MotorStator)cubeBlock.GetObjectBuilderCubeBlock();
							if (stator.RotorEntityId != 0)
							{
								IMyEntity connectedEntity = (IMyEntity)MyAPIGateway.Entities.GetEntityById(stator.RotorEntityId);

								if (connectedEntity != null)
									entitiesConnected.Add(entity);

								break;
							}
						}

						if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorRotor))
						{
							entitiesConnected.Add(entity);
							break;
						}
					}
				}

				if (!entitiesConnected.Contains(entity))
					entitiesFound.Add(entity);
			}

			foreach (IMyEntity entity in entitiesFound)
			{
				CubeGridEntity gridEntity = (CubeGridEntity)GameEntityManager.GetEntity(entity.EntityId);
				Communication.SendPrivateInformation(userId, string.Format("Found entity '{0}' ({1}) at {2} with no beacon.", gridEntity.Name, entity.EntityId, General.Vector3DToString(entity.GetPosition())));
			}

			for(int r = entitiesFound.Count - 1; r >= 0; r--)
			{
				//MyAPIGateway.Entities.RemoveEntity(entity);
				IMyEntity entity = entitiesFound[r];
				CubeGridEntity gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
				gridEntity.Dispose();
			}

			Communication.SendPrivateInformation(userId, string.Format("Removed {0} grids with no beacons", entitiesFound.Count));

			return true;
		}
	}
}
