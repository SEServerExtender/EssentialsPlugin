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
			string[] splits = General.SplitString(string.Join(" ", words));
			if (splits.Length != 1)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			DateTime start = DateTime.Now;
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities);
			int count = 0;
			int enabled = 0;
			int disabled = 0;
			int keepOn = 0;
			foreach (IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				if (!entity.InScene)
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				//MyObjectBuilder_CubeGrid builder = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entity);
				//if (builder == null)
				//	continue;

				List<IMySlimBlock> blocks = new List<IMySlimBlock>();
				grid.GetBlocks(blocks);
				foreach(IMySlimBlock block in blocks)
				//foreach (MyObjectBuilder_CubeBlock block in builder.CubeBlocks)
				{
					//if (block is MyObjectBuilder_TurretBase)
					if (block.FatBlock == null)
						continue;

					if(block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_InteriorTurret) ||
					   block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_LargeGatlingTurret) ||
					   block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_LargeMissileTurret))
					{
						//IMyEntity turret = MyAPIGateway.Entities.GetEntityById(block.EntityId);
						IMyEntity turret = block.FatBlock;
						bool state = FunctionalBlockEntity.GetState(turret);

						if(splits[0].ToLower() == "toggle")
							FunctionalBlockEntity.SetState(turret, !state);

						count++;

						if (state)
							enabled++;
						else
							disabled++;

						if (splits[0].ToLower() == "test" && state)
						{
							BoundingSphereD sphere = new BoundingSphereD(grid.GetPosition(), 2000);
							List<IMyEntity> testEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
							bool found = false;
							foreach (IMyEntity testEntity in testEntities)
							{
								if (entity == testEntity)
									continue;

								if (testEntity is IMyCubeBlock)
									continue;

								if (!(testEntity is IMyControllableEntity))
								{
									//Console.WriteLine("Entity: {0}", testEntity.GetType());
									continue;
								}


								if (testEntity is IMyCubeGrid)
								{
									IMyCubeGrid testGrid = (IMyCubeGrid)testEntity;
									foreach (long owner in testGrid.BigOwners)
									{
										if (block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies ||
											block.FatBlock.GetUserRelationToOwner(owner) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral)
										{
											found = true;
											keepOn++;
											break;
										}
										else
										{
											Console.WriteLine("Relation: {0} - {1}", block.FatBlock.GetUserRelationToOwner(owner), testGrid.DisplayName);
										}
									}

									if (found)
										break;
								}
								else
								{
									var builderBase = testEntity.GetObjectBuilder();
									if (builderBase is MyObjectBuilder_Character)
									{
										MyObjectBuilder_Character c = (MyObjectBuilder_Character)builderBase;
										ulong steamId = PlayerMap.Instance.GetSteamId(c.EntityId);
										if (steamId < 1)
											continue;

										long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(steamId);
										if (playerId < 1)
											continue;

										if (block.FatBlock.GetUserRelationToOwner(playerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies ||
											block.FatBlock.GetUserRelationToOwner(playerId) == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral)
										{
											found = true;
											keepOn++;
											break;
										}
/*
										else
										{
											Console.WriteLine("Character Relation: {0} - {1}", block.FatBlock.GetUserRelationToOwner(playerId), c.DisplayName);
										}
*/
										if (found)
											break;
									}
								}
							}

							//break;
						}
					}
				}
			}

			Communication.SendPrivateInformation(userId, string.Format("{0} turrets.  {1} on, {2} off.  {3} keepon ({4} ms)", count, enabled, disabled, keepOn, (DateTime.Now - start).TotalMilliseconds));
			return true;
		}
	}
}
