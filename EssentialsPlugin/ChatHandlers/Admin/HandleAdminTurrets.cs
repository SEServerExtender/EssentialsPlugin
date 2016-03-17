namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using System.Collections.Generic;
    using EssentialsPlugin.Utility;
    using Sandbox.Common;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public class HandleAdminTurrets : ChatHandlerBase
	{
		private Random m_random = new Random();
		//private bool m_working = false;
		public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin turrets";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "";
            DialogItem.content = GetHelp( );
            DialogItem.buttonText = "close";
            return DialogItem;
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
			if (words.Length != 1)
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

						if (words[0].ToLower() == "toggle")
							FunctionalBlockEntity.SetState(turret, !state);

						count++;

						if (state)
							enabled++;
						else
							disabled++;

						if (words[0].ToLower() == "test" && state)
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


								IMyCubeGrid testGrid = testEntity as IMyCubeGrid;
								if (testGrid != null)
								{
									foreach (long owner in testGrid.BigOwners)
									{
										if (block.FatBlock.GetUserRelationToOwner(owner) == MyRelationsBetweenPlayerAndBlock.Enemies ||
											block.FatBlock.GetUserRelationToOwner(owner) == MyRelationsBetweenPlayerAndBlock.Neutral)
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
									MyObjectBuilder_Character c = builderBase as MyObjectBuilder_Character;
									if (c != null)
									{
										ulong steamId = PlayerMap.Instance.GetSteamId(c.EntityId);
										if (steamId < 1)
											continue;

										long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(steamId);
										if (playerId < 1)
											continue;

										if (block.FatBlock.GetUserRelationToOwner(playerId) == MyRelationsBetweenPlayerAndBlock.Enemies ||
											block.FatBlock.GetUserRelationToOwner(playerId) == MyRelationsBetweenPlayerAndBlock.Neutral)
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

		/*
		IMyGridTerminalSystem GridTerminalSystem;
		List<IMyTerminalBlock> buffer = new List<IMyTerminalBlock>(); 
		void Main()
		{

			if (buffer.Count < 1)
			{
				GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(buffer);
			}

			if(buffer.Count < 1)
				return;

			IMyTerminalBlock block1 = buffer[0];
			IMyTerminalBlock block2 = buffer[1];

			IMyCubeGrid grid1Parent = ((IMyCubeBlock)block1).CubeGrid
			IMyCubeGrid grid2Parent = ((IMyCubeBlock)block2).CubeGrid

			throw new Exception(string.Format("Here: {0} - {1} - {2}", buffer.Count, grid1Parent.GridIntegerToWorld(block1.Min), grid2Parent.GridIntegerToWorld(block2.Min)));
		}
		 */ 
	}
}
