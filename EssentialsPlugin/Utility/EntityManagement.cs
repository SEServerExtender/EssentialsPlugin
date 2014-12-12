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

namespace EssentialsPlugin.Utility
{
	public class EntityManagement
	{
		private static HashSet<IMyEntity> m_processedGrids = new HashSet<IMyEntity>();
		public static void CheckAndConcealEntities()
		{
			try
			{
				DateTime start = DateTime.Now;

				m_processedGrids.Clear();
				List<IMyPlayer> players = new List<IMyPlayer>();
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>();

				//Wrapper.GameAction(() =>
				//{
					MyAPIGateway.Players.GetPlayers(players);
					MyAPIGateway.Entities.GetEntities(entities);

					//CubeGrids.GetConnectedGrids(entities);
				//});

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
						// Check for beacon / antenna ranges
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
		}

		private static bool CheckConcealBlockRules(IMyCubeGrid grid, List<IMyPlayer> players)
		{
			/*
			MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);

			if (gridBuilder == null)
				return false;

			if (!CubeGrids.DoesGridHavePowerSupply(gridBuilder) && !CubeGrids.DoesGridHaveFourBeacons(gridBuilder))
				return false;

			int beaconCount = 0;
			foreach (MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
			{
				if (block is MyObjectBuilder_Beacon)
				{
					beaconCount++;
					// Keep this return here, as 4 beacons always means true
					if (beaconCount >= 4)
					{
						return true;
					}

					MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)block;
					if (!beacon.Enabled)
						continue;

					foreach (IMyPlayer player in players)
					{
						double distance = 0d;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < beacon.BroadcastRadius)
							{
								return true;
							}
						}
					}
				}

				if (block is MyObjectBuilder_RadioAntenna)
				{
					MyObjectBuilder_RadioAntenna antenna = (MyObjectBuilder_RadioAntenna)block;

					if (!antenna.Enabled)
						continue;

					foreach (IMyPlayer player in players)
					{
						double distance = 0d;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < antenna.BroadcastRadius)
							{
								return true;
							}
						}
					}
				}

				if (block is MyObjectBuilder_MedicalRoom)
				{
					MyObjectBuilder_MedicalRoom medical = (MyObjectBuilder_MedicalRoom)block;

					if (!medical.Enabled)
						continue;

					if (PluginSettings.Instance.ConcealIncludeMedBays)
					{
						foreach (ulong connectedPlayer in PlayerManager.Instance.ConnectedPlayers)
						{
							if (PlayerMap.Instance.GetPlayerIdsFromSteamId(connectedPlayer).Count < 1)
								continue;

							long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId(connectedPlayer).First();
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

				if (block is MyObjectBuilder_ProductionBlock)
				{
					MyObjectBuilder_ProductionBlock production = (MyObjectBuilder_ProductionBlock)block;
					if (!production.Enabled)
						continue;

					IMyEntity cubeBlock = null;
					if (!MyAPIGateway.Entities.TryGetEntityById(block.EntityId, out cubeBlock))
						continue;

					Sandbox.ModAPI.Ingame.IMyProductionBlock productionBlock = (Sandbox.ModAPI.Ingame.IMyProductionBlock)cubeBlock;
					if (productionBlock.IsProducing)
					{
						//found = true;
						return true;
					}
				}

			}
			MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);
			if (gridBuilder == null)
				return false;

			if(!CubeGrids.DoesGridHavePowerSupply(gridBuilder) && !CubeGrids.DoesGridHaveFourBeacons(gridBuilder))
				return false;

			 */
 
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

				/*
				MyObjectBuilder_CubeBlock blockBuilder = null;
				try
				{
					blockBuilder = cubeBlock.GetObjectBuilderCubeBlock();
				}
				catch
				{
					continue;
				}
				 * 
				if (block.FatBlock.EntityId == 0)
					continue;

				if(CubeGrids.DoesBlockSupplyPower(gridBuilder.CubeBlocks.FirstOrDefault(x => x.EntityId == cubeBlock.EntityId)))
				{
					powered = true;
				}
				 * */
			}

			//if (found && powered)
			//	return true;
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
				if (!entity.InScene)
					return;

				MyObjectBuilder_CubeGrid builder = null;
				try
				{
					builder = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(true);
				}
				catch
				{
					return;
				}

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				long ownerId = 0;
				string ownerName = "";
				if (CubeGrids.GetOwner(builder, out ownerId))
				{
					//ownerId = grid.BigOwners.First();
					ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
				}

				entity.Physics.LinearVelocity = Vector3.Zero;
				entity.Physics.AngularVelocity = Vector3.Zero;

				/*
				entity.InScene = false;
				entity.CastShadows = false;
				entity.Visible = false;
				*/

				builder.PersistentFlags = MyPersistentEntityFlags2.None;
				MyAPIGateway.Entities.RemapObjectBuilder(builder);

				Logging.WriteLineAndConsole("Conceal", string.Format("Concealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName, ownerId, ownerName, builder.EntityId));

				BaseEntityNetworkManager.BroadcastRemoveEntity(entity);
				MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
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

				HashSet<IMyEntity> entitiesToReveal = new HashSet<IMyEntity>();
				foreach (IMyEntity entity in entities)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if(entity.InScene)
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					bool found = false;
					foreach (IMyPlayer player in players)
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

					if (!found)
					{
						if(CheckRevealBlockRules(grid, players))
						{
							found = true;
						}
					}

					if (found)
					{
						entitiesToReveal.Add(entity);
					}
				}


				if (entitiesToReveal.Count > 0)
					RevealEntities(entitiesToReveal);

				if((DateTime.Now - start).TotalMilliseconds > 1000)
					Logging.WriteLineAndConsole(string.Format("Completed Reveal Check: {0}ms", (DateTime.Now - start).TotalMilliseconds));
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndReveal(): {0}", ex.ToString()));
			}
		}

		private static bool CheckRevealBlockRules(IMyCubeGrid grid, List<IMyPlayer> players)
		{
			/*
			MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);
			
			if (gridBuilder == null)
				return false;

			if (!CubeGrids.DoesGridHavePowerSupply(gridBuilder) && !CubeGrids.DoesGridHaveFourBeacons(gridBuilder))
				return false;

			foreach (MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
			{
				if (block is MyObjectBuilder_Beacon)
				{
					MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)block;
					if (!beacon.Enabled)
						continue;

					foreach (IMyPlayer player in players)
					{
						double distance = 0d;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < beacon.BroadcastRadius)
							{
								return true;
							}
						}
					}
				}

				if (block is MyObjectBuilder_RadioAntenna)
				{
					MyObjectBuilder_RadioAntenna antenna = (MyObjectBuilder_RadioAntenna)block;

					if (!antenna.Enabled)
						continue;

					foreach (IMyPlayer player in players)
					{
						double distance = 0d;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < antenna.BroadcastRadius)
							{
								return true;
							}
						}
					}
				}

				if (block is MyObjectBuilder_MedicalRoom)
				{
					MyObjectBuilder_MedicalRoom medical = (MyObjectBuilder_MedicalRoom)block;

					if (!medical.Enabled)
						continue;

					if (PluginSettings.Instance.ConcealIncludeMedBays)
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
			}
		    */
			
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
								return true;
							}
						}
					}
					else
					{
						return true;
					}
				}

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_ProductionBlock))
				{
					MyObjectBuilder_ProductionBlock production = (MyObjectBuilder_ProductionBlock)cubeBlock.GetObjectBuilderCubeBlock();
					if (!production.Enabled)
						continue;

					Console.WriteLine("Here: {0} {1}", production.Enabled, production.Queue.Length);
					Sandbox.ModAPI.Ingame.IMyProductionBlock productionBlock = (Sandbox.ModAPI.Ingame.IMyProductionBlock)cubeBlock;

					if (production.Queue.Length > 0)
						return true;
				}
			}

			return false;
		}

		private static void RevealEntities(HashSet<IMyEntity> entitiesToReveal)
		{
			foreach (IMyEntity entity in entitiesToReveal)
			{
				RevealEntity(entity);
			}

			Logging.WriteLineAndConsole(string.Format("Revealed {0} entities.", entitiesToReveal.Count));
		}

		private static void RevealEntity(IMyEntity entity)
		{
			Wrapper.GameAction(() =>
			{
				MyObjectBuilder_CubeGrid builder = null;
				try
				{
					builder = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(true);
				}
				catch
				{
					return;
				}

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				long ownerId = 0;
				string ownerName = "";
				if (CubeGrids.GetOwner(builder, out ownerId))
				{
					//ownerId = grid.BigOwners.First();
					ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId(ownerId).Name;
				}

				builder.PersistentFlags = (MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.CastShadows);
				MyAPIGateway.Entities.RemapObjectBuilder(builder);
				Logging.WriteLineAndConsole("Conceal", string.Format("Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, builder.EntityId));

				/*
				entity.InScene = true;
				entity.CastShadows = true;
				entity.Visible = true;
				*/

				BaseEntityNetworkManager.BroadcastRemoveEntity(entity);
				MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
				List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>();
				addList.Add(builder);
				MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
			});
		}
	}
}
