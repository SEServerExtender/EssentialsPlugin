using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

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
	public class HandleAdminTest : ChatHandlerBase
	{
		private Random m_random = new Random();
		private bool m_working = false;
		public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin test";
		}

		public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			GC.Collect();
			Wrapper.GameAction(() =>
			{
				GC.Collect();
			});

			if (m_working)
				return true;

			m_working = true;
			if (words[0] == "reveal")
			{
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				Wrapper.GameAction(() =>
				{
					MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
				});

				List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>();
				List<IMyEntity> updateList = new List<IMyEntity>();

				Wrapper.GameAction(() =>
				{
					foreach (IMyEntity entity in entities)
					{
						if (entity.InScene)
							continue;

						MyObjectBuilder_CubeGrid builder = null;
						try
						{
							builder = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(true);
						}
						catch
						{
							continue;
						}

						/*
						entity.InScene = true;
						entity.CastShadows = true;
						entity.Visible = true;
						*/

						Logging.WriteLineAndConsole(string.Format("Setting {0}", entity.EntityId));
						BaseEntityNetworkManager.BroadcastRemoveEntity(entity);
						//MyAPIGateway.Entities.RemoveEntity(entity);
						//MyAPIGateway.Entities.RemoveFromClosedEntities(entity);

						builder.PersistentFlags = (MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.CastShadows);
						MyAPIGateway.Entities.RemapObjectBuilder(builder);
						//SectorObjectManager.Instance.AddEntity(new CubeGridEntity(builder));

						MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
						addList.Add(builder);
						MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
						addList.Clear();
					}

					//if(addList.Count > 0)
						//MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);

				});
			}
			else
			{
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				Wrapper.GameAction(() =>
				{
					MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
				});

				List<MyObjectBuilder_CubeGrid> removeList = new List<MyObjectBuilder_CubeGrid>();
				Wrapper.GameAction(() =>
				{
					foreach (IMyEntity entity in entities)
					{
						if (!entity.InScene)
							continue;
				
						MyObjectBuilder_CubeGrid builder = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(true);
						if (builder.GridSizeEnum != MyCubeSize.Small)
							continue;

						Logging.WriteLineAndConsole(string.Format("Concealing {0}", entity.EntityId));

						entity.Physics.LinearVelocity = Vector3.Zero;
						entity.Physics.AngularVelocity = Vector3.Zero;

						/*
						entity.InScene = false;
						entity.CastShadows = false;
						entity.Visible = false;
						*/

						builder.PersistentFlags = MyPersistentEntityFlags2.None;
						MyAPIGateway.Entities.RemapObjectBuilder(builder);
						//removeList.Add(builder);

						BaseEntityNetworkManager.BroadcastRemoveEntity(entity);
						MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
						//MyAPIGateway.Entities.RemoveEntity(entity);
						//MyAPIGateway.Entities.RemoveFromClosedEntities(entity);
					}
				});

				/*
				Wrapper.GameAction(() =>
				{
					List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>();
					foreach (MyObjectBuilder_CubeGrid grid in removeList)
					{
						Logging.WriteLineAndConsole(string.Format("Adding {0}", grid.EntityId));
						MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid);

						//SectorObjectManager.Instance.AddEntity(new CubeGridEntity(grid));
						//						addList.Add(grid);
//						MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
//						addList.Clear();
					}
				});
				 */ 
			}

			m_working = false;
			return true;
		}

		private float GenerateRandomCoord(float halfExtent)
		{
			float result = (m_random.Next(200) + halfExtent) * (m_random.Next(2) == 0 ? -1 : 1);
			return result;
		}

	}
}
