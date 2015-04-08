namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;

	class ProcessBlockEnforcement : ProcessHandlerBase
	{
		public override int GetUpdateResolution( )
		{
			return 45000;
		}

		public override void Handle( )
		{
			try
			{
				if ( !PluginSettings.Instance.BlockEnforcementEnabled )
					return;

				DateTime start = DateTime.Now;
				if ( PluginSettings.Instance.BlockEnforcementItems.Count > 0 )
				{
					ScanForBlockItems( );
					if ( ( DateTime.Now - start ).TotalMilliseconds > 2000 )
						Log.Info( "Block Enforce Scan Time Elapsed: {0}ms", ( DateTime.Now - start ).TotalMilliseconds );
				}
			}
			catch ( Exception ex )
			{
				Log.Error( ex );
			}

			base.Handle( );
		}

		private void ScanForBlockItems( )
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );

			try
			{
				MyAPIGateway.Entities.GetEntities( entities );
			}
			catch ( Exception ex )
			{
				Log.Error( "Entity list busy, skipping scan.", ex );
			}

			foreach ( IMyEntity entity in entities )
			{
				if ( !( entity is IMyCubeGrid ) )
					continue;

				if ( entity.Physics == null )
					continue;

				if ( !entity.InScene )
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				Sandbox.ModAPI.Ingame.IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid( grid );

				Dictionary<SettingsBlockEnforcementItem, int> blocks = new Dictionary<SettingsBlockEnforcementItem, int>( );
				foreach ( Sandbox.ModAPI.Ingame.IMyTerminalBlock myTerminalBlock in gridTerminal.Blocks )
				{
					Sandbox.ModAPI.Ingame.IMyTerminalBlock block = myTerminalBlock;
					foreach ( SettingsBlockEnforcementItem item in PluginSettings.Instance.BlockEnforcementItems )
					{
						if ( item.Mode == SettingsBlockEnforcementItem.EnforcementMode.Off )
							continue;

						if ( item.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId
							&& !string.IsNullOrEmpty( block.BlockDefinition.SubtypeId )
							&& block.BlockDefinition.SubtypeId.Contains( item.BlockTypeId ) )
						{
							if ( blocks.ContainsKey( item ) )
								blocks[ item ] += 1;
							else
								blocks.Add( item, 1 );
						}

						if ( item.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockId && block.BlockDefinition.TypeIdString.Contains( item.BlockTypeId ) )
						{
							if ( blocks.ContainsKey( item ) )
								blocks[ item ] += 1;
							else
								blocks.Add( item, 1 );
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

						if (block.GetId().ToString().Contains(item.BlockTypeId))
						{
							if (blocks.ContainsKey(item.BlockTypeId))
								blocks[item.BlockTypeId] += 1;
							else
								blocks.Add(item.BlockTypeId, 1);
						}
					}
				}
				*/

				foreach ( SettingsBlockEnforcementItem item in PluginSettings.Instance.BlockEnforcementItems )
				{
					if ( item.Mode== SettingsBlockEnforcementItem.EnforcementMode.Off )
						continue;

					if ( !blocks.ContainsKey( item ) )
						continue;

					if ( blocks[ item ] > item.MaxPerGrid )
					{
						//foreach(long playerId in CubeGrids.GetBigOwners(gridBuilder))
						foreach ( long playerId in grid.BigOwners )
						{
							ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerId( playerId );
							if ( steamId > 0 )
							{
								//Communication.SendPrivateInformation(steamId, string.Format("You have exceeded the max block count of {0} on the ship '{1}'.  We are removing {2} blocks to enforce this block limit.", item.BlockTypeId, gridBuilder.DisplayName, blocks[item.BlockTypeId] - item.MaxPerGrid));
								Communication.SendPrivateInformation( steamId, string.Format( "You have exceeded the max block count of {0} on the ship '{1}'.  We are removing {2} blocks to enforce this block limit.", item.BlockTypeId, grid.DisplayName, blocks[ item ] - item.MaxPerGrid ) );
							}
						}

						//DeleteReverse(item.BlockTypeId, blocks[item.BlockTypeId] - item.MaxPerGrid, grid, gridBuilder);
						DeleteReverse( item, blocks[ item ] - item.MaxPerGrid, grid );
					}
				}
			}
		}

		private void DeleteReverse( SettingsBlockEnforcementItem blockEnforcementSetting, int remove, IMyCubeGrid grid )
		{
			int count = 0;
			Sandbox.ModAPI.Ingame.IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid( grid );

			List<IMyTerminalBlock> blocksToRemove = new List<IMyTerminalBlock>( );
			for ( int r = gridTerminal.Blocks.Count - 1; r >= 0; r-- )
			{
				IMyTerminalBlock block = (IMyTerminalBlock)gridTerminal.Blocks[ r ];
				switch ( blockEnforcementSetting.Mode )
				{
					case SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId:
						if ( !string.IsNullOrEmpty( block.BlockDefinition.SubtypeId ) && block.BlockDefinition.SubtypeId.Contains( blockEnforcementSetting.BlockSubtypeId ) )
						{
							blocksToRemove.Add( block );
							count++;
						}
						break;
					case SettingsBlockEnforcementItem.EnforcementMode.BlockId:
						if ( block.BlockDefinition.TypeIdString.Contains( blockEnforcementSetting.BlockTypeId ) )
						{
							blocksToRemove.Add( block );
							count++;
						}
						break;
				}

				if ( count == remove )
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

			if ( blocksToRemove.Count < 1 )
				return;

			List<VRageMath.Vector3I> razeList = new List<VRageMath.Vector3I>( );
			foreach ( IMyTerminalBlock block in blocksToRemove )
			{
				razeList.Add( block.Min );
			}

			Wrapper.GameAction( ( ) =>
			{
				grid.RazeBlocks( razeList );
			} );
		}
	}
}
