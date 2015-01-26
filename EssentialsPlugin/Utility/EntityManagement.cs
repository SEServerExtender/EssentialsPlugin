using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

using EssentialsPlugin.ProcessHandler;

namespace EssentialsPlugin.Utility
{
	public class EntityManagement
	{
		private static volatile bool _checkReveal;
		private static volatile bool _checkConceal;
		private static readonly HashSet<IMyEntity> ProcessedGrids = new HashSet<IMyEntity>();
		private static readonly List<long> RemovedGrids = new List<long>();
		private static List<IMyEntity> _scanCache = new List<IMyEntity>();
		private static readonly List<IMyIdentity> IdentityCache = new List<IMyIdentity>();
		private static DateTime _lastIdentityUpdate = DateTime.Now.AddHours(-1);
		private static int _turretsEnabled;
		private static int _turretsDisabled;
		private static int _turretsToggled;

		public static void CheckAndConcealEntities()
		{
			if (_checkConceal)
				return;
			
			_checkConceal = true;
			try
			{
				DateTime start = DateTime.Now;
				double distCheck = 0d;
				double blockRules = 0d;
				double getGrids = 0d;
				double co = 0f;

				ProcessedGrids.Clear();
				List<IMyPlayer> players = new List<IMyPlayer>();
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				HashSet<IMyEntity> entitiesFiltered = new HashSet<IMyEntity>();
				HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>();

				try
				{
					MyAPIGateway.Players.GetPlayers(players);
				}
				catch(Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("Error getting players list.  Check and Conceal failed: {0}", ex));
					return;
				}

				try
				{
					MyAPIGateway.Entities.GetEntities(entities);
				}
				catch
				{
					Logging.WriteLineAndConsole("CheckAndConcealEntities(): Error getting entity list, skipping check");
					return;
				}

				foreach (IMyEntity entity in entities)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if (!entity.InScene)
						continue;

					entitiesFiltered.Add(entity);
				}
				
				DateTime getGridsStart = DateTime.Now;
				CubeGrids.GetGridsUnconnected(entitiesFound, entitiesFiltered);
				getGrids += (DateTime.Now - getGridsStart).TotalMilliseconds;

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
					DateTime distStart = DateTime.Now;
					foreach(IMyPlayer player in players)
					{
						double distance;
						if (GetDistanceBetweenGridAndPlayer(grid, player, out distance))
						{
							if (distance < PluginSettings.Instance.DynamicConcealDistance)
							{
								found = true;
							}

						}
					}
					distCheck += (DateTime.Now - distStart).TotalMilliseconds;

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
								
							}
						}
					}

					if(!found)
					{
						// Check for block type rules
						DateTime blockStart = DateTime.Now;
						if (CheckConcealBlockRules(grid, players))
						{
							found = true;
						}

						blockRules += (DateTime.Now - blockStart).TotalMilliseconds;
					}

					if(!found)
					{
						entitiesToConceal.Add(entity);
					}
				}

				DateTime coStart = DateTime.Now;
				if(entitiesToConceal.Count > 0)
					ConcealEntities(entitiesToConceal);
				co += (DateTime.Now - coStart).TotalMilliseconds;

				if ((DateTime.Now - start).TotalMilliseconds > 2000)
					Logging.WriteLineAndConsole(string.Format("Completed Conceal Check: {0}ms (gg: {3}, dc: {2} ms, br: {1}ms, co: {4}ms)", (DateTime.Now - start).TotalMilliseconds, blockRules, distCheck, getGrids, co));

			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndConceal(): {0}", ex));
			}
			finally
			{
				_checkConceal = false;
			}
		}

		private static bool CheckConcealBlockRules(IMyCubeGrid grid, List<IMyPlayer> players)
		{
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();

			// Live dangerously
			grid.GetBlocks(blocks, x => x.FatBlock != null);
			//CubeGrids.GetAllConnectedBlocks(_processedGrids, grid, blocks, x => x.FatBlock != null);

			int beaconCount = 0;
			//bool found = false;
			//bool powered = false;
			foreach (IMySlimBlock block in blocks)
			{
				IMyCubeBlock cubeBlock = block.FatBlock;

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon))
				{
					Sandbox.ModAPI.Ingame.IMyBeacon beacon = (Sandbox.ModAPI.Ingame.IMyBeacon)cubeBlock;					
					//MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)cubeBlock.GetObjectBuilderCubeBlock();
					beaconCount++;
					// Keep this return here, as 4 beacons always means true
					if(beaconCount >= 4)
					{
						return true;
					}

					if (!beacon.Enabled)
						continue;

					//Sandbox.ModAPI.Ingame.IMyTerminalBlock terminalBlock = (Sandbox.ModAPI.Ingame.IMyTerminalBlock)cubeBlock;
					//Console.WriteLine( "Found: {0} {1} {2}", beacon.BroadcastRadius, terminalBlock.IsWorking, terminalBlock.IsFunctional );
					//if (!terminalBlock.IsWorking)
					//{
//						continue;
					//}

					foreach(IMyPlayer player in players)
					{
						double distance;
						if(GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < beacon.Radius)
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
					//MyObjectBuilder_RadioAntenna antenna = (MyObjectBuilder_RadioAntenna)cubeBlock.GetObjectBuilderCubeBlock();
					Sandbox.ModAPI.Ingame.IMyRadioAntenna antenna = (Sandbox.ModAPI.Ingame.IMyRadioAntenna)cubeBlock;

					if (!antenna.Enabled)
						continue;

					//Sandbox.ModAPI.Ingame.IMyTerminalBlock terminalBlock = (Sandbox.ModAPI.Ingame.IMyTerminalBlock)cubeBlock;
					//if (!terminalBlock.IsWorking)
					//	continue;

					foreach (IMyPlayer player in players)
					{
						double distance;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < antenna.Radius)
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
					//MyObjectBuilder_MedicalRoom medical = (MyObjectBuilder_MedicalRoom)cubeBlock.GetObjectBuilderCubeBlock();
					Sandbox.ModAPI.Ingame.IMyMedicalRoom medical = (Sandbox.ModAPI.Ingame.IMyMedicalRoom)cubeBlock;
					
					if (!medical.Enabled)
						continue;

					//Sandbox.ModAPI.Ingame.IMyTerminalBlock terminalBlock = (Sandbox.ModAPI.Ingame.IMyTerminalBlock)cubeBlock;
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
							//if (medical.Owner == playerId || (medical.ShareMode == MyOwnershipShareModeEnum.Faction && Player.CheckPlayerSameFaction(medical.Owner, playerId)))
							if(medical.HasPlayerAccess(playerId))
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
					//MyObjectBuilder_ProductionBlock production = (MyObjectBuilder_ProductionBlock)cubeBlock.GetObjectBuilderCubeBlock();
					Sandbox.ModAPI.Ingame.IMyProductionBlock production = (Sandbox.ModAPI.Ingame.IMyProductionBlock)cubeBlock;
					if (!production.Enabled)
						continue;

					if (production.IsProducing)
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

		private static bool CheckConcealForce(IMyCubeGrid grid, ulong steamId)
		{
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();

			// Live dangerously
			grid.GetBlocks(blocks, x => x.FatBlock != null);
			foreach (IMySlimBlock block in blocks)
			{
				IMyCubeBlock cubeBlock = block.FatBlock;

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MedicalRoom))
				{
					MyObjectBuilder_MedicalRoom medical = (MyObjectBuilder_MedicalRoom)cubeBlock.GetObjectBuilderCubeBlock();

					if (!medical.Enabled)
						continue;

					Sandbox.ModAPI.Ingame.IMyTerminalBlock terminalBlock = (Sandbox.ModAPI.Ingame.IMyTerminalBlock)cubeBlock;
					long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(steamId);
					if (medical.Owner == playerId || (medical.ShareMode == MyOwnershipShareModeEnum.Faction && Player.CheckPlayerSameFaction(medical.Owner, playerId)))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static void ConcealEntities(HashSet<IMyEntity> entitesToConceal)
		{
			Wrapper.GameAction(() =>
			{
				foreach (IMyEntity entity in entitesToConceal)
				{
					ConcealEntity(entity);
				}
			});

			Logging.WriteLineAndConsole(string.Format("Concealed {0} entities.", entitesToConceal.Count));
		}

		private static void ConcealEntity(IMyEntity entity)
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
				//IMyCubeGrid grid = (IMyCubeGrid)entity;
				long ownerId;
				string ownerName = string.Empty;
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
				if (RemovedGrids.Contains(entity.EntityId))
				{
					Logging.WriteLineAndConsole("Conceal", string.Format("Concealing - Id: {0} DUPE FOUND - Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName, ownerId, ownerName));
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
					RemovedGrids.Add(entity.EntityId);
					MyAPIGateway.Entities.RemoveEntity(entity);
					BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
					MyAPIGateway.Entities.AddEntity(newEntity, false);
					Logging.WriteLineAndConsole("Conceal", string.Format("End Concealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName, ownerId, ownerName, builder.EntityId));
					pos = 6;
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("ConcealEntity({1}): {0}", ex, pos));
			}
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
			if(_checkReveal)
				return;

			_checkReveal = true;
			try
			{
				DateTime start = DateTime.Now;
				double br = 0f;
				double re = 0f;

				List<IMyPlayer> players = new List<IMyPlayer>();
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				//Wrapper.GameAction(() =>
				//{
				MyAPIGateway.Players.GetPlayers(players);
				MyAPIGateway.Entities.GetEntities(entities);
				//});

				Dictionary<IMyEntity, string> entitiesToReveal = new Dictionary<IMyEntity, string>();
				//HashSet<IMyEntity> entitiesToReveal = new HashSet<IMyEntity>();
				foreach (IMyEntity entity in entities)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if (entity.InScene)
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					bool found = false;
					string currentReason = string.Empty;
					foreach (IMyPlayer player in players)
					{
						double distance;
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
						DateTime brStart = DateTime.Now;
						if (CheckRevealBlockRules(grid, players, out currentReason))
						{
							found = true;
						}
						br += (DateTime.Now - brStart).TotalMilliseconds;
					}

					if (found)
					{
						entitiesToReveal.Add(entity, currentReason);
					}
				}

				DateTime reStart = DateTime.Now;
				if (entitiesToReveal.Count > 0)
					RevealEntities(entitiesToReveal);
				re += (DateTime.Now - reStart).TotalMilliseconds;

				if ((DateTime.Now - start).TotalMilliseconds > 2000)
					Logging.WriteLineAndConsole(string.Format("Completed Reveal Check: {0}ms (br: {1}ms, re: {2}ms)", (DateTime.Now - start).TotalMilliseconds, br, re));
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndReveal(): {0}", ex));
			}
			finally
			{
				_checkReveal = false;
			}
		}

		private static bool CheckRevealBlockRules(IMyCubeGrid grid, List<IMyPlayer> players, out string reason)
		{
			reason = "";			
			// This is actually faster, but doesn't include power checks
			
			// Live dangerously
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			grid.GetBlocks(blocks, x => x.FatBlock != null);
			//CubeGrids.GetAllConnectedBlocks(_processedGrids, grid, blocks, x => x.FatBlock != null);
			//bool found = false;
			//bool powered = false;
			foreach (IMySlimBlock block in blocks)
			{
				IMyCubeBlock cubeBlock = block.FatBlock;

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon))
				{
					//MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)cubeBlock.GetObjectBuilderCubeBlock();
					Sandbox.ModAPI.Ingame.IMyBeacon beacon = (Sandbox.ModAPI.Ingame.IMyBeacon)cubeBlock;
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
						double distance;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < beacon.Radius)
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
					//MyObjectBuilder_RadioAntenna antenna = (MyObjectBuilder_RadioAntenna)cubeBlock.GetObjectBuilderCubeBlock();
					Sandbox.ModAPI.Ingame.IMyRadioAntenna antenna = (Sandbox.ModAPI.Ingame.IMyRadioAntenna)cubeBlock;

					if (!antenna.Enabled)
						continue;

					//Sandbox.ModAPI.Ingame.IMyFunctionalBlock functionalBlock = (Sandbox.ModAPI.Ingame.IMyFunctionalBlock)cubeBlock;
					//if (!functionalBlock.Enabled)
					//	continue;

					foreach (IMyPlayer player in players)
					{
						double distance;
						if (GetDistanceBetweenPointAndPlayer(grid.GetPosition(), player, out distance))
						{
							if (distance < antenna.Radius)
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
					//MyObjectBuilder_MedicalRoom medical = (MyObjectBuilder_MedicalRoom)cubeBlock.GetObjectBuilderCubeBlock();
					Sandbox.ModAPI.Ingame.IMyMedicalRoom medical = (Sandbox.ModAPI.Ingame.IMyMedicalRoom)cubeBlock;
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
							//if (medical.Owner == playerId || (medical.ShareMode == MyOwnershipShareModeEnum.Faction && Player.CheckPlayerSameFaction(medical.Owner, playerId)))
							//if (functionalBlock.OwnerId == playerId || (functionalBlock.GetUserRelationToOwner(playerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.FactionShare))
							if(medical.HasPlayerAccess(playerId))
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

					if (production.Queue.Length > 0)
					{
						reason = string.Format("Grid has production facility that has a queue");
						return true;
					}
				}
			}

			return false;
		}

		private static bool CheckRevealMedbay(IMyCubeGrid grid, ulong steamId)
		{
			// Live dangerously
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			grid.GetBlocks(blocks, x => x.FatBlock != null);
			foreach (IMySlimBlock block in blocks)
			{
				IMyCubeBlock cubeBlock = block.FatBlock;

				if (cubeBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MedicalRoom))
				{
					Sandbox.ModAPI.Ingame.IMyMedicalRoom medical = (Sandbox.ModAPI.Ingame.IMyMedicalRoom)cubeBlock;
					if (!medical.Enabled)
						continue;

					long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(steamId);
					//if (medical.Owner == playerId || (medical.ShareMode == MyOwnershipShareModeEnum.Faction && Player.CheckPlayerSameFaction(medical.Owner, playerId)))
					if(medical.HasPlayerAccess(playerId))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static void RevealEntities(Dictionary<IMyEntity, string> entitiesToReveal)
		{
			Wrapper.GameAction(() =>
			{
				foreach (KeyValuePair<IMyEntity, string> entity in entitiesToReveal)
				{
					RevealEntity(entity);
				}
			});

			Logging.WriteLineAndConsole(string.Format("Revealed {0} entities.", entitiesToReveal.Count));
		}

		private static void RevealEntity(KeyValuePair<IMyEntity, string> item)
		{
			IMyEntity entity = item.Key;
			string reason = item.Value;
			//Wrapper.GameAction(() =>
			//{
				MyObjectBuilder_CubeGrid builder = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entity);
				if (builder == null)
					return;

				long ownerId = 0;
				string ownerName = string.Empty;
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
				builder.EntityId = 0;

				if(RemovedGrids.Contains(entity.EntityId))
				{
					Logging.WriteLineAndConsole("Conceal", string.Format("Revealing - Id: {0} DUPE FOUND Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, reason));
					BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
				}
				else
				{
					/*
					Logging.WriteLineAndConsole("Conceal", string.Format("Start Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, reason));
					BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
					_removedGrids.Add(entity.EntityId);

					IMyEntity newEntity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
					if (newEntity == null)
					{
						Logging.WriteLineAndConsole("Conceal", string.Format("Issue - CreateFromObjectBuilder failed: {0}", newEntity.EntityId));
						return;
					}

					List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>();
					addList.Add(builder);
					MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
					*/

					Logging.WriteLineAndConsole("Conceal", string.Format("Start Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, reason));
					//builder.PositionAndOrientation = new MyPositionAndOrientation(new Vector3D(Math.Round(builder.PositionAndOrientation.Value.Position.X, 0), Math.Round(builder.PositionAndOrientation.Value.Position.Y, 0), Math.Round(builder.PositionAndOrientation.Value.Position.Z, 0)), builder.PositionAndOrientation.Value.Forward, builder.PositionAndOrientation.Value.Up);
					//builder.PositionAndOrientation = new MyPositionAndOrientation(new Vector3(builder.PositionAndOrientation.Value.Position.X, builder.PositionAndOrientation.Value.Position.Y, builder.PositionAndOrientation.Value.Position.Z), builder.PositionAndOrientation.Value.Forward, builder.PositionAndOrientation.Value.Up);
					//MyAPIGateway.Entities.RemoveEntity(entity);
					RemovedGrids.Add(entity.EntityId);
					BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
					//MyAPIGateway.Entities.AddEntity(newEntity, true);

					IMyEntity newEntity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
					if (newEntity == null)
					{
						Logging.WriteLineAndConsole("Conceal", string.Format("Issue - CreateFromObjectBuilder failed: {0}", newEntity.EntityId));
						return;
					}

					builder.EntityId = newEntity.EntityId;
					List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase> { builder };
					MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
					Logging.WriteLineAndConsole("Conceal", string.Format("End Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, reason));
				}
			//});
		}

		static public void RevealAll()
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() => MyAPIGateway.Entities.GetEntities(entities) );

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
					//IMyCubeGrid grid = (IMyCubeGrid)entity;
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
					MyAPIGateway.Entities.AddEntity(newEntity);
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
				UpdateIdentityCache();
				_turretsEnabled = 0;
				_turretsDisabled = 0;
				_turretsToggled = 0;
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

				if (_turretsToggled > 0 || DateTime.Now - start > TimeSpan.FromSeconds(1))
				{
					Logging.WriteLineAndConsole(string.Format("Disable: {0} turrets enabled.  {1} turrets diabled.  {2} turrets toggled. ({3} ms)", _turretsEnabled, _turretsDisabled, _turretsToggled, (DateTime.Now - start).TotalMilliseconds));
				}
			}
			catch (Exception ex)			
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndDisableTurrets(): {0}", ex));
			}
		}

		private static List<IMyEntity> DisableTurretsWithoutTargets(IMyEntity entity)
		{
			_scanCache.Clear();

			List<IMyEntity> turretList = new List<IMyEntity>();
			if (!(entity is IMyCubeGrid))
				return turretList;

			IMyCubeGrid grid = (IMyCubeGrid)entity;
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			grid.GetBlocks(blocks);
			//bool disable = false;
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
						_turretsEnabled++;
					else
						_turretsDisabled++;

					if (state)// && !ignore)
					{
						//Console.WriteLine("Finding targets on: {0}", entity.DisplayName);
						// No target, so we're not going to enable anything on this grid
						//if (!disable && DoesGridHaveTarget(grid, block))
						if (DoesGridHaveTarget(grid, block))
						{
							// We'll ignore state and only collect for statistics
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
						
						_turretsToggled++;
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
				UpdateIdentityCache();
				_turretsEnabled = 0;
				_turretsDisabled = 0;
				_turretsToggled = 0;
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

				if (_turretsToggled > 0 || DateTime.Now - start > TimeSpan.FromSeconds(1))
				{
					Logging.WriteLineAndConsole(string.Format("Enable: {0} turrets enabled.  {1} turrets diabled.  {2} turrets toggled. ({3} ms)", _turretsEnabled, _turretsDisabled, _turretsToggled, (DateTime.Now - start).TotalMilliseconds));
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndDisableTurrets(): {0}", ex));
			}
		}

		private static List<IMyEntity> EnableTurretsWithTargets(IMyEntity entity)
		{
			List<IMyEntity> turretList = new List<IMyEntity>();

			if (!(entity is IMyCubeGrid))
				return turretList;

			IMyCubeGrid grid = (IMyCubeGrid)entity;
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			_scanCache.Clear();
			grid.GetBlocks(blocks);
			//bool enable = false;
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
						_turretsEnabled++;
					else
						_turretsDisabled++;

					if (!state) // && !ignore)
					{
						// No target, so we're not going to disable anything on this grid
						//if (!enable && !DoesGridHaveTarget(grid, block))
						if (!DoesGridHaveTarget(grid, block))
						{
							// We'll ignore state check and only collect for statistics
//							Console.WriteLine("Ignore");
							continue;
						}

//						Console.WriteLine("Enabling");
						//enable = true;
						
						if (PluginSettings.Instance.DynamicTurretAllowExemption)
						{
							IMyFunctionalBlock functional = (IMyFunctionalBlock)turret;
							if (functional.CustomName.ToLower().Contains("[manualcontrol]"))
							{
								continue;
							}
						}
						
						_turretsToggled++;
						turretList.Add(turret);
					}
				}
			}

			return turretList;
		}

		private static bool DoesGridHaveTarget(IMyCubeGrid grid, IMySlimBlock block)
		{
			if (_scanCache.Count < 1)
			{
				BoundingSphereD sphere = new BoundingSphereD(grid.GetPosition(), PluginSettings.Instance.DynamicTurretTargetDistance);
				_scanCache = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
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
			foreach (IMyEntity testEntity in _scanCache)
			{
				if (grid == testEntity)
					continue;

				if (!testEntity.InScene)
					continue;

				if (testEntity is IMyCubeBlock)
				{
					continue;
					/*
					IMyCubeBlock cubeBlock = (IMyCubeBlock)testEntity;
					if (cubeBlock.OwnerId == 0)
						continue;

					if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.AllButOwner)
					{
						if (block.FatBlock.GetUserRelationToOwner(cubeBlock.OwnerId) != Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Owner)
						{
							Console.WriteLine("Block: Not owner");
							return true;
						}
					}

					if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.NeutralAndEnemy)
					{
						if (block.FatBlock.GetUserRelationToOwner(cubeBlock.OwnerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies ||
							block.FatBlock.GetUserRelationToOwner(cubeBlock.OwnerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral)
						{
							Console.WriteLine("Block: Enemy or Neutral: {0} {1} {2}", cubeBlock.OwnerId, cubeBlock.Parent.DisplayName, cubeBlock.BlockDefinition);
							return true;
						}
					}

					if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.Enemy)
					{
						if (block.FatBlock.GetUserRelationToOwner(cubeBlock.OwnerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies)
						{
//							Console.WriteLine("Block: Enemy: {0} {1} {2}", cubeBlock.OwnerId, cubeBlock.Parent.DisplayName, cubeBlock.BlockDefinition);
							return true;
						}
					}
					 */ 
				}

				IMyCubeGrid entity = testEntity as IMyCubeGrid;
				DynamicTurretManagementTypes dynamicTurretManagementType = PluginSettings.Instance.DynamicTurretManagementType;
				if (entity != null)
				{
					if (dynamicTurretManagementType == DynamicTurretManagementTypes.All)
						return true;

					IMyCubeGrid testGrid = entity;
					// Always enable if grid has no owner.  Seems suspect.  Might be a user trying to abuse a no ownership ship.
					/*
					if (testGrid.BigOwners.Count < 1 && testGrid.SmallOwners.Count < 1)
					{
						//if(!(testEntity is IMyControllableEntity))
						//Console.WriteLine("Grid: No owner");
						return true;
					}
					*/

					foreach (long owner in testGrid.BigOwners)
					{
						if (dynamicTurretManagementType == DynamicTurretManagementTypes.AllButOwner)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) != Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Owner)
							{
								//Console.WriteLine("Grid: Not owner");
								return true;
							}
						}

						if (dynamicTurretManagementType == DynamicTurretManagementTypes.NeutralAndEnemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies ||
								block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral)
							{
								//Console.WriteLine("Grid: Enemy or Neutral: {0} {1}", owner, grid.DisplayName);
								return true;
							}
						}

						if (dynamicTurretManagementType == DynamicTurretManagementTypes.Enemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies)
							{
								//Console.WriteLine("{3} Target: Grid - Enemy: {0} - {1} ({2})", block.FatBlock.OwnerId, owner, testEntity.DisplayName, ((IMyTerminalBlock)block.FatBlock).Parent.DisplayName);

								//Console.WriteLine("Grid: Enemy: {0} {1}", owner, grid.DisplayName);
								return true;
							}
						}
					}

					/*
					foreach (long owner in testGrid.SmallOwners)
					{
						if (dynamicTurretManagementType == DynamicTurretManagementTypes.AllButOwner)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) != Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Owner)
							{
								Console.WriteLine("Grid: Not owner");
								return true;
							}
						}

						if (dynamicTurretManagementType == DynamicTurretManagementTypes.NeutralAndEnemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies ||
								block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral)
							{
								Console.WriteLine("Grid: Enemy or Neutral: {0} {1}", owner, grid.DisplayName);
								return true;
							}
						}

						if (dynamicTurretManagementType == DynamicTurretManagementTypes.Enemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies)
							{
								Console.WriteLine("Grid: Enemy: {0} {1}", owner, grid.DisplayName);
								return true;
							}
						}
					}
					 */
				}
				else
				{
					if (dynamicTurretManagementType == DynamicTurretManagementTypes.All)
						return true;

					var builderBase = testEntity.GetObjectBuilder();
					if (builderBase is MyObjectBuilder_Character)
					{
						IMyPlayer player = null;
						IMyIdentity identity;
						long playerId = 0;
						try
						{
							identity = IdentityCache.FirstOrDefault(x => x.DisplayName == testEntity.DisplayName);
							//List<IMyPlayer> players = new List<IMyPlayer>();
							//MyAPIGateway.Players.GetPlayers(players);							
							//player = players.FirstOrDefault(x => x.DisplayName == testEntity.DisplayName);
							
							if (player == null)
							{
								/*
								//player = players.FirstOrDefault(x => x.Controller != null && x.Controller.ControlledEntity != null && x.Controller.ControlledEntity.Entity != null && x.Controller.ControlledEntity.Entity.EntityId == testEntity.EntityId);
								if(testEntity is IMyControllableEntity)
								{
									IMyControllableEntity control = (IMyControllableEntity)testEntity;
									List<PlayerMap.InternalPlayerItem> items = PlayerMap.Instance.GetPlayerItemsFromPlayerName(control.Entity.DisplayName);
									if (items != null)
										playerId = items.First().playerId;
								}
								 */ 
							}
						}
						catch
						{
							//Console.WriteLine("{3} Target: Character - Unknown: {0} - {1} ({2})", block.FatBlock.OwnerId, testEntity.EntityId, testEntity.DisplayName, ((IMyTerminalBlock)block.FatBlock).Parent.DisplayName);
							//Console.WriteLine("Unknown1: {0}", testEntity.DisplayName);
							return true;
						}

						if (identity == null)
						{
							//Console.WriteLine("{3} Target: Character - Unknown2: {0} - {1} ({2})", block.FatBlock.OwnerId, testEntity.EntityId, testEntity.DisplayName, ((IMyTerminalBlock)block.FatBlock).Parent.DisplayName);
							//Console.WriteLine("Unknown2: {0} - {1}", testEntity.DisplayName, testEntity.GetFriendlyName());
							return true;
							//continue;
						}

						if (identity != null)
							playerId = identity.PlayerId;

						if (player != null)
							playerId = player.PlayerID;

						if (dynamicTurretManagementType == DynamicTurretManagementTypes.AllButOwner && block.FatBlock.GetUserRelationToOwner(playerId) != Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Owner)
						{
							//Console.WriteLine("Character: Not Owner: {0} - {1}", block.FatBlock.OwnerId, playerId);
							return true;
						}

						if (dynamicTurretManagementType == DynamicTurretManagementTypes.NeutralAndEnemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(playerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies ||
								block.FatBlock.GetUserRelationToOwner(playerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral)
							{
								//Console.WriteLine("Character: Enemy or Neutral: {0} - {1}", block.FatBlock.OwnerId, playerId);
								return true;
							}
						}

						if (dynamicTurretManagementType == DynamicTurretManagementTypes.Enemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(playerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies)
							{
								//Console.WriteLine("{3} Target: Character - Enemy: {0} - {1} ({2})", block.FatBlock.OwnerId, playerId, testEntity.DisplayName, ((IMyTerminalBlock)block.FatBlock).Parent.DisplayName);
								return true;
							}
						}
					}
				}
			}

			return false;
		}

		private static void UpdateIdentityCache()
		{
			if(DateTime.Now - _lastIdentityUpdate > TimeSpan.FromMinutes(1))
			{
				_lastIdentityUpdate = DateTime.Now;
				IdentityCache.Clear();
				MyAPIGateway.Players.GetAllIdentites(IdentityCache);
			}
		}

		public static bool ToggleMedbayGrids(ulong steamId)
		{
			if (_checkConceal || _checkReveal)
			{
				Communication.SendPrivateInformation(steamId, "Server busy");
				return false;
			}

			_checkConceal = true;
			_checkReveal = true;
			try
			{
				DateTime start = DateTime.Now;

				// Toggle off
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>();

				try
				{
					MyAPIGateway.Entities.GetEntities(entities);
				}
				catch
				{
					Logging.WriteLineAndConsole("CheckAndConcealEntities(): Error getting entity list, skipping check");
					return false;
				}

				CubeGrids.GetGridsUnconnected(entitiesFound, entities);

				HashSet<IMyEntity> entitiesToConceal = new HashSet<IMyEntity>();
				foreach (IMyEntity entity in entitiesFound)
				{
					if (!(entity is IMyCubeGrid))
						continue;

					if (entity.DisplayName.Contains("CommRelay"))
						continue;

					if (!entity.InScene)
						continue;

					if (((IMyCubeGrid)entity).GridSizeEnum != MyCubeSize.Small && !PluginSettings.Instance.ConcealIncludeLargeGrids)
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(steamId);
					if (!grid.BigOwners.Contains(playerId) && !grid.SmallOwners.Contains(playerId))
						continue;

					bool found = false;
					// Check to see if grid is close to dock / shipyard
					foreach (IMyCubeGrid checkGrid in ProcessDockingZone.ZoneCache)
					{
						try
						{
							if (Vector3D.Distance(checkGrid.GetPosition(), grid.GetPosition()) < 100d)
							{
								found = true;
								break;
							}
						}
						catch
						{
							
						}
					}

					if (!found)
					{
						// Check for block type rules

					}

					if (!found)
					{
						entitiesToConceal.Add(entity);
					}
				}

				if (entitiesToConceal.Count > 0)
				{
					Communication.SendClientMessage(steamId, string.Format("/conceal {0}", string.Join(",", entitiesToConceal.Select(x => string.Format( "{0}:{1}:{2}", x.EntityId, ((MyObjectBuilder_CubeGrid)x.GetObjectBuilder()).CubeBlocks.Count, x.DisplayName )).ToArray())));
					Thread.Sleep(1500);
					ConcealEntities(entitiesToConceal);
					//CheckAndRevealEntities();
				}

				if ((DateTime.Now - start).TotalMilliseconds > 2000)
					Logging.WriteLineAndConsole(string.Format("Completed Toggle: {0}ms", (DateTime.Now - start).TotalMilliseconds));
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("CheckAndConceal(): {0}", ex));
			}
			finally
			{
				_checkConceal = false;
				_checkReveal = false;
			}

			return true;
		}
	}
}
