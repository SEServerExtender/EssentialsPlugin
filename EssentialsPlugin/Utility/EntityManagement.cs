using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

using EssentialsPlugin.ProcessHandler;

namespace EssentialsPlugin.Utility
{
	public class EntityManagement
	{
		private static volatile bool m_checkReveal = false;
		private static volatile bool m_checkConceal = false;
		private static HashSet<IMyEntity> m_processedGrids = new HashSet<IMyEntity>();
		private static List<long> m_removedGrids = new List<long>();
		private static List<IMyEntity> m_scanCache = new List<IMyEntity>();
		private static int m_turretsEnabled = 0;
		private static int m_turretsDisabled = 0;
		private static int m_turretsToggled = 0;

		public static void CheckAndConcealEntities()
		{
			if (m_checkConceal)
				return;

			m_checkConceal = true;
			try
			{
				DateTime start = DateTime.Now;

				m_processedGrids.Clear();
				List<IMyPlayer> players = new List<IMyPlayer>();
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>();

				Wrapper.GameAction(() =>
				{
					try
					{
						MyAPIGateway.Players.GetPlayers(players);
					}
					catch(Exception ex)
					{
						Logging.WriteLineAndConsole(string.Format("Error getting players list.  Check and Conceal failed: {0}", ex.ToString()));
						return;
					}
				});

				try
				{
					MyAPIGateway.Entities.GetEntities(entities);
				}
				catch
				{
					Logging.WriteLineAndConsole("CheckAndConcealEntities(): Error getting entity list, skipping check");
					return;
				}
				
				CubeGrids.GetBlocksUnconnected(entitiesFound, entities);

				HashSet<IMyEntity> entitiesToConceal = new HashSet<IMyEntity>();
				foreach (IMyEntity entity in entitiesFound)
				{
					if(!(entity is IMyCubeGrid))
						continue;

					if (entity.DisplayName.Contains("CommRelay"))
						continue;

					if(!entity.InScene)
						continue;

					if(((IMyCubeGrid)entity).GridSizeEnum != MyCubeSize.Small && !PluginSettings.Instance.ConcealIncludeLargeGrids)
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;

					bool found = false;
					foreach(IMyPlayer player in players)
					{
						double distance = 0f;
						if (GetDistanceBetweenGridAndPlayer(grid, player, out distance))
						{
							if (distance < PluginSettings.Instance.DynamicConcealDistance)
							{
								found = true;
							}

						}
					}

					if(!found)
					{
						// Check to see if grid is close to dock / shipyard
						foreach (IMyCubeGrid checkGrid in ProcessDockingZone.ZoneCache)
						{
							try
							{
								if(Vector3D.Distance(checkGrid.GetPosition(), grid.GetPosition()) < 100d)
								{
									found = true;
									break;
								}
							}	
							catch
							{
								continue;
							}
						}
					}

					if(!found)
					{
						// Check for block type rules
						if (CheckConcealBlockRules(grid, players))
						{
							found = true;
						}
					}

					if(!found)
					{
						entitiesToConceal.Add(entity);
					}
				}

				if(entitiesToConceal.Count > 0)
					ConcealEntities(entitiesToConceal);

				if ((DateTime.Now - start).TotalMilliseconds > 2000)
					Logging.WriteLineAndConsole(string.Format("Completed Conceal Check: {0}ms", (DateTime.Now - start).TotalMilliseconds));

			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndConceal(): {0}", ex.ToString()));
			}
			finally
			{
				m_checkConceal = false;
			}
		}

		private static bool CheckConcealBlockRules(IMyCubeGrid grid, List<IMyPlayer> players)
		{
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();

			// Live dangerously
			grid.GetBlocks(blocks, x => x.FatBlock != null);
			//CubeGrids.GetAllConnectedBlocks(m_processedGrids, grid, blocks, x => x.FatBlock != null);

			int beaconCount = 0;
			//bool found = false;
			//bool powered = false;
			foreach (IMySlimBlock block in blocks)
			{
				IMyCubeBlock cubeBlock = block.FatBlock;

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon))
				{
					MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)cubeBlock.GetObjectBuilderCubeBlock();
					beaconCount++;
					// Keep this return here, as 4 beacons always means true
					if(beaconCount >= 4)
					{
						return true;
					}

					if (!beacon.Enabled)
						continue;

					Sandbox.ModAPI.Ingame.IMyTerminalBlock terminalBlock = (Sandbox.ModAPI.Ingame.IMyTerminalBlock)cubeBlock;
//					Console.WriteLine("Found: {0} {1} {2}", beacon.BroadcastRadius, terminalBlock.IsWorking, terminalBlock.IsFunctional);
					//if (!terminalBlock.IsWorking)
					//{
//						continue;
					//}

					foreach(IMyPlayer player in players)
					{
						double distance = 0d;
						if(GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < beacon.BroadcastRadius)
							{
//								Console.WriteLine("Not concealed due to broadcast radius");
								//found = true;
								//break;
								return true;
							}
						}
					}
				}

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_RadioAntenna))
				{
					MyObjectBuilder_RadioAntenna antenna = (MyObjectBuilder_RadioAntenna)cubeBlock.GetObjectBuilderCubeBlock();

					if (!antenna.Enabled)
						continue;

					Sandbox.ModAPI.Ingame.IMyTerminalBlock terminalBlock = (Sandbox.ModAPI.Ingame.IMyTerminalBlock)cubeBlock;
					//if (!terminalBlock.IsWorking)
					//	continue;

					foreach (IMyPlayer player in players)
					{
						double distance = 0d;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < antenna.BroadcastRadius)
							{
//								Console.WriteLine("Not concealed due to antenna broadcast radius");
								//found = true;
								//break;
								return true;
							}
						}
					}
				}

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MedicalRoom))
				{
					MyObjectBuilder_MedicalRoom medical = (MyObjectBuilder_MedicalRoom)cubeBlock.GetObjectBuilderCubeBlock();

					if (!medical.Enabled)
						continue;

					Sandbox.ModAPI.Ingame.IMyTerminalBlock terminalBlock = (Sandbox.ModAPI.Ingame.IMyTerminalBlock)cubeBlock;
					//if (!terminalBlock.IsWorking)
					//	continue;

					if(PluginSettings.Instance.DynamicConcealIncludeMedBays)
					{
						foreach (ulong connectedPlayer in PlayerManager.Instance.ConnectedPlayers)
						{
							//if (PlayerMap.Instance.GetPlayerIdsFromSteamId(connectedPlayer).Count < 1)
								//continue;

							//long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId(connectedPlayer).First();
							long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(connectedPlayer);
							if (medical.Owner == playerId || (medical.ShareMode == MyOwnershipShareModeEnum.Faction && Player.CheckPlayerSameFaction(medical.Owner, playerId)))
							{
								return true;
							}
						}
					}
					else
					{
						return true;
					}
				}

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Refinery) || cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Assembler))
				{
					MyObjectBuilder_ProductionBlock production = (MyObjectBuilder_ProductionBlock)cubeBlock.GetObjectBuilderCubeBlock();
					if (!production.Enabled)
						continue;

					Sandbox.ModAPI.Ingame.IMyProductionBlock productionBlock = (Sandbox.ModAPI.Ingame.IMyProductionBlock)cubeBlock;
					if (productionBlock.IsProducing)
						return true;
				}

				foreach(string subType in PluginSettings.Instance.DynamicConcealIgnoreSubTypeList)
				{
					if (cubeBlock.BlockDefinition.SubtypeName.Contains(subType))
					{
//						Console.WriteLine("Not concealed due subtype");
						//found = true;
						return true;
					}
				}
			}

			return false;
		}

		private static void ConcealEntities(HashSet<IMyEntity> entitesToConceal)
		{
			foreach (IMyEntity entity in entitesToConceal)
			{
				ConcealEntity(entity);
			}

			Logging.WriteLineAndConsole(string.Format("Concealed {0} entities.", entitesToConceal.Count));
		}

		private static void ConcealEntity(IMyEntity entity)
		{
			Wrapper.GameAction(() =>
			{
				int pos = 0;
				try
				{
					if (!entity.InScene)
						return;

					MyObjectBuilder_CubeGrid builder = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entity);
					if (builder == null)
						return;

					pos = 1;
					IMyCubeGrid grid = (IMyCubeGrid)entity;
					long ownerId = 0;
					string ownerName = "";
					if (CubeGrids.GetOwner(builder, out ownerId))
					{
						//ownerId = grid.BigOwners.First();
						ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
					}

					pos = 2;
					if (entity.Physics != null)
					{
						entity.Physics.LinearVelocity = Vector3.Zero;
						entity.Physics.AngularVelocity = Vector3.Zero;
					}

					/*
					entity.InScene = false;
					entity.CastShadows = false;
					entity.Visible = false;
					*/

					builder.PersistentFlags = MyPersistentEntityFlags2.None;
					MyAPIGateway.Entities.RemapObjectBuilder(builder);

					pos = 3;
					if (m_removedGrids.Contains(entity.EntityId))
					{
						Logging.WriteLineAndConsole("Conceal", string.Format("Concealing - Id: {0} DUPE FOUND - Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName, ownerId, ownerName, builder.EntityId));
						BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
					}
					else
					{
						pos = 4;
						Logging.WriteLineAndConsole("Conceal", string.Format("Start Concealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName, ownerId, ownerName, builder.EntityId));

						IMyEntity newEntity = MyAPIGateway.Entities.CreateFromObjectBuilder(builder);
						if (newEntity == null)
						{
							Logging.WriteLineAndConsole("Conceal", string.Format("Issue - CreateFromObjectBuilder failed: {0}", newEntity.EntityId));
							return;
						}

						pos = 5;
						m_removedGrids.Add(entity.EntityId);
						BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
						MyAPIGateway.Entities.AddEntity(newEntity, false);
						Logging.WriteLineAndConsole("Conceal", string.Format("End Concealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName, ownerId, ownerName, builder.EntityId));
						pos = 6;
					}
				}
				catch (Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("ConcealEntity({1}): {0}", ex.ToString(), pos));
				}
			});
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
				Logging.WriteLineAndConsole("GetDistanceBetweenGridAndPlayer(): {0}", ex.ToString());
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
				Logging.WriteLineAndConsole("GetDistanceBetweenPointAndPlayer(): {0}", ex.ToString());
				return false;
			}

			return true;
		}

		public static void CheckAndRevealEntities()
		{
			if(m_checkReveal)
				return;

			m_checkReveal = true;
			try
			{
				DateTime start = DateTime.Now;

				List<IMyPlayer> players = new List<IMyPlayer>();
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				//Wrapper.GameAction(() =>
				//{
				MyAPIGateway.Players.GetPlayers(players);
				MyAPIGateway.Entities.GetEntities(entities);
				//});

				Dictionary<IMyEntity, string> entitiesToReveal = new Dictionary<IMyEntity, string>();
				string currentReason = "";
				//HashSet<IMyEntity> entitiesToReveal = new HashSet<IMyEntity>();
				foreach (IMyEntity entity in entities)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if (entity.InScene)
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					bool found = false;
					currentReason = "";
					foreach (IMyPlayer player in players)
					{
						double distance = 0f;
						if (GetDistanceBetweenGridAndPlayer(grid, player, out distance))
						{
							if (distance < PluginSettings.Instance.DynamicConcealDistance)
							{
								found = true;
								currentReason = string.Format("{0} distance to grid: {1}", player.DisplayName, distance);
							}
						}
					}

					if (!found)
					{
						if (CheckRevealBlockRules(grid, players, out currentReason))
						{
							found = true;
						}
					}

					if (found)
					{
						entitiesToReveal.Add(entity, currentReason);
					}
				}


				if (entitiesToReveal.Count > 0)
					RevealEntities(entitiesToReveal);

				if ((DateTime.Now - start).TotalMilliseconds > 2000)
					Logging.WriteLineAndConsole(string.Format("Completed Reveal Check: {0}ms", (DateTime.Now - start).TotalMilliseconds));
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndReveal(): {0}", ex.ToString()));
			}
			finally
			{
				m_checkReveal = false;
			}
		}

		private static bool CheckRevealBlockRules(IMyCubeGrid grid, List<IMyPlayer> players, out string reason)
		{
			reason = "";			
			// This is actually faster, but doesn't include power checks
			
			// Live dangerously
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			grid.GetBlocks(blocks, x => x.FatBlock != null);
			//CubeGrids.GetAllConnectedBlocks(m_processedGrids, grid, blocks, x => x.FatBlock != null);
			//bool found = false;
			//bool powered = false;
			foreach (IMySlimBlock block in blocks)
			{
				IMyCubeBlock cubeBlock = block.FatBlock;

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon))
				{
					MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)cubeBlock.GetObjectBuilderCubeBlock();
					if (!beacon.Enabled)
						continue;

					//Sandbox.ModAPI.Ingame.IMyFunctionalBlock functionalBlock = (Sandbox.ModAPI.Ingame.IMyFunctionalBlock)cubeBlock;
					//if (!functionalBlock.Enabled)
					//	continue;

					//Console.WriteLine("Beacon: {0} {1} {2}", beacon.BroadcastRadius, terminalBlock.IsWorking, terminalBlock.IsFunctional);
					//if (!terminalBlock.IsWorking)
					//	continue;

					
					foreach (IMyPlayer player in players)
					{
						double distance = 0d;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < beacon.BroadcastRadius)
							{
								//found = true;
								//break;
								reason = string.Format("{0} distance to beacon broadcast: {1}", player.DisplayName, distance);
								return true;
							}
						}
					}				
				}

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_RadioAntenna))
				{
					MyObjectBuilder_RadioAntenna antenna = (MyObjectBuilder_RadioAntenna)cubeBlock.GetObjectBuilderCubeBlock();

					if (!antenna.Enabled)
						continue;

					//Sandbox.ModAPI.Ingame.IMyFunctionalBlock functionalBlock = (Sandbox.ModAPI.Ingame.IMyFunctionalBlock)cubeBlock;
					//if (!functionalBlock.Enabled)
					//	continue;

					foreach (IMyPlayer player in players)
					{
						double distance = 0d;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < antenna.BroadcastRadius)
							{
								//found = true;
								//break;
								reason = string.Format("{0} distance to antenna broadcast: {1}", player.DisplayName, distance);
								return true;
							}
						}
					}
				}

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MedicalRoom))
				{
					MyObjectBuilder_MedicalRoom medical = (MyObjectBuilder_MedicalRoom)cubeBlock.GetObjectBuilderCubeBlock();
					if (!medical.Enabled)
						continue;

					//Sandbox.ModAPI.Ingame.IMyFunctionalBlock functionalBlock = (Sandbox.ModAPI.Ingame.IMyFunctionalBlock)cubeBlock;
					//if (!functionalBlock.Enabled)
					//	continue;
					
					if (PluginSettings.Instance.DynamicConcealIncludeMedBays)
					{
						foreach (ulong connectedPlayer in PlayerManager.Instance.ConnectedPlayers)
						{
							long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(connectedPlayer);
							if (medical.Owner == playerId || (medical.ShareMode == MyOwnershipShareModeEnum.Faction && Player.CheckPlayerSameFaction(medical.Owner, playerId)))
							//if (functionalBlock.OwnerId == playerId || (functionalBlock.GetUserRelationToOwner(playerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.FactionShare))
							{
								reason = string.Format("Grid has medbay and player is logged in - playerid: {0}", playerId);
								return true;
							}
						}
					}
					else
					{
						reason = string.Format("Grid has medbay and conceal can not include medbays");
						return true;
					}
				}

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_ProductionBlock))
				{
					MyObjectBuilder_ProductionBlock production = (MyObjectBuilder_ProductionBlock)cubeBlock.GetObjectBuilderCubeBlock();
					if (!production.Enabled)
						continue;

					Sandbox.ModAPI.Ingame.IMyProductionBlock productionBlock = (Sandbox.ModAPI.Ingame.IMyProductionBlock)cubeBlock;

					if (production.Queue.Length > 0)
					{
						reason = string.Format("Grid has production facility that has a queue");
						return true;
					}
				}
			}

			return false;
		}

		private static void RevealEntities(Dictionary<IMyEntity, string> entitiesToReveal)
		{
			foreach (KeyValuePair<IMyEntity, string> entity in entitiesToReveal)
			{
				RevealEntity(entity);
			}

			Logging.WriteLineAndConsole(string.Format("Revealed {0} entities.", entitiesToReveal.Count));
		}

		private static void RevealEntity(KeyValuePair<IMyEntity, string> item)
		{
			IMyEntity entity = item.Key;
			string reason = item.Value;
			Wrapper.GameAction(() =>
			{
				MyObjectBuilder_CubeGrid builder = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entity);
				if (builder == null)
					return;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				long ownerId = 0;
				string ownerName = "";
				if (CubeGrids.GetBigOwners(builder).Count > 0)
				{
					ownerId = CubeGrids.GetBigOwners(builder).First();
					ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
				}
				/*
				entity.InScene = true;
				entity.CastShadows = true;
				entity.Visible = true;
				*/

				builder.PersistentFlags = (MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.CastShadows);
				MyAPIGateway.Entities.RemapObjectBuilder(builder);

				if(m_removedGrids.Contains(entity.EntityId))
				{
					Logging.WriteLineAndConsole("Conceal", string.Format("Revealing - Id: {0} DUPE FOUND Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, reason));
					BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
				}
				else
				{
					Logging.WriteLineAndConsole("Conceal", string.Format("Start Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, reason));
//					builder.PositionAndOrientation = new MyPositionAndOrientation(new Vector3D(Math.Round(builder.PositionAndOrientation.Value.Position.X, 0), Math.Round(builder.PositionAndOrientation.Value.Position.Y, 0), Math.Round(builder.PositionAndOrientation.Value.Position.Z, 0)), builder.PositionAndOrientation.Value.Forward, builder.PositionAndOrientation.Value.Up);
					//builder.PositionAndOrientation = new MyPositionAndOrientation(new Vector3(builder.PositionAndOrientation.Value.Position.X, builder.PositionAndOrientation.Value.Position.Y, builder.PositionAndOrientation.Value.Position.Z), builder.PositionAndOrientation.Value.Forward, builder.PositionAndOrientation.Value.Up);
					IMyEntity newEntity = MyAPIGateway.Entities.CreateFromObjectBuilder(builder);
					if (newEntity == null)
					{
						Logging.WriteLineAndConsole("Conceal", string.Format("Issue - CreateFromObjectBuilder failed: {0}", newEntity.EntityId));
						return;
					}

					m_removedGrids.Add(entity.EntityId);
					BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
					MyAPIGateway.Entities.AddEntity(newEntity, true);
					List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>();
					addList.Add(builder);
					MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
					Logging.WriteLineAndConsole("Conceal", string.Format("End Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, reason));
				}
			});
		}

		static public void RevealAll()
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities);
			});

			List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>();
			int count = 0;
			Wrapper.GameAction(() =>
			{
				foreach (IMyEntity entity in entities)
				{
					if (entity.InScene)
						continue;

					if (!(entity is IMyCubeGrid))
						continue;

					MyObjectBuilder_CubeGrid builder = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entity);
					if (builder == null)
						continue;

					count++;
					IMyCubeGrid grid = (IMyCubeGrid)entity;
					long ownerId = 0;
					string ownerName = "";
					if (CubeGrids.GetBigOwners(builder).Count > 0)
					{
						ownerId = CubeGrids.GetBigOwners(builder).First();
						ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
					}

					//grid.PersistentFlags = (MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.CastShadows);
					//grid.InScene = true;
					//grid.CastShadows = true;
					builder.PersistentFlags = (MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.CastShadows);
					MyAPIGateway.Entities.RemapObjectBuilder(builder);
					Logging.WriteLineAndConsole("Conceal", string.Format("Force Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, builder.EntityId));

					IMyEntity newEntity = MyAPIGateway.Entities.CreateFromObjectBuilder(builder);
					if (newEntity == null)
					{
						Logging.WriteLineAndConsole("Conceal", string.Format("Issue - CreateFromObjectBuilder failed: {0}", newEntity.EntityId));
						continue;
					}

					BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
					MyAPIGateway.Entities.AddEntity(newEntity, true);
					addList.Add(builder);
					MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
					addList.Clear();
				}
			});

			Logging.WriteLineAndConsole(string.Format("Revealed {0} grids", count));
		}

		public static void CheckAndDisableTurrets()
		{
			try
			{
				DateTime start = DateTime.Now;
				List<IMyEntity> turretList = new List<IMyEntity>();
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				MyAPIGateway.Entities.GetEntities(entities);
				m_turretsEnabled = 0;
				m_turretsDisabled = 0;
				m_turretsToggled = 0;
				foreach (IMyEntity entity in entities)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if (!entity.InScene)
						continue;

					turretList.AddList(DisableTurretsWithoutTargets(entity));
				}

				if (turretList.Count > 0)
				{
					Wrapper.GameAction(() =>
					{
						foreach (IMyEntity entity in turretList)
						{
							FunctionalBlockEntity.SetState(entity, false);
						}
					});
				}

				if (m_turretsToggled > 0 || DateTime.Now - start > TimeSpan.FromSeconds(1))
				{
					Logging.WriteLineAndConsole(string.Format("Disable: {0} turrets enabled.  {1} turrets diabled.  {2} turrets toggled. ({3} ms)", m_turretsEnabled, m_turretsDisabled, m_turretsToggled, (DateTime.Now - start).TotalMilliseconds));
				}
			}
			catch (Exception ex)			
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndDisableTurrets(): {0}", ex.ToString()));
			}
		}

		private static List<IMyEntity> DisableTurretsWithoutTargets(IMyEntity entity)
		{
			List<IMyEntity> turretList = new List<IMyEntity>();
			if (!(entity is IMyCubeGrid))
				return turretList;

			IMyCubeGrid grid = (IMyCubeGrid)entity;
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			grid.GetBlocks(blocks);
			//bool disable = false;
			bool ignore = false;
			foreach (IMySlimBlock block in blocks)
			{
				if (block.FatBlock == null)
					continue;

				if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_InteriorTurret) ||
				   block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_LargeGatlingTurret) ||
				   block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_LargeMissileTurret))
				{
					IMyEntity turret = block.FatBlock;
					bool state = FunctionalBlockEntity.GetState(turret);

					if (state)
						m_turretsEnabled++;
					else
						m_turretsDisabled++;

					if (state && !ignore)
					{
						// No target, so we're not going to enable anything on this grid
						//if (!disable && DoesGridHaveTarget(grid, block))
						if (DoesGridHaveTarget(grid, block))
						{
							// We'll ignore state and only collect for statistics
							ignore = true;
							continue;
						}

//						Console.WriteLine("Disabling");
						//disable = true;
						
						if (PluginSettings.Instance.DynamicTurretAllowExemption)
						{
							IMyFunctionalBlock functional = (IMyFunctionalBlock)turret;
							if (functional.CustomName.ToLower().Contains("[manualcontrol]"))
							{
								continue;
							}
						}
						
						m_turretsToggled++;
						turretList.Add(turret);
					}
				}
			}

			return turretList;
		}

		public static void CheckAndEnableTurrets()
		{
			try
			{
				DateTime start = DateTime.Now;

				List<IMyEntity> turretList = new List<IMyEntity>();
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				MyAPIGateway.Entities.GetEntities(entities);
				m_turretsEnabled = 0;
				m_turretsDisabled = 0;
				m_turretsToggled = 0;
				foreach (IMyEntity entity in entities)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if (!entity.InScene)
						continue;

					turretList.AddList(EnableTurretsWithTargets(entity));
				}

				if (turretList.Count > 0)
				{
					Wrapper.GameAction(() =>
					{
						foreach (IMyEntity entity in turretList)
						{
							FunctionalBlockEntity.SetState(entity, true);
						}
					});
				}

				if (m_turretsToggled > 0 || DateTime.Now - start > TimeSpan.FromSeconds(1))
				{
					Logging.WriteLineAndConsole(string.Format("Enable: {0} turrets enabled.  {1} turrets diabled.  {2} turrets toggled. ({3} ms)", m_turretsEnabled, m_turretsDisabled, m_turretsToggled, (DateTime.Now - start).TotalMilliseconds));
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndDisableTurrets(): {0}", ex.ToString()));
			}
		}

		private static List<IMyEntity> EnableTurretsWithTargets(IMyEntity entity)
		{
			List<IMyEntity> turretList = new List<IMyEntity>();

			if (!(entity is IMyCubeGrid))
				return turretList;

			IMyCubeGrid grid = (IMyCubeGrid)entity;
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			m_scanCache.Clear();
			grid.GetBlocks(blocks);
			//bool enable = false;
			bool ignore = false;
			foreach (IMySlimBlock block in blocks)
			{
				if (block.FatBlock == null)
					continue;

				if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_InteriorTurret) ||
				   block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_LargeGatlingTurret) ||
				   block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_LargeMissileTurret))
				{
					IMyEntity turret = block.FatBlock;
					bool state = FunctionalBlockEntity.GetState(turret);

					if (state)
						m_turretsEnabled++;
					else
						m_turretsDisabled++;

					if (!state && !ignore)
					{
						// No target, so we're not going to disable anything on this grid
						//if (!enable && !DoesGridHaveTarget(grid, block))
						if (!DoesGridHaveTarget(grid, block))
						{
							// We'll ignore state check and only collect for statistics
//							Console.WriteLine("Ignore");
							ignore = true;
							continue;
						}

//						Console.WriteLine("Enabling");
						///enable = true;
						
						if (PluginSettings.Instance.DynamicTurretAllowExemption)
						{
							IMyFunctionalBlock functional = (IMyFunctionalBlock)turret;
							if (functional.CustomName.ToLower().Contains("[manualcontrol]"))
							{
								continue;
							}
						}
						
						m_turretsToggled++;
						turretList.Add(turret);
					}
				}
			}

			return turretList;
		}

		private static bool DoesGridHaveTarget(IMyCubeGrid grid, IMySlimBlock block)
		{
			if (m_scanCache.Count < 1)
			{
				BoundingSphereD sphere = new BoundingSphereD(grid.GetPosition(), 2000);
				m_scanCache = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
			}
			/*
			HashSet<IMyEntity> testEntities = new HashSet<IMyEntity>();
			try
			{
				MyAPIGateway.Entities.GetEntities(testEntities);
			}
			catch
			{
				return false;
			}
			*/
			//bool found = false;
			foreach (IMyEntity testEntity in m_scanCache)
			{
				if ((IMyEntity)grid == testEntity)
					continue;

				if (!testEntity.InScene)
					continue;

				if (testEntity is IMyCubeBlock)
					continue;

				if (!(testEntity is IMyControllableEntity))
				{
					continue;
				}

				if (testEntity is IMyCubeGrid)
				{
					IMyCubeGrid testGrid = (IMyCubeGrid)testEntity;
					// Always enable if grid has no owner.  Seems suspect.  Might be a user trying to abuse a no ownership ship.
					if (testGrid.BigOwners.Count < 1 && testGrid.SmallOwners.Count < 1)
						return true;

					foreach (long owner in testGrid.BigOwners)
					{
						if (block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies ||
						    block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral)
						{
							return true;
						}
					}
				}
				else
				{
					var builderBase = testEntity.GetObjectBuilder();
					if (builderBase is MyObjectBuilder_Character)
					{
						IMyPlayer player = null;
						try
						{
							List<IMyPlayer> players = new List<IMyPlayer>();
							MyAPIGateway.Players.GetPlayers(players);
							player = players.FirstOrDefault(x => x.DisplayName == testEntity.DisplayName);
						}
						catch
						{

						}

						if (player == null)
							continue;

						long playerId = player.PlayerID;

						if (block.FatBlock.GetUserRelationToOwner(playerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies ||
						    block.FatBlock.GetUserRelationToOwner(playerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral)
						{
							return true;
						}
					}
				}
			}

			return false;
		}
	}
}
