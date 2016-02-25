namespace EssentialsPlugin.EntityManagers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Xml.Serialization;
	using EssentialsPlugin.Utility;
	using EssentialsPlugin.UtilityClasses;
	using NLog;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
	using VRage.Game;
	using VRage.ModAPI;

	public class BlockManagement
	{
		private static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
		private static BlockManagement _instance = null;
		private static bool _checkDisable = false;
		private static bool _checkEnable = false;
		private static int _enableCount = 0;
		private static DateTime _lastCleanup = DateTime.Now;

		public BlockManagement()
		{
			GridDisabled = new HashSet<long>();
			GridBlocksDisabled = new SerializableDictionary<long, HashSet<long>>();
		}

		public static BlockManagement Instance
		{
			get
			{
				if (_instance == null)
				{
					Load();
				}

				return _instance;
			}
		}

		public SerializableDictionary<long, HashSet<long>> GridBlocksDisabled { get; set; }

		public HashSet<long> GridDisabled { get; set; }

		public void CheckAndDisableBlocks()
		{
			if (_checkDisable)
				return;

			_checkDisable = true;
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
					Log.Info(string.Format("CheckAndDisableBlocks(): Entity list busy, skipping check"));
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

				_enableCount = 0;
				Wrapper.GameAction(() =>
				{
					foreach (IMyEntity entity in disableSet)
					{
						DisableGrid((IMyCubeGrid)entity);
					}
				});

				if (disableSet.Count > 0)
				{
					Log.Info(string.Format("Disable: Block Management disabled {0} grids ({1} blocks diabled) - ({0} ms)", disableSet.Count, _enableCount, (DateTime.Now - start).TotalMilliseconds));
				}
			}
			catch (Exception ex)
			{
				Log.Info(string.Format("CheckAndDisableBlocks(): {0}", ex.ToString()));
			}
			finally
			{
				_checkDisable = false;
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
					_enableCount++;
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
			if (_checkEnable)
				return;

			_checkEnable = true;
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
					Log.Info(string.Format("CheckAndDisableBlocks(): Entity list busy, skipping check"));
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

				_enableCount = 0;
				Wrapper.GameAction(() =>
				{
					foreach (IMyEntity entity in enableSet)
					{
						EnableGrid((IMyCubeGrid)entity);
					}
				});

				if (enableSet.Count > 0)
				{
					Log.Info(string.Format("Enable: Block Management enabled {0} grids ({1} blocks enabled) - ({0} ms)", enableSet.Count, _enableCount, (DateTime.Now - start).TotalMilliseconds));
				}
			}
			catch (Exception ex)
			{
				Log.Info(string.Format("CheckAndDisableBlocks(): {0}", ex.ToString()));
			}
			finally
			{
				_checkEnable = false;
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
					_enableCount++;
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
						_instance = (BlockManagement)x.Deserialize(reader);
						reader.Close();
					}
				}
				else
					_instance = new BlockManagement();
			}
			catch (Exception ex)
			{
				Log.Info(string.Format("BlockManagement Load Error: {0}", ex.ToString()));
				_instance = new BlockManagement();
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
				Log.Info(string.Format("Error saving BlockManagement Data: {0}", ex.ToString()));
			}
		}

		private void GridListCleanup()
		{
			if (DateTime.Now - _lastCleanup < TimeSpan.FromSeconds(120))
				return;

			_lastCleanup = DateTime.Now;

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities);

			lock (GridDisabled)
			{
				int removed = GridDisabled.RemoveWhere(x => entities.FirstOrDefault(y => y.EntityId == x) == null);
				if(removed > 0)
					Log.Info(string.Format("Removed {0} entities from Disabled Grid List", removed));

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
