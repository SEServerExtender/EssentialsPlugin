namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using Sandbox.ModAPI.Ingame;
	using SEModAPIInternal.API.Common;
	using VRage.ModAPI;
	using VRageMath;
	using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
	using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;

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
						Essentials.Log.Info( "Block Enforce Scan Time Elapsed: {0}ms", ( DateTime.Now - start ).TotalMilliseconds );
				}
			}
			catch ( Exception ex )
			{
				Essentials.Log.Error( ex );
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
				Essentials.Log.Error( "Entity list busy, skipping scan.", ex );
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
				IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid( grid );

				Dictionary<SettingsBlockEnforcementItem, int> blocks = new Dictionary<SettingsBlockEnforcementItem, int>( );
				foreach ( IMyTerminalBlock myTerminalBlock in gridTerminal.Blocks )
				{
					IMyTerminalBlock block = myTerminalBlock;
					foreach ( SettingsBlockEnforcementItem item in PluginSettings.Instance.BlockEnforcementItems )
					{
						if ( item.Mode == SettingsBlockEnforcementItem.EnforcementMode.Off )
							continue;

						if ( item.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId && string.IsNullOrEmpty( item.BlockTypeId ) )
						{
							Essentials.Log.Warn( "Block Enforcement item for \"{0}\" is set to mode BlockTypeId but does not have BlockTypeId set." );
							continue;
						}
						if ( item.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId && string.IsNullOrEmpty( item.BlockSubtypeId ) )
						{
							Essentials.Log.Warn( "Block Enforcement item for \"{0}\" is set to mode BlockSubtypeId but does not have BlockSubtypeId set." );
							continue;
						}

						if ( item.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId
							&& !string.IsNullOrEmpty( block.BlockDefinition.SubtypeId )
							&& block.BlockDefinition.SubtypeId.Contains( item.BlockSubtypeId ) )
						{
							if ( blocks.ContainsKey( item ) )
								blocks[ item ] += 1;
							else
								blocks.Add( item, 1 );
						}

						if ( item.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId
							&& !string.IsNullOrEmpty( block.BlockDefinition.TypeIdString )
							&& block.BlockDefinition.TypeIdString.Contains( item.BlockTypeId ) )
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
			IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid( grid );

			List<Sandbox.ModAPI.IMyTerminalBlock> blocksToRemove = new List<Sandbox.ModAPI.IMyTerminalBlock>( );
			for ( int r = gridTerminal.Blocks.Count - 1; r >= 0; r-- )
			{
				Sandbox.ModAPI.IMyTerminalBlock block = (Sandbox.ModAPI.IMyTerminalBlock)gridTerminal.Blocks[ r ];
				switch ( blockEnforcementSetting.Mode )
				{
					case SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId:
						if ( !string.IsNullOrEmpty( block.BlockDefinition.SubtypeId ) && block.BlockDefinition.SubtypeId.Contains( blockEnforcementSetting.BlockSubtypeId ) )
						{
							blocksToRemove.Add( block );
							count++;
						}
						break;
					case SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId:
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

			List<Vector3I> razeList = new List<Vector3I>( );
			foreach ( Sandbox.ModAPI.IMyTerminalBlock block in blocksToRemove )
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
