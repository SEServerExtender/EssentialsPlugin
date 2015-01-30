using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

using EssentialsPlugin.UtilityClasses;


namespace EssentialsPlugin.EntityManagers
{
	public class BlockManagement
	{
		private static BlockManagement m_instance = null;
		private static bool m_checkDisable = false;
		private static bool m_checkEnable = false;
		private static int m_enableCount = 0;
		private static DateTime m_lastCleanup = DateTime.Now;

		private HashSet<long> m_gridsDisabled;
		private SerializableDictionary<long, HashSet<long>> m_gridBlocksDisabled;

		public BlockManagement()
		{
			m_gridsDisabled = new HashSet<long>();
			m_gridBlocksDisabled = new SerializableDictionary<long, HashSet<long>>();
		}

		public static BlockManagement Instance
		{
			get
			{
				if (BlockManagement.m_instance == null)
				{
					Load();
				}

				return BlockManagement.m_instance;
			}
		}

		public SerializableDictionary<long, HashSet<long>> GridBlocksDisabled
		{
			get { return m_gridBlocksDisabled; }
			set { m_gridBlocksDisabled = value; }
		}

		public HashSet<long> GridDisabled
		{
			get { return m_gridsDisabled; }
			set { m_gridsDisabled = value; }
		}

		public void CheckAndDisableBlocks()
		{
			if (m_checkDisable)
				return;

			m_checkDisable = true;
			try
			{
				GridListCleanup();

				DateTime start = DateTime.Now;
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				List<IMyPlayer> players = new List<IMyPlayer>();
				try
				{
					MyAPIGateway.Entities.GetEntities(entities);
					MyAPIGateway.Players.GetPlayers(players);
				}
				catch
				{
					Logging.WriteLineAndConsole(string.Format("CheckAndDisableBlocks(): Entity list busy, skipping check"));
					return;
				}

				HashSet<IMyEntity> disableSet = new HashSet<IMyEntity>();

				foreach (IMyEntity entity in entities)
				{
					if(!entity.InScene)
						continue;

					if (!(entity is IMyCubeGrid))
						continue;					

					IMyCubeGrid grid = (IMyCubeGrid)entity;

					lock (GridDisabled)
					{
						if (GridDisabled.Contains(grid.EntityId))
							continue;
					}

					if (grid.Physics == null) // Projection
						continue;
				
					bool found = true;
					foreach (IMyPlayer player in players)
					{
						double distance = 0d;
						if (Entity.GetDistanceBetweenGridAndPlayer(grid, player, out distance))
						{
							if (distance < PluginSettings.Instance.DynamicConcealDistance)
							{
								found = false;
								break;
							}
						}
					}

					if (found)
					{
						disableSet.Add(grid);
					}
				}

				m_enableCount = 0;
				Wrapper.GameAction(() =>
				{
					foreach (IMyEntity entity in disableSet)
					{
						DisableGrid((IMyCubeGrid)entity);
					}
				});

				if (disableSet.Count > 0)
				{
					Logging.WriteLineAndConsole(string.Format("Disable: Block Management disabled {0} grids ({1} blocks diabled) - ({0} ms)", disableSet.Count, m_enableCount, (DateTime.Now - start).TotalMilliseconds));
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndDisableBlocks(): {0}", ex.ToString()));
			}
			finally
			{
				m_checkDisable = false;
			}
		}
		
		private void DisableGrid(IMyCubeGrid grid)
		{
			HashSet<long> disabledBlocks = new HashSet<long>();
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			grid.GetBlocks(blocks);

			foreach (IMySlimBlock block in blocks)
			{
				if (block.FatBlock == null)
					continue;

				IMyCubeBlock cubeBlock = block.FatBlock;

				if(!(cubeBlock is IMyFunctionalBlock))
					continue;

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor) ||
					cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Thrust) ||
					cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Battery) ||
					cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_SolarPanel) ||
					cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Gyro) ||
					cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MedicalRoom))
				{
					continue;
				}

				if(cubeBlock is IMyProductionBlock)
				{
					IMyProductionBlock productionBlock = (IMyProductionBlock)cubeBlock;

					if (productionBlock.IsProducing)
						continue;
				}

				if (FunctionalBlockEntity.GetState(cubeBlock))
				{
					FunctionalBlockEntity.SetState(cubeBlock, false);
					disabledBlocks.Add(cubeBlock.EntityId);
					m_enableCount++;
				}
			}

			lock (GridDisabled)
			{
				GridDisabled.Add(grid.EntityId);
				//if (disabledBlocks.Count > 0)
				//{
					Console.WriteLine("Adding");
					GridBlocksDisabled.Add(grid.EntityId, disabledBlocks);
				//}				
			}
		}

		public void CheckAndEnableBlocks()
		{
			if (m_checkEnable)
				return;

			m_checkEnable = true;
			try
			{
				GridListCleanup();

				DateTime start = DateTime.Now;
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				List<IMyPlayer> players = new List<IMyPlayer>();
				try
				{
					MyAPIGateway.Entities.GetEntities(entities);
					MyAPIGateway.Players.GetPlayers(players);
				}
				catch
				{
					Logging.WriteLineAndConsole(string.Format("CheckAndDisableBlocks(): Entity list busy, skipping check"));
					return;
				}

				HashSet<IMyEntity> enableSet = new HashSet<IMyEntity>();

				foreach (IMyEntity entity in entities)
				{
					if (!entity.InScene)
						continue;

					if (!(entity is IMyCubeGrid))
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;

					if(!GridDisabled.Contains(grid.EntityId))
						continue;

					if (grid.Physics == null) // Projection
						continue;

					bool found = false;
					foreach (IMyPlayer player in players)
					{
						double distance = 0d;
						if (Entity.GetDistanceBetweenGridAndPlayer(grid, player, out distance))
						{
							if (distance < PluginSettings.Instance.DynamicConcealDistance)
							{
								found = true;
								break;
							}
						}
					}

					if (found)
					{
						enableSet.Add(grid);
					}
				}

				m_enableCount = 0;
				Wrapper.GameAction(() =>
				{
					foreach (IMyEntity entity in enableSet)
					{
						EnableGrid((IMyCubeGrid)entity);
					}
				});

				if (enableSet.Count > 0)
				{
					Logging.WriteLineAndConsole(string.Format("Enable: Block Management enabled {0} grids ({1} blocks enabled) - ({0} ms)", enableSet.Count, m_enableCount, (DateTime.Now - start).TotalMilliseconds));
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndDisableBlocks(): {0}", ex.ToString()));
			}
			finally
			{
				m_checkEnable = false;
			}
		}

		public void EnableGrid(IMyCubeGrid grid)
		{
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			grid.GetBlocks(blocks);

			lock (GridDisabled)
			{
				if (!GridBlocksDisabled.ContainsKey(grid.EntityId))
				{
					if (GridDisabled.Contains(grid.EntityId))
						GridDisabled.Remove(grid.EntityId);

					return;
				}
			}

			HashSet<long> disabledBlocks = GridBlocksDisabled[grid.EntityId];
			
			foreach (IMySlimBlock block in blocks)
			{
				if (block.FatBlock == null)
					continue;

				IMyCubeBlock cubeBlock = block.FatBlock;

				if (!(cubeBlock is IMyFunctionalBlock))
					continue;

				if (!disabledBlocks.Contains(cubeBlock.EntityId))
					continue;

				if (!FunctionalBlockEntity.GetState(cubeBlock))
				{
					FunctionalBlockEntity.SetState(cubeBlock, true);
					m_enableCount++;
				}
			}

			lock (GridDisabled)
			{
				if(GridDisabled.Contains(grid.EntityId))
					GridDisabled.Remove(grid.EntityId);

				GridBlocksDisabled.Remove(grid.EntityId);
			}
		}

		private static void Load()
		{
			try
			{
				string fileName = Essentials.PluginPath + "Essentials-BlockManagement.xml";
				if (File.Exists(fileName))
				{
					using (StreamReader reader = new StreamReader(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(BlockManagement));
						BlockManagement.m_instance = (BlockManagement)x.Deserialize(reader);
						reader.Close();
					}
				}
				else
					BlockManagement.m_instance = new BlockManagement();
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("BlockManagement Load Error: {0}", ex.ToString()));
				BlockManagement.m_instance = new BlockManagement();
			}
		}

		public void Save()
		{
			try
			{
				lock (Instance)
				{
					string fileName = Essentials.PluginPath + "Essentials-BlockManagement.xml";
					string fileNameNew = Essentials.PluginPath + "Essentials-BlockManagement.xml.new";

					using (StreamWriter writer = new StreamWriter(fileNameNew))
					{
						XmlSerializer x = new XmlSerializer(typeof(BlockManagement));
						x.Serialize(writer, Instance);
						writer.Close();
					}

					if (File.Exists(fileName))
						File.Delete(fileName);

					File.Move(fileNameNew, fileName);
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("Error saving BlockManagement Data: {0}", ex.ToString()));
			}
		}

		private void GridListCleanup()
		{
			if (DateTime.Now - m_lastCleanup < TimeSpan.FromSeconds(120))
				return;

			m_lastCleanup = DateTime.Now;

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities);

			lock (GridDisabled)
			{
				int removed = GridDisabled.RemoveWhere(x => entities.FirstOrDefault(y => y.EntityId == x) == null);
				if(removed > 0)
					Logging.WriteLineAndConsole(string.Format("Removed {0} entities from Disabled Grid List", removed));

				HashSet<long> removeSet = new HashSet<long>();
				foreach (KeyValuePair<long, HashSet<long>> p in GridBlocksDisabled)
				{
					if (entities.FirstOrDefault(x => x.EntityId == p.Key) == null)
						removeSet.Add(p.Key);
				}

				foreach (long item in removeSet)
					GridBlocksDisabled.Remove(item);
			}
		}
	}
}
