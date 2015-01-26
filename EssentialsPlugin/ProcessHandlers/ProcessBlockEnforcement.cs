namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using EssentialsPlugin.ProcessHandler;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;

	class ProcessBlockEnforcement : ProcessHandlerBase
	{

		public override int GetUpdateResolution()
		{
			return 45000;
		}

		public override void Handle()
		{
			try
			{
				if (!PluginSettings.Instance.BlockEnforcementEnabled)
					return;

				DateTime start = DateTime.Now;
				if(PluginSettings.Instance.BlockEnforcementItems.Count > 0)
				{
					ScanForBlockItems();
					if ((DateTime.Now - start).TotalMilliseconds > 2000)
						Logging.WriteLineAndConsole(string.Format("Block Enforce Scan Time Elapsed: {0}ms", (DateTime.Now - start).TotalMilliseconds));
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("ProcessBlockEnforcement.Handle(): {0}", ex.ToString()));
			}

			base.Handle();
		}

		private void ScanForBlockItems()
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();

			try
			{
				MyAPIGateway.Entities.GetEntities(entities);
			}
			catch
			{
				Logging.WriteLineAndConsole(string.Format("ScanForBlockItems(): Entity list busy, skipping scan."));
			}

			foreach (IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				if (entity.Physics == null)
					continue;

				if (!entity.InScene)
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				Sandbox.ModAPI.Ingame.IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

				Dictionary<string, int> blocks = new Dictionary<string, int>();
				foreach (IMyTerminalBlock block in gridTerminal.Blocks)
				{
					foreach (SettingsBlockEnforcementItem item in PluginSettings.Instance.BlockEnforcementItems)
					{
						if (!item.Enabled)
							continue;

						if (block.BlockDefinition.TypeId.ToString().Contains(item.BlockType))
						{
							if (blocks.ContainsKey(item.BlockType))
								blocks[item.BlockType] += 1;
							else
								blocks.Add(item.BlockType, 1);
						}
					}
				}

				/*
				MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);
				if (gridBuilder == null)
					continue;

				Dictionary<string, int> blocks = new Dictionary<string, int>();
				foreach (MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
				{
					foreach(SettingsBlockEnforcementItem item in PluginSettings.Instance.BlockEnforcementItems)
					{
						if (!item.Enabled)
							continue;

						if (block.GetId().ToString().Contains(item.BlockType))
						{
							if (blocks.ContainsKey(item.BlockType))
								blocks[item.BlockType] += 1;
							else
								blocks.Add(item.BlockType, 1);
						}
					}
				}
				*/

				foreach(SettingsBlockEnforcementItem item in PluginSettings.Instance.BlockEnforcementItems)
				{
					if (!item.Enabled)
						continue;

					if (!blocks.ContainsKey(item.BlockType))
						continue;

					if (blocks[item.BlockType] > item.MaxPerGrid)
					{
						//foreach(long playerId in CubeGrids.GetBigOwners(gridBuilder))
						foreach(long playerId in grid.BigOwners)
						{
							ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerId(playerId);
							if (steamId > 0)
							{
								//Communication.SendPrivateInformation(steamId, string.Format("You have exceeded the max block count of {0} on the ship '{1}'.  We are removing {2} blocks to enforce this block limit.", item.BlockType, gridBuilder.DisplayName, blocks[item.BlockType] - item.MaxPerGrid));
								Communication.SendPrivateInformation(steamId, string.Format("You have exceeded the max block count of {0} on the ship '{1}'.  We are removing {2} blocks to enforce this block limit.", item.BlockType, grid.DisplayName, blocks[item.BlockType] - item.MaxPerGrid));
							}
						}

						//DeleteReverse(item.BlockType, blocks[item.BlockType] - item.MaxPerGrid, grid, gridBuilder);
						DeleteReverse(item.BlockType, blocks[item.BlockType] - item.MaxPerGrid, grid);
					}
				}
			}
		}

		private void DeleteReverse(string id, int remove, IMyCubeGrid grid)
		{
			int count = 0;
			Sandbox.ModAPI.Ingame.IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

			List<IMyTerminalBlock> blocksToRemove = new List<IMyTerminalBlock>();
			for (int r = gridTerminal.Blocks.Count - 1; r >= 0; r--)
			{
				IMyTerminalBlock block = (IMyTerminalBlock)gridTerminal.Blocks[r];
				if (block.BlockDefinition.TypeId.ToString().Contains(id))
				{
					blocksToRemove.Add(block);
					count++;
				}

				if (count == remove)
					break;
			}

			/*
			List<MyObjectBuilder_CubeBlock> blocksToRemove = new List<MyObjectBuilder_CubeBlock>();
			for (int r = gridBuilder.CubeBlocks.Count - 1; r >= 0; r--)
			{
				MyObjectBuilder_CubeBlock block = gridBuilder.CubeBlocks[r];
				if (block.GetId().ToString().Contains(id))
				{
					blocksToRemove.Add(block);
					count++;
				}

				if (count == remove)
					break;
			}
			*/

			if (blocksToRemove.Count < 1)
				return;

			List<VRageMath.Vector3I> razeList = new List<VRageMath.Vector3I>();
			foreach (IMyTerminalBlock block in blocksToRemove)
			{
				razeList.Add(block.Min);
			}

			Wrapper.GameAction(() =>
			{
				grid.RazeBlocks(razeList);
			});			
		}
	}
}
