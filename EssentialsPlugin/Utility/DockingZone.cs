using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;

using VRageMath;

using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;

using EssentialsPlugin.UtilityClasses;

namespace EssentialsPlugin.Utility
{
	public static class DockingZone
	{
		private static List<DockingCooldownItem> m_cooldownItems = new List<DockingCooldownItem>();

		public static bool IsGridInside(IMyCubeGrid dockingEntity, List<IMyCubeBlock> beaconList)
		{
			// Get bounding box of both the docking zone and docking ship
			OrientedBoundingBoxD targetBounding = Entity.GetBoundingBox(beaconList);
			OrientedBoundingBoxD dockingBounding = Entity.GetBoundingBox(dockingEntity);

			// If the docking entity is bigger in some way than the zone, this will fail (docking ship larger than dock) ???
			if (!Entity.GreaterThan(dockingBounding.HalfExtent * 2, targetBounding.HalfExtent * 2))
			{
				return false;
			}

			// Make sure the docking zone contains the docking ship.  If they intersect or are disjointed, then fail
			if (targetBounding.Contains(ref dockingBounding) != ContainmentType.Contains)
			{
				return false;
			}

			return true;
		}

		public static Dictionary<string, List<IMyCubeBlock>> GetZonesInGrid(IMyCubeGrid cubeGrid)
		{
			Dictionary<String, List<IMyCubeBlock>> testList = new Dictionary<string, List<IMyCubeBlock>>();
			List<IMySlimBlock> cubeBlocks = new List<IMySlimBlock>();
			cubeGrid.GetBlocks(cubeBlocks);
			foreach (IMySlimBlock entityBlock in cubeBlocks)
			{
				if (entityBlock.FatBlock == null)
					continue;

				IMyCubeBlock cubeBlock = entityBlock.FatBlock;

				if (!(cubeBlock is Sandbox.ModAPI.Ingame.IMyBeacon))
					continue;

				Sandbox.ModAPI.Ingame.IMyBeacon beacon = (Sandbox.ModAPI.Ingame.IMyBeacon)cubeBlock;
				if (string.IsNullOrEmpty( beacon.CustomName ))
					continue;

				if (testList.ContainsKey(beacon.CustomName))
				{
					testList[beacon.CustomName].Add(entityBlock.FatBlock);
				}
				else
				{
					List<IMyCubeBlock> testBeaconList = new List<IMyCubeBlock>();
					testBeaconList.Add(entityBlock.FatBlock);
					testList.Add(beacon.CustomName, testBeaconList);
				}
			}

			Dictionary<String, List<IMyCubeBlock>> resultList = new Dictionary<string, List<IMyCubeBlock>>();
			foreach (KeyValuePair<String, List<IMyCubeBlock>> p in testList)
			{
				if (p.Value.Count == 4)
				{
					resultList.Add(p.Key, p.Value);
				}
			}

			return resultList;
		}

		static public bool DoesGridContainZone(IMyCubeGrid cubeGrid)
		{
			return GetZonesInGrid(cubeGrid).Count > 0;
		}

		static public void FindByName(String pylonName, out Dictionary<String, List<IMyCubeBlock>> testList, out List<IMyCubeBlock> beaconList, long playerId)
		{
			IMyCubeGrid beaconParent = null;
			testList = new Dictionary<string, List<IMyCubeBlock>>();
			beaconList = new List<IMyCubeBlock>();
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, null);
			});

			foreach (IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				IMyCubeGrid cubeGrid = (IMyCubeGrid)entity;

				if (cubeGrid == null || cubeGrid.GridSizeEnum == MyCubeSize.Small)
					continue;

				if (!cubeGrid.BigOwners.Contains(playerId) && !cubeGrid.SmallOwners.Contains(playerId))
					continue;

				testList.Clear();
				beaconList.Clear();
				beaconParent = cubeGrid;

				List<IMySlimBlock> cubeBlocks = new List<IMySlimBlock>();
				cubeGrid.GetBlocks(cubeBlocks);
				foreach (IMySlimBlock entityBlock in cubeBlocks)
				{
					if (entityBlock.FatBlock == null)
						continue;

					if (!(entityBlock.FatBlock is IMyCubeBlock))
						continue;

					IMyCubeBlock cubeBlock = entityBlock.FatBlock;

					if (!(cubeBlock is Sandbox.ModAPI.Ingame.IMyBeacon))
						continue;

					IMyTerminalBlock beacon = (IMyTerminalBlock)cubeBlock;
					/*
					MyObjectBuilder_CubeBlock blockObject;
					try
					{
						blockObject = entityBlock.FatBlock.GetObjectBuilderCubeBlock();
						if (blockObject == null)
							continue;
					}
					catch
					{
						continue;
					}

					if (!(blockObject is MyObjectBuilder_Beacon))
						continue;

					MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)blockObject;
					 */ 

					if (string.IsNullOrEmpty( beacon.CustomName ))
						continue;

					if (beacon.IsFunctional &&
					   beacon.CustomName.ToLower() == pylonName.ToLower()
					  )
					{
						beaconList.Add(entityBlock.FatBlock);
						Vector3D beaconPos = Entity.GetBlockEntityPosition(entityBlock.FatBlock);
						continue;
					}

					if (testList.ContainsKey(beacon.CustomName))
						testList[beacon.CustomName].Add(entityBlock.FatBlock);
					else
					{
						List<IMyCubeBlock> testBeaconList = new List<IMyCubeBlock>();
						testBeaconList.Add(entityBlock.FatBlock);
						testList.Add(beacon.CustomName, testBeaconList);
					}
				}

				if (beaconList.Count == 4)
					break;
			}
		}

		public static void AddCooldown(string name)
		{
			DockingCooldownItem item = new DockingCooldownItem();
			item.Start = DateTime.Now;
			item.Name = name;

			lock (m_cooldownItems)
				m_cooldownItems.Add(item);
		}

		public static bool CheckCooldown(string name)
		{
			lock (m_cooldownItems)
			{
				DockingCooldownItem item = m_cooldownItems.FindAll(x => x.Name == name).FirstOrDefault();
				if (item != null)
				{
					if (DateTime.Now - item.Start > TimeSpan.FromSeconds(15))
					{
						m_cooldownItems.RemoveAll(x => x.Name == name);
						return true;
					}
					else
					{
						return false;
					}
				}
			}

			return true;
		}
	}

	public class Docking
	{
		private static Docking m_instance;
		private static bool m_loading = false;

		private List<DockingItem> m_dockingItems = new List<DockingItem>();

		public Docking()
		{
			if (!Directory.Exists(Essentials.PluginPath + "\\Docking"))
				Directory.CreateDirectory(Essentials.PluginPath + "\\Docking");
		}

		public static Docking Instance
		{
			get 			
			{
				if (Docking.m_instance == null)
				{
					Docking.m_instance = new Docking();
					Docking.m_instance.Load();
				}

				return Docking.m_instance; 
			}
		}

		public List<DockingItem> DockingItems
		{
			get { return m_dockingItems; }
		}

		public void Save()
		{
			if (m_loading)
				return;

			lock(m_instance)
			{
				try
				{
					string fileName = Essentials.PluginPath + "Eessentials-Docking.xml";
					using (StreamWriter writer = new StreamWriter(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(Docking));
						x.Serialize(writer, m_instance);
						writer.Close();
					}					
				}
				catch (Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("Save(): {0}", ex.ToString()));
				}
			}
		}

		public void Load()
		{
			if (m_loading)
				return;

			lock (m_instance)
			{
				try
				{
					m_loading = true;
					string fileName = Essentials.PluginPath + "Eessentials-Docking.xml";

					if (File.Exists(fileName))
					{
						using (StreamReader reader = new StreamReader(fileName))
						{
							XmlSerializer x = new XmlSerializer(typeof(Docking));
							m_instance = (Docking)x.Deserialize(reader);
							reader.Close();
						}

						Logging.WriteLineAndConsole(string.Format("Loaded {0} Docking Items", m_instance.DockingItems));
					}
				}
				catch (Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("Load(): {0}", ex.ToString()));
				}
				finally
				{
					m_loading = false;
				}
			}
		}

		public void Add(DockingItem item)
		{
			lock(m_instance)
				m_dockingItems.Add(item);

			Save();
		}

		public List<DockingItem> Find(Func<DockingItem, bool> search)
		{
			lock (m_instance)
				return m_dockingItems.Where(search).ToList();
		}

		public void Remove(DockingItem item)
		{
			lock (m_instance)
				m_dockingItems.Remove(item);

			Save();
		}
	}

	[Serializable]
	public class DockingItem
	{
		public long PlayerId { get; set; }

		public long DockedEntityId { get; set; }

		public long TargetEntityId { get; set; }

		public long[ ] DockingBeaconIds { get; set; }

		public Vector3 SavePos { get; set; }

		public Quaternion SaveQuat { get; set; }

		public string DockedName { get; set; }
	}

	public class DockingCooldownItem
	{
		public DateTime Start { get; set; }

		public string Name { get; set; }
	}
}
