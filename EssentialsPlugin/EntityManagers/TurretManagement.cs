namespace EssentialsPlugin.EntityManagers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.Common;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
	using VRageMath;

	public class TurretManagement
	{
		private static List<IMyEntity> m_scanCache = new List<IMyEntity>();
		private static List<IMyIdentity> m_identityCache = new List<IMyIdentity>();
		private static DateTime m_lastIdentityUpdate = DateTime.Now.AddHours(-1);
		private static int m_turretsEnabled = 0;
		private static int m_turretsDisabled = 0;
		private static int m_turretsToggled = 0;

		public static void CheckAndDisableTurrets()
		{
			try
			{
				DateTime start = DateTime.Now;
				List<IMyEntity> turretList = new List<IMyEntity>();
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				MyAPIGateway.Entities.GetEntities(entities);
				UpdateIdentityCache();
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
			m_scanCache.Clear();

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

					if (state)// && !ignore)
					{
						//Console.WriteLine("Finding targets on: {0}", entity.DisplayName);
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
				UpdateIdentityCache();
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

					if (!state) // && !ignore)
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

		private static bool DoesGridHaveTarget(IMyCubeGrid grid, IMySlimBlock block, bool disabling = false)
		{
			if (m_scanCache.Count < 1)
			{
				BoundingSphereD sphere = new BoundingSphereD(grid.GetPosition(), PluginSettings.Instance.DynamicTurretTargetDistance);
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

				if (testEntity is IMyCubeGrid)
				{
					if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.All)
						return true;

					IMyCubeGrid testGrid = (IMyCubeGrid)testEntity;
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
						if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.AllButOwner)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) != MyRelationsBetweenPlayerAndBlock.Owner)
							{
								//Console.WriteLine("Grid: Not owner");
								return true;
							}
						}

						if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.NeutralAndEnemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) == MyRelationsBetweenPlayerAndBlock.Enemies ||
								block.FatBlock.GetUserRelationToOwner(owner) == MyRelationsBetweenPlayerAndBlock.Neutral)
							{
								//Console.WriteLine("Grid: Enemy or Neutral: {0} {1}", owner, grid.DisplayName);
								return true;
							}
						}

						if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.Enemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) == MyRelationsBetweenPlayerAndBlock.Enemies)
							{
								//Console.WriteLine("{3} Target: Grid - Enemy: {0} - {1} ({2})", block.FatBlock.OwnerId, owner, testEntity.DisplayName, ((IMyTerminalBlock)block.FatBlock).Parent.DisplayName);

								//Console.WriteLine("Grid: Enemy: {0} {1}", owner, grid.DisplayName);
								return true;
							}
						}
					}

					foreach (long owner in testGrid.SmallOwners)
					{
						if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.AllButOwner)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) != MyRelationsBetweenPlayerAndBlock.Owner)
							{
								//Console.WriteLine("Grid: Not owner");
								return true;
							}
						}

						if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.NeutralAndEnemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) == MyRelationsBetweenPlayerAndBlock.Enemies ||
								block.FatBlock.GetUserRelationToOwner(owner) == MyRelationsBetweenPlayerAndBlock.Neutral)
							{
								//Console.WriteLine("Grid: Enemy or Neutral: {0} {1}", owner, grid.DisplayName);
								return true;
							}
						}

						if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.Enemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(owner) == MyRelationsBetweenPlayerAndBlock.Enemies)
							{
								//Console.WriteLine("Grid: Enemy: {0} {1}", owner, grid.DisplayName);
								return true;
							}
						}
					}
				}
				else
				{
					if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.All)
						return true;

					if(testEntity is IMyCharacter)
//					var builderBase = testEntity.GetObjectBuilder();
//					if (builderBase is MyObjectBuilder_Character)
					{
						IMyPlayer player = null;
						IMyIdentity identity = null;
						long playerId = 0;
						try
						{
							identity = m_identityCache.FirstOrDefault(x => x.DisplayName == testEntity.DisplayName);
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

						if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.AllButOwner && block.FatBlock.GetUserRelationToOwner(playerId) != MyRelationsBetweenPlayerAndBlock.Owner)
						{
							//Console.WriteLine("Character: Not Owner: {0} - {1}", block.FatBlock.OwnerId, playerId);
							return true;
						}

						if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.NeutralAndEnemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(playerId) == MyRelationsBetweenPlayerAndBlock.Enemies ||
								block.FatBlock.GetUserRelationToOwner(playerId) == MyRelationsBetweenPlayerAndBlock.Neutral)
							{
								//Console.WriteLine("Character: Enemy or Neutral: {0} - {1}", block.FatBlock.OwnerId, playerId);
								return true;
							}
						}

						if (PluginSettings.Instance.DynamicTurretManagementType == DynamicTurretManagementTypes.Enemy)
						{
							if (block.FatBlock.GetUserRelationToOwner(playerId) == MyRelationsBetweenPlayerAndBlock.Enemies)
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
			if(DateTime.Now - m_lastIdentityUpdate > TimeSpan.FromMinutes(1))
			{
				m_lastIdentityUpdate = DateTime.Now;
				m_identityCache.Clear();
				MyAPIGateway.Players.GetAllIdentites(m_identityCache);
			}
		}

	}
}
