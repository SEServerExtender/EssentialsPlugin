namespace EssentialsPlugin.Utility
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using NLog;
	using Sandbox.Common;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using VRage;
	using VRage.ModAPI;
	using VRageMath;
    using Sandbox.Game.Entities;
    using VRage.Groups;
    using Sandbox.Game.GameSystems;
    using Sandbox.Game.Entities.Cube;

	public enum RemoveGridTypes
	{
		All,
		Ships,
		Stations
	}

	public static class CubeGrids
	{
		private static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
		public static Vector3D RemoveGridsInSphere( ulong userId, Vector3D startPosition, float radius, RemoveGridTypes removeType )
		{
			BoundingSphereD sphere = new BoundingSphereD( startPosition, radius );
			List<IMyEntity> entitiesToMove = MyAPIGateway.Entities.GetEntitiesInSphere( ref sphere );
			List<IMyEntity> entitiesToRemove = new List<IMyEntity>( );
			int count = 0;

			Wrapper.GameAction( ( ) =>
			{
				foreach ( IMyEntity entity in entitiesToMove )
				{
					if ( !( entity is IMyCubeGrid ) )
						continue;

					IMyCubeGrid grid = (IMyCubeGrid)entity;
					MyObjectBuilder_CubeGrid cubeGrid = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( );
					if ( removeType == RemoveGridTypes.Ships && cubeGrid.IsStatic )
						continue;

					if ( removeType == RemoveGridTypes.Stations && !cubeGrid.IsStatic )
						continue;

					entitiesToRemove.Add( entity );
					Communication.SendPrivateInformation( userId, string.Format( "Deleting entity '{0}' at {1}", entity.DisplayName, General.Vector3DToString( entity.GetPosition( ) ) ) );
					count++;
				}
			} );

			for ( int r = entitiesToRemove.Count - 1; r >= 0; r-- )
			{
				IMyEntity entity = entitiesToRemove[ r ];
				//MyAPIGateway.Entities.RemoveEntity(entity);
				CubeGridEntity gridEntity = new CubeGridEntity( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ), entity );
				gridEntity.Dispose( );
			}

			Communication.SendPrivateInformation( userId, string.Format( "Total entities removed: {0}", count ) );
			return startPosition;
		}

		public static MyPositionAndOrientation CreatePositionAndOrientation( Vector3 position, Vector3 lookAt )
		{
			Vector3 newForward = Vector3.Normalize( position - lookAt );
			Quaternion rotate = MathUtility.GetRotation( Vector3.Forward, newForward, Vector3.Up );
			Matrix rot = Matrix.CreateFromQuaternion( rotate );
			return new MyPositionAndOrientation( position, rot.Forward, rot.Up );
		}

		public static void GetGridsUnconnected( HashSet<IMyEntity> connectedList, HashSet<IMyEntity> entitiesToConfirm )
		{
			foreach ( IMyEntity entity in entitiesToConfirm )
			{
				if ( !( entity is IMyCubeGrid ) )
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				MyObjectBuilder_CubeGrid gridBuilder = SafeGetObjectBuilder( grid );
				if ( gridBuilder == null )
					continue;

				bool result = false;
				foreach ( MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks )
				{
					if ( block.TypeId == typeof( MyObjectBuilder_ShipConnector ) )
					{
						MyObjectBuilder_ShipConnector connector = (MyObjectBuilder_ShipConnector)block;
						if ( connector.Connected )
						{
							IMyEntity connectedEntity;
							MyAPIGateway.Entities.TryGetEntityById( connector.ConnectedEntityId, out connectedEntity );

							if ( connectedEntity != null )
							{
								result = true;
								break;
							}
						}
					}

					if ( block.TypeId == typeof( MyObjectBuilder_PistonBase ) )
					{
						result = true;
						break;
					}

					if ( block.TypeId == typeof( MyObjectBuilder_ExtendedPistonBase ) )
					{
						result = true;
						break;
					}

					if ( block.TypeId == typeof( MyObjectBuilder_PistonTop ) )
					{
						result = true;
						break;
					}

					if ( block.TypeId == typeof( MyObjectBuilder_MotorAdvancedStator ) )
					{
						MyObjectBuilder_MotorAdvancedStator stator = (MyObjectBuilder_MotorAdvancedStator)block;
						if ( stator.RotorEntityId != 0 )
						{
							IMyEntity connectedEntity;
							MyAPIGateway.Entities.TryGetEntityById( (long) stator.RotorEntityId, out connectedEntity );

							if ( connectedEntity != null )
							{
								result = true;
								break;
							}
						}
					}

					if ( block.TypeId == typeof( MyObjectBuilder_MotorAdvancedRotor ) )
					{
						result = true;
						break;
					}

					if ( block.TypeId == typeof( MyObjectBuilder_MotorStator ) )
					{
						MyObjectBuilder_MotorStator stator = (MyObjectBuilder_MotorStator)block;
						if ( stator.RotorEntityId != 0 )
						{
							IMyEntity connectedEntity;
							MyAPIGateway.Entities.TryGetEntityById( (long) stator.RotorEntityId, out connectedEntity );

							if ( connectedEntity != null )
							{
								result = true;
								break;
							}
						}
					}

					if ( block.TypeId == typeof( MyObjectBuilder_MotorRotor ) )
					{
						result = true;
						break;
					}
				}

				if ( !result )
					connectedList.Add( entity );
			}
		}

		public static IMyEntity Find( string displayName )
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );

			Wrapper.GameAction( ( ) =>
			{
				MyAPIGateway.Entities.GetEntities( entities, x => x is IMyCubeGrid );
			} );

			return entities.FirstOrDefault( entity => entity.DisplayName.ToLower( ).Contains( displayName.ToLower( ) ) );
		}

		public static bool WaitForLoadingEntity( CubeGridEntity grid )
		{
			int count = 0;
			while ( grid.IsLoading )
			{
				Thread.Sleep( 100 );
				count++;
				if ( count > 10 )
					return false;
			}

			return true;
		}

		public static HashSet<IMyEntity> ScanCleanup( ulong userId, string[ ] words )
		{
			Dictionary<string, string> options = new Dictionary<string, string>( );

			bool requiresFunctional = true;
			bool requiresTerminal = true;
			bool requiresPower = true;
			bool hasDisplayName = false;
			bool ignoreOwnership = false;
			bool requiresOwner = false;
			bool debug = false;
			bool hasBlockSubType = false;
			bool hasBlockSubTypeLimits = false;
			bool hasDisplayNameExact = false;

			options.Add( "Requires Functional", "true" );
			options.Add( "Requires Terminal", "true" );
			options.Add( "Requires Valid Power", "true" );
			options.Add( "Matches Display Name Text", "false" );
			options.Add( "Ignore Ownership", "false" );
			options.Add( "Requires Ownership", "false" );
			options.Add( "Debug", "false" );
			options.Add( "Has Sub Block Type", "false" );
			options.Add( "Has Sub Block Type Limits", "false" );

			string displayName = "";
			Dictionary<string, int> blockSubTypes = new Dictionary<string, int>( );

			if ( words.Any( ) )
			{
				if ( words.FirstOrDefault( x => x.ToLower( ) == "debug" ) != null )
				{
					options[ "Debug" ] = "true";
					debug = true;
				}

				if ( words.SingleOrDefault( x => x.ToLower( ) == "ignoreownership" ) != null )
				{
					options[ "Ignore Ownership" ] = "true";
					ignoreOwnership = true;
				}

				if ( words.SingleOrDefault( x => x.ToLower( ) == "isowned" ) != null )
				{
					options[ "Requires Ownership" ] = "true";
					requiresOwner = true;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "ignorefunctional" ) != null )
				{
					options[ "Requires Functional" ] = "false";
					requiresFunctional = false;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "ignoreterminal" ) != null )
				{
					options[ "Requires Terminal" ] = "false";
					requiresTerminal = false;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "ignorepower" ) != null )
				{
					options[ "Requires Valid Power" ] = "ignore";
					requiresPower = false;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ) != null )
				{
					hasDisplayName = true;
					displayName = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ).Split( ':' )[ 1 ];
					options[ "Matches Display Name Text" ] = "true:" + displayName;

					if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ).Split( ':' ).Length > 2 && words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ).Split( ':' )[ 2 ] == "exact" )
						hasDisplayNameExact = true;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasblocksubtype:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasblocksubtype:" ) ).Split( ':' );
					hasBlockSubType = true;
					options[ "Has Sub Block Type" ] = "true";

					if ( parts.Length < 3 )
					{
						blockSubTypes.Add( parts[ 1 ], 1 );
						options.Add( "Sub Block Type: " + parts[ 1 ], "1" );
					}
					else
					{
						int count = 1;
						int.TryParse( parts[ 2 ], out count );
						blockSubTypes.Add( parts[ 1 ], count );
						options.Add( "Sub Block Type: " + parts[ 1 ], count.ToString( ) );
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "limitblocksubtype:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "limitblocksubtype:" ) ).Split( ':' );
					hasBlockSubTypeLimits = true;
					options[ "Has Sub Block Type Limits" ] = "true";

					if ( parts.Length < 3 )
					{
						blockSubTypes.Add( parts[ 1 ], 1 );
						options.Add( "Sub Block Type Limit: " + parts[ 1 ], "1" );
					}
					else
					{
						int count = 1;
						int.TryParse( parts[ 2 ], out count );
						blockSubTypes.Add( parts[ 1 ], count );
						options.Add( "Sub Block Type Limit: " + parts[ 1 ], count.ToString( ) );
					}
				}
			}

			Communication.SendPrivateInformation( userId, string.Format( "Scanning for ships with options: {0}", GetOptionsText( options ) ) );

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
			Wrapper.GameAction( ( ) =>
			{
				MyAPIGateway.Entities.GetEntities( entities, x => x is IMyCubeGrid );
			} );

			HashSet<IMyEntity> entitiesToConfirm = new HashSet<IMyEntity>( );
			HashSet<IMyEntity> entitiesUnconnected = new HashSet<IMyEntity>( );
			HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>( );
			foreach ( IMyEntity entity in entities )
			{
				if ( !( entity is IMyCubeGrid ) )
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				MyObjectBuilder_CubeGrid gridBuilder;
				try
				{
					gridBuilder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( );
				}
				catch
				{
					continue;
				}

				//CubeGridEntity gridEntity = (CubeGridEntity)GameEntityManager.GetEntity(grid.EntityId);

				if ( PluginSettings.Instance.LoginEntityWhitelist.Contains( entity.EntityId.ToString( ) ) || PluginSettings.Instance.LoginEntityWhitelist.Contains( entity.DisplayName ) )
					continue;

				if ( hasDisplayName && displayName != "" )
				{
					if ( !hasDisplayNameExact && entity.DisplayName.Contains( displayName ) )
						entitiesToConfirm.Add( entity );

					if ( hasDisplayNameExact && entity.DisplayName.Equals( displayName ) )
						entitiesToConfirm.Add( entity );
				}
				else if ( ignoreOwnership )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( requiresOwner && HasOwner( gridBuilder ) )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( !requiresOwner && !HasOwner( gridBuilder ) )
				{
					entitiesToConfirm.Add( entity );
				}
			}

			Dictionary<string, int> subTypeDict = new Dictionary<string, int>( );
			GetGridsUnconnected( entitiesUnconnected, entitiesToConfirm );
			foreach ( IMyEntity entity in entitiesUnconnected )
			{
				subTypeDict.Clear( );
				MyObjectBuilder_CubeGrid grid = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( );
				bool found = false;
				foreach ( MyObjectBuilder_CubeBlock block in grid.CubeBlocks )
				{
					if ( requiresFunctional )
					{
						if ( block is MyObjectBuilder_FunctionalBlock )
						{
							if ( debug && !found )
								Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which has a functional block.  BlockCount={2}", entity.DisplayName, entity.EntityId, ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );

							found = true;
						}
					}

					if ( requiresTerminal )
					{
						if ( block is MyObjectBuilder_TerminalBlock )
						{
							if ( debug && !found )
								Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which has a terminal block.  BlockCount={2}", entity.DisplayName, entity.EntityId, ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );

							found = true;
						}
					}


					if ( requiresPower )
					{
						if ( DoesBlockSupplyPower( block ) )
						{
							if ( debug && !found )
								Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which has power.  BlockCount={2}", entity.DisplayName, entity.EntityId, ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );

							found = true;
						}
					}

					if ( hasBlockSubType || hasBlockSubTypeLimits )
					{
						string subTypeName = block.GetId( ).SubtypeName;
						if ( subTypeDict.ContainsKey( subTypeName ) )
							subTypeDict[ subTypeName ] = subTypeDict[ subTypeName ] + 1;
						else
							subTypeDict.Add( subTypeName, 1 );
					}
				}

				if ( hasBlockSubType )
				{
					foreach ( KeyValuePair<string, int> p in subTypeDict )
					{
						foreach ( KeyValuePair<string, int> s in blockSubTypes )
						{
							if ( p.Key.ToLower( ).Contains( s.Key.ToLower( ) ) )
							{
								if ( p.Value >= s.Value )
								{
									if ( debug )
										Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which contains at least {4} of block type {3} ({5}).  BlockCount={2}", entity.DisplayName, entity.EntityId, ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count, p.Key, s.Value, p.Value ) );

									found = true;
									break;
								}
							}
						}
					}
				}

				if ( hasBlockSubTypeLimits && found )
				{
					foreach ( KeyValuePair<string, int> p in subTypeDict )
					{
						foreach ( KeyValuePair<string, int> s in blockSubTypes )
						{
							if ( p.Key.ToLower( ).Contains( s.Key.ToLower( ) ) )
							{
								if ( p.Value > s.Value )
								{
									if ( found )
										found = false;

									Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which is over limit of block type {3} at {4}.  BlockCount={2}", entity.DisplayName, entity.EntityId, ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count, s.Key, p.Value ) );
									break;
								}
							}
						}
					}
				}

				if ( !found )
					entitiesFound.Add( entity );
			}

			foreach ( IMyEntity entity in entitiesFound )
			{
				Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which has unconnected and has parameters specified.  BlockCount={2}", entity.DisplayName, entity.EntityId, ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );
			}

			Communication.SendPrivateInformation( userId, string.Format( "Found {0} grids considered to be trash", entitiesFound.Count ) );
			return entitiesFound;
		}

		public static HashSet<IMyEntity> ScanGrids( ulong userId, string[ ] words )
		{
			Dictionary<string, string> options = new Dictionary<string, string>( );

			// 0 - ignore 1 - no 2 - yes
			// functional
			// terminal
			// ownership
			// power
			int functional = 0;
			int terminal = 0;
			int power = 0;
			int owner = 0;
			int online = 0;

			// debug
			// hasdisplayname
			// blocksubtype
			// blocksubtypelimit
			bool hasDisplayName = false;
			bool hasDisplayNameExact = false;
			bool hasBlockSubType = false;
			bool hasBlockSubTypeLimits = false;
			bool includesBlockType = false;
			bool excludesBlockType = false;
			bool debug = false;
			bool isOwnedBy = false;
			bool quiet = false;
            bool safeMode = true;
			bool requireBlockCount = false;
			bool requireBlockCountLess = false;
			bool isBlockSize = false;
			bool hasCustomName = false;
			bool hasCustomNameExact = false;

			string displayName = string.Empty;
			string customName = string.Empty;
			Dictionary<string, int> blockSubTypes = new Dictionary<string, int>( );
			Dictionary<string, int> blockTypes = new Dictionary<string, int>( );
			Dictionary<string, int> blockTypesExcluded = new Dictionary<string, int>( );
			long ownedByPlayerId = 0;
			int blockCount = 0;
			int blockCountLess = 0;
			int blockSize = 0;
			if ( words.Any( ) )
			{
				if ( words.FirstOrDefault( x => x.ToLower( ) == "debug" ) != null )
				{
					options.Add( "Debug", "true" );
					debug = true;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "quiet" ) != null )
				{
					options.Add( "Quiet", "true" );
					quiet = true;
				}

                if ( words.FirstOrDefault( x => x.ToLower( ) == "unsafe") != null )
                {
                    options.Add( "Unsafe", "true" );
                    safeMode = false;
                }

				if ( words.SingleOrDefault( x => x.ToLower( ) == "ownership" ) != null )
				{
					options.Add( "Ownership", "true" );
					owner = 2;
				}

				if ( words.SingleOrDefault( x => x.ToLower( ) == "noownership" ) != null )
				{
					options.Add( "Ownership", "false" );
					owner = 1;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "functional" ) != null )
				{
					options.Add( "Functional", "true" );
					functional = 2;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "nofunctional" ) != null )
				{
					options.Add( "Functional", "false" );
					functional = 1;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "terminal" ) != null )
				{
					options.Add( "Terminal", "true" );
					terminal = 2;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "noterminal" ) != null )
				{
					options.Add( "Terminal", "false" );
					terminal = 1;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "power" ) != null )
				{
					options.Add( "Has Power", "true" );
					power = 2;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "nopower" ) != null )
				{
					options.Add( "Has Power", "false" );
					power = 1;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "notonline" ) != null )
				{
					options.Add( "Online", "false" );
					online = 1;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ) == "online" ) != null )
				{
					options.Add( "Online", "true" );
					online = 2;
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ).Split( ':' );
					if ( parts.Length > 1 )
					{
						hasDisplayName = true;
						displayName = parts[ 1 ];
						options.Add( "Matches Display Name Text", "true:" + displayName );
						//Console.WriteLine("Here: {0}", parts[2]);
						if ( parts.Length > 2 && parts[ 2 ].ToLower( ) == "exact" )
						{
							hasDisplayNameExact = true;
							options.Add( "Matches Display Exactly", "true" );
						}
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hascustomname:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hascustomname:" ) ).Split( ':' );
					if ( parts.Length > 1 )
					{
						hasCustomName = true;
						customName = parts[ 1 ];
						options.Add( "Matches Custom Name Text", "true:" + displayName );
						//Console.WriteLine("Here: {0}", parts[2]);
						if ( parts.Length > 2 && parts[ 2 ].ToLower( ) == "exact" )
						{
							hasCustomNameExact = true;
							options.Add( "Matches Custom Name Exactly", "true" );
						}
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "includesblocksubtype:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "includesblocksubtype:" ) ).Split( ':' );
					hasBlockSubType = true;
					options.Add( "Has Sub Block Type", "true" );

					if ( parts.Length < 3 )
					{
						blockSubTypes.Add( parts[ 1 ], 1 );
						options.Add( "Sub Block Type: " + parts[ 1 ], "1" );
					}
					else
					{
						int count = 1;
						int.TryParse( parts[ 2 ], out count );
						blockSubTypes.Add( parts[ 1 ], count );
						options.Add( "Sub Block Type: " + parts[ 1 ], count.ToString( ) );
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "excludesblocksubtype:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "excludesblocksubtype:" ) ).Split( ':' );
					hasBlockSubTypeLimits = true;
					options.Add( "Exclude Has Sub Block Type", "true" );

					if ( parts.Length < 3 )
					{
						blockSubTypes.Add( parts[ 1 ], 1 );
						options.Add( "Sub Block Type Limit: " + parts[ 1 ], "1" );
					}
					else
					{
						int count = 1;
						int.TryParse( parts[ 2 ], out count );
						blockSubTypes.Add( parts[ 1 ], count );
						options.Add( "Sub Block Type Limit: " + parts[ 1 ], count.ToString( ) );
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "ownedby:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "ownedby:" ) ).Split( ':' );
					if ( parts.Length > 1 )
					{
						isOwnedBy = true;
						string ownedBy = parts[ 1 ];
						if ( PlayerMap.Instance.GetPlayerItemsFromPlayerName( ownedBy ).Count > 0 )
							ownedByPlayerId = PlayerMap.Instance.GetPlayerItemsFromPlayerName( ownedBy ).First( ).PlayerId;

						options.Add( "Owned By", ownedBy );
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "includesblocktype:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "includesblocktype:" ) ).Split( ':' );
					includesBlockType = true;
					options.Add( "Includes Block Type", "true" );

					if ( parts.Length < 3 )
					{
						blockTypes.Add( parts[ 1 ], 1 );
						options.Add( "Includes Block Type Count: " + parts[ 1 ], "1" );
					}
					else
					{
						int count = 1;
						int.TryParse( parts[ 2 ], out count );
						blockTypes.Add( parts[ 1 ], count );
						options.Add( "Includes Block Type Count: " + parts[ 1 ], count.ToString( ) );
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "excludesblocktype:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "excludesblocktype:" ) ).Split( ':' );
					excludesBlockType = true;
					options.Add( "Excludes Block Type", "true" );

					if ( parts.Length < 3 )
					{
						blockTypesExcluded.Add( parts[ 1 ], 1 );
						options.Add( "Excludes Block Type Count: " + parts[ 1 ], "1" );
					}
					else
					{
						int count = 1;
						int.TryParse( parts[ 2 ], out count );
						blockTypesExcluded.Add( parts[ 1 ], count );
						options.Add( "Excludes Block Type Count: " + parts[ 1 ], count.ToString( ) );
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blockcount:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blockcount:" ) ).Split( ':' );
					requireBlockCount = true;
					options.Add( "Requires Block Count", "true" );

					if ( parts.Length < 2 )
					{
						blockCount = 1;
						options.Add( "Block Count:", "1" );
					}
					else
					{
						int count = 1;
						int.TryParse( parts[ 1 ], out count );
						blockCount = count;
						options.Add( "Block Count:", blockCount.ToString( ) );
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blockcountlessthan:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blockcountlessthan:" ) ).Split( ':' );
					requireBlockCountLess = true;
					options.Add( "Requires Block Count Less Than", "true" );

					if ( parts.Length < 2 )
					{
						blockCountLess = 1;
						options.Add( "Block Count:", "1" );
					}
					else
					{
						int count = 1;
						int.TryParse( parts[ 1 ], out count );
						blockCountLess = count;
						options.Add( "Block Count:", blockCount.ToString( ) );
					}
				}

				if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blocksize:" ) ) != null )
				{
					string[ ] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blocksize:" ) ).Split( ':' );

					if ( parts[ 1 ].ToLower( ) == "small" )
					{
						options.Add( "Is Block Size", "small" );
						isBlockSize = true;
						blockSize = 0;
					}
					else if ( parts[ 1 ].ToLower( ) == "large" )
					{
						options.Add( "Is Block Size", "large" );
						isBlockSize = true;
						blockSize = 1;
					}
					else if ( parts[ 1 ].ToLower( ) == "station" )
					{
						options.Add( "Is Block Size", "station" );
						isBlockSize = true;
						blockSize = 2;
					}
					else if ( parts[ 1 ].ToLower( ) == "largeship" )
					{
						options.Add( "Is Block Size", "largeship" );
						isBlockSize = true;
						blockSize = 3;
					}
				}

			}

			if ( options.Count < 1 && quiet )
			{
				Communication.SendPrivateInformation( userId, "No options supplied for quiet scan, cancelling due to possible error" );
				return new HashSet<IMyEntity>( );
			}

			if ( words.Length > options.Count )
			{
				Communication.SendPrivateInformation( userId, "Possible problem with your parameters (options provided is larger than options found).  Not returning any results in case of error" );
				return new HashSet<IMyEntity>( );
			}

			if ( !quiet )
				Communication.SendPrivateInformation( userId, string.Format( "Scanning for ships with options: {0}", GetOptionsText( options ) ) );

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
            Wrapper.GameAction( ( ) =>
            {
            	MyAPIGateway.Entities.GetEntities( entities, x => x is IMyCubeGrid );
            } );
            

            HashSet<IMyEntity> entitiesToConfirm = new HashSet<IMyEntity>( );
			HashSet<IMyEntity> entitiesUnconnected = new HashSet<IMyEntity>( );
			HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>( );
			foreach ( IMyEntity entity in entities )
			{
				if ( !( entity is IMyCubeGrid ) )
					continue;

                if (entity.DisplayName.Contains("CommRelay"))
                    continue;
                //this should make CommRelays immune to chat scan/delete commands

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				MyObjectBuilder_CubeGrid gridBuilder;
				try
				{
					gridBuilder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( );
				}
				catch
				{
					continue;
				}

				if ( PluginSettings.Instance.LoginEntityWhitelist.Contains( entity.EntityId.ToString( ) ) || PluginSettings.Instance.LoginEntityWhitelist.Contains( entity.DisplayName ) )
					continue;


				if ( hasDisplayName && displayName != string.Empty )
				{
					if ( !hasDisplayNameExact && entity.DisplayName.Contains( displayName ) )
						entitiesToConfirm.Add( entity );

					if ( hasDisplayNameExact && entity.DisplayName.Equals( displayName ) )
						entitiesToConfirm.Add( entity );
				}
				else if ( hasCustomName && HasCustomName( gridBuilder, customName, hasCustomNameExact ) )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( hasCustomName )
				{
					continue;
				}
				else if ( isOwnedBy && ownedByPlayerId > 0 && GetAllOwners( gridBuilder ).Contains( ownedByPlayerId ) )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( !isOwnedBy && owner == 0 )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( !isOwnedBy && owner == 2 && HasOwner( gridBuilder ) )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( !isOwnedBy && owner == 1 && !HasOwner( gridBuilder ) )
				{
					entitiesToConfirm.Add( entity );
				}
			}
			Dictionary<string, int> subTypeDict = new Dictionary<string, int>( );
			Dictionary<string, int> typeDict = new Dictionary<string, int>( );
			List<string> checkList = new List<string>( );

            //this is an ugly workaround to not being able to combine grids on pistons/rotors with the main grid
            //with safe mode on we remove any grid with a piston or rotor from the list of grids to process

            if (safeMode)
                GetGridsUnconnected(entitiesUnconnected, entitiesToConfirm);
            else
                entitiesUnconnected = entitiesToConfirm;
            
            //int blocks = 0;
            

            foreach(IMyEntity entity in entitiesUnconnected)
            {
                subTypeDict.Clear( );
				typeDict.Clear( );
				checkList.Clear( );
				MyObjectBuilder_CubeGrid grid = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( );
                
				if ( online == 1 ) // notonline
				{
					bool foundOnline = false;
					foreach ( ulong player in PlayerManager.Instance.ConnectedPlayers )
					{
						long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId( player );
						if ( playerId < 1 )
							continue;

						if ( GetAllOwners( grid ).Contains( playerId ) )
						{
							foundOnline = true;
							break;
						}
					}

					if ( foundOnline )
						continue;
				}
				else if ( online == 2 ) // online
				{
					bool foundOnline = true;
					foreach ( ulong player in PlayerManager.Instance.ConnectedPlayers )
					{
						long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId( player );
						if ( playerId < 1 )
							continue;

						if ( GetAllOwners( grid ).Contains( playerId ) )
						{
							foundOnline = false;
							break;
						}
					}

					if ( foundOnline )
						continue;
				}

				bool found = true;
				foreach ( MyObjectBuilder_CubeBlock block in grid.CubeBlocks )
				{
					if ( functional != 0 )
					{
						if ( block is MyObjectBuilder_FunctionalBlock )
						{
							//							if (debug && !found)
							//								Communication.SendPrivateInformation(userId, string.Format("Found grid '{0}' ({1}) which has a functional block.  BlockCount={2}", entity.DisplayName, entity.EntityId, ((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder()).CubeBlocks.Count));

							if ( !checkList.Contains( "functional" ) )
								checkList.Add( "functional" );
						}
					}

					if ( terminal != 0 )
					{
						if ( block is MyObjectBuilder_TerminalBlock )
						{
							//if (debug && !found)
							//	Communication.SendPrivateInformation(userId, string.Format("Found grid '{0}' ({1}) which has a terminal block.  BlockCount={2}", entity.DisplayName, entity.EntityId, ((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder()).CubeBlocks.Count));

							if ( !checkList.Contains( "terminal" ) )
								checkList.Add( "terminal" );
						}
					}

					if ( power != 0 )
					{
						if ( DoesBlockSupplyPower( block ) )
						{
							//if (debug && !found)
							//	Communication.SendPrivateInformation(userId, string.Format("Found grid '{0}' ({1}) which has power.  BlockCount={2}", entity.DisplayName, entity.EntityId, ((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder()).CubeBlocks.Count));

							if ( !checkList.Contains( "power" ) )
								checkList.Add( "power" );
						}
					}

					if ( hasBlockSubType || hasBlockSubTypeLimits )
					{
						string subTypeName = block.GetId( ).SubtypeName;
						if ( subTypeDict.ContainsKey( subTypeName ) )
							subTypeDict[ subTypeName ] = subTypeDict[ subTypeName ] + 1;
						else
							subTypeDict.Add( subTypeName, 1 );
					}

					if ( includesBlockType || excludesBlockType )
					{
						string typeName = block.GetId( ).TypeId.ToString( );
						if ( typeDict.ContainsKey( typeName ) )
							typeDict[ typeName ] = typeDict[ typeName ] + 1;
						else
							typeDict.Add( typeName, 1 );
					}
				}

				if ( functional != 0 )
				{
					if ( !checkList.Contains( "functional" ) && functional == 2 )
						found = false;

					if ( checkList.Contains( "functional" ) && functional == 1 )
						found = false;
				}

				if ( terminal != 0 )
				{
					if ( !checkList.Contains( "terminal" ) && terminal == 2 )
						found = false;

					if ( checkList.Contains( "terminal" ) && terminal == 1 )
						found = false;
				}

				if ( power != 0 )
				{
					if ( !checkList.Contains( "power" ) && power == 2 )
						found = false;

					if ( checkList.Contains( "power" ) && power == 1 )
						found = false;
				}

				if ( hasBlockSubType )
				{
					found = ApplyBlockSubTypeFilter( userId, subTypeDict, blockSubTypes, debug, quiet, entity, found );
				}

				if ( includesBlockType )
				{
					found = ApplyBlockInclusionFilter( userId, typeDict, blockTypes, debug, quiet, entity, found );
				}

				if ( hasBlockSubTypeLimits && found )
				{
					found = ApplyBlockSubTypeExclusionFilter( userId, subTypeDict, blockSubTypes, found, quiet, debug, entity );
				}

				if ( excludesBlockType && found )
					found = ApplyBlockExclusionFilter( userId, typeDict, blockTypes, quiet, debug, entity );

				if ( requireBlockCount && found && grid.CubeBlocks.Count < blockCount )
				{
					found = false;
				}

				if ( requireBlockCountLess && found && grid.CubeBlocks.Count >= blockCountLess )
				{
					found = false;
				}

				if ( isBlockSize && found && blockSize == 0 && grid.GridSizeEnum != MyCubeSize.Small )
				{
					found = false;
				}

				if ( isBlockSize && found && blockSize == 1 && grid.GridSizeEnum != MyCubeSize.Large )
				{
					found = false;
				}

				if ( isBlockSize && found && blockSize == 2 && ( grid.GridSizeEnum != MyCubeSize.Large || grid.GridSizeEnum == MyCubeSize.Large && !grid.IsStatic ) )
				{
					found = false;
				}

				if ( isBlockSize && found && blockSize == 2 && ( grid.GridSizeEnum != MyCubeSize.Large || grid.GridSizeEnum == MyCubeSize.Large && grid.IsStatic ) )
				{
					found = false;
				}

				if ( found )
					entitiesFound.Add( entity );
			}

			foreach ( IMyEntity entity in entitiesFound )
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;
				MyObjectBuilder_CubeGrid gridBuilder = SafeGetObjectBuilder( grid );
				string ownerName = "none";
				if ( GetBigOwners( gridBuilder ).Count > 0 )
				{
					long ownerId = GetBigOwners( gridBuilder ).First( );
					ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId( ownerId ).Name;
				}

				if ( !quiet )
					Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) (Owner: {2}) which has specified parameters.  BlockCount={3}", entity.DisplayName, entity.EntityId, ownerName, ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );
			}

			if ( !quiet )
				Communication.SendPrivateInformation( userId, string.Format( "Found {0} grids", entitiesFound.Count ) );

			return entitiesFound;
		}

		private static bool ApplyBlockSubTypeExclusionFilter( ulong userId, Dictionary<string, int> subTypeDict, Dictionary<string, int> blockSubTypes, bool found, bool quiet, bool debug, IMyEntity entity )
		{
			foreach ( KeyValuePair<string, int> pairBlockTypesInGrid in subTypeDict )
			{
				foreach ( KeyValuePair<string, int> pairBlockTypesFilter in blockSubTypes )
				{
					if ( pairBlockTypesInGrid.Key.ToLower( ).Contains( pairBlockTypesFilter.Key.ToLower( ) ) )
					{
						if ( pairBlockTypesInGrid.Value >= pairBlockTypesFilter.Value )
						{
							if ( found )
							{
								found = false;
							}

							if ( !quiet && debug )
							{
								Communication.SendPrivateInformation( userId,
								                                      string.Format( "Exclusion: Found grid '{0}' ({1}) which excludes block type of {3} at {4}.  BlockCount={2}",
								                                                     entity.DisplayName,
								                                                     entity.EntityId,
								                                                     ( (MyObjectBuilder_CubeGrid) entity.GetObjectBuilder( ) ).CubeBlocks.Count,
								                                                     pairBlockTypesFilter.Key,
								                                                     pairBlockTypesInGrid.Value ) );
							}

							break;
						}
					}
				}
			}
			return found;
		}

		private static bool ApplyBlockSubTypeFilter( ulong userId, Dictionary<string, int> subTypeDict, Dictionary<string, int> blockSubTypes, bool debug, bool quiet, IMyEntity entity, bool found )
		{
			bool hasType = false;
			foreach ( KeyValuePair<string, int> pairBlockTypesInGrid in subTypeDict )
			{
				foreach ( KeyValuePair<string, int> pairBlockTypesFilter in blockSubTypes )
				{
					if ( pairBlockTypesInGrid.Key.ToLower( ).Contains( pairBlockTypesFilter.Key.ToLower( ) ) )
					{
						if ( pairBlockTypesInGrid.Value >= pairBlockTypesFilter.Value )
						{
							if ( debug && !quiet )
							{
								Communication.SendPrivateInformation( userId,
								                                      string.Format( "Found grid '{0}' ({1}) which contains at least {4} of block type {3} ({5}).  BlockCount={2}",
								                                                     entity.DisplayName,
								                                                     entity.EntityId,
								                                                     ( (MyObjectBuilder_CubeGrid) entity.GetObjectBuilder( ) ).CubeBlocks.Count,
								                                                     pairBlockTypesInGrid.Key,
								                                                     pairBlockTypesFilter.Value,
								                                                     pairBlockTypesInGrid.Value ) );
							}

							hasType = true;
							break;
						}
					}
				}
			}

			if ( !hasType )
			{
				if ( debug && !quiet )
				{
					Communication.SendPrivateInformation( userId,
					                                      string.Format( "Found grid '{0}' ({1}) which does not contain block type.  BlockCount={2}",
					                                                     entity.DisplayName,
					                                                     entity.EntityId,
					                                                     ( (MyObjectBuilder_CubeGrid) entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );
				}

				found = false;
			}
			return found;
		}

		private static bool ApplyBlockInclusionFilter( ulong userId, Dictionary<string, int> typeDict, Dictionary<string, int> blockTypes, bool debug, bool quiet, IMyEntity entity, bool found )
		{
			bool hasType = false;
			foreach ( KeyValuePair<string, int> pairBlockTypesInGrid in typeDict )
			{
				foreach ( KeyValuePair<string, int> pairBlockTypesFilter in blockTypes )
				{
					if ( pairBlockTypesInGrid.Key.ToLower( ).Contains( pairBlockTypesFilter.Key.ToLower( ) ) )
					{
						if ( pairBlockTypesInGrid.Value >= pairBlockTypesFilter.Value )
						{
							if ( debug && !quiet )
							{
								Communication.SendPrivateInformation( userId,
								                                      string.Format( "Found grid '{0}' ({1}) which contains at least {4} of block type {3} ({5}).  BlockCount={2}",
								                                                     entity.DisplayName,
								                                                     entity.EntityId,
								                                                     ( (MyObjectBuilder_CubeGrid) entity.GetObjectBuilder( ) ).CubeBlocks.Count,
								                                                     pairBlockTypesInGrid.Key,
								                                                     pairBlockTypesFilter.Value,
								                                                     pairBlockTypesInGrid.Value ) );
							}

							hasType = true;
							break;
						}
					}
				}
			}

			if ( !hasType )
			{
				if ( debug && !quiet )
				{
					Communication.SendPrivateInformation( userId,
					                                      string.Format( "Found grid '{0}' ({1}) which does not contain block type.  BlockCount={2}",
					                                                     entity.DisplayName,
					                                                     entity.EntityId,
					                                                     ( (MyObjectBuilder_CubeGrid) entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );
				}

				found = false;
			}
			return found;
		}

		private static bool ApplyBlockExclusionFilter( ulong userId, Dictionary<string, int> typeDict, Dictionary<string, int> blockTypes, bool quiet, bool debug, IMyEntity entity )
		{
			bool found = true;
			foreach ( KeyValuePair<string, int> blockTypesInGrid in typeDict )
			{
				foreach ( KeyValuePair<string, int> pairBlockTypesFilter in blockTypes )
				{
					if ( blockTypesInGrid.Key.ToLower( ).Contains( pairBlockTypesFilter.Key.ToLower( ) ) )
					{
						if ( blockTypesInGrid.Value >= pairBlockTypesFilter.Value )
						{
							if ( found )
							{
								found = false;
							}

							if ( !quiet && debug )
							{
								Communication.SendPrivateInformation( userId,
																	  string.Format( "Exclusion: Found grid '{0}' ({1}) which excludes block type of {3} at {4}.  BlockCount={2}",
																					 entity.DisplayName,
																					 entity.EntityId,
																					 ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count,
																					 pairBlockTypesFilter.Key,
																					 blockTypesInGrid.Value ) );
							}

							break;
						}
					}
				}
			}
			return found;
		}

		public static void DeleteGrids( HashSet<IMyEntity> entities )
		{
			Wrapper.GameAction( ( ) =>
			{
				foreach ( IMyEntity entity in entities )
				{
					if ( !( entity is IMyCubeGrid ) )
						continue;

					MyObjectBuilder_CubeGrid gridBuilder = SafeGetObjectBuilder( (IMyCubeGrid)entity );
					long ownerId = 0;
					string ownerName = "";
					if ( GetBigOwners( gridBuilder ).Count > 0 )
					{
						ownerId = GetBigOwners( gridBuilder ).First( );
						ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId( ownerId ).Name;
					}

					Log.Info( "Cleanup", string.Format( "Cleanup Removed Grid - Id: {0} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName, ownerId, ownerName ) );
					BaseEntityNetworkManager.BroadcastRemoveEntity( entity, false );
				}
			} );
		}

		public static bool GetOwner( MyObjectBuilder_CubeGrid grid, out long ownerId )
		{
			ownerId = 0;
			foreach ( MyObjectBuilder_CubeBlock block in grid.CubeBlocks )
			{
				if ( !( block is MyObjectBuilder_TerminalBlock ) )
					continue;

				MyObjectBuilder_TerminalBlock functional = (MyObjectBuilder_TerminalBlock)block;
				if ( functional.Owner != 0 )
				{
					ownerId = functional.Owner;
					return true;
				}
			}

			return false;
		}

		public static bool IsFullOwner( MyObjectBuilder_CubeGrid grid, long ownerId, IMyPlayer factionPlayer = null )
		{
			bool found = false;
			foreach ( MyObjectBuilder_CubeBlock block in grid.CubeBlocks )
			{
				if ( !( block is MyObjectBuilder_TerminalBlock ) )
					continue;

				MyObjectBuilder_TerminalBlock functional = (MyObjectBuilder_TerminalBlock)block;
				if ( factionPlayer == null )
				{
					if ( functional.Owner != 0 && functional.Owner != ownerId )
					{
						return false;
					}
					else if ( functional.Owner != 0 )
					{
						found = true;
					}
				}
				else
				{
					MyRelationsBetweenPlayerAndBlock relation = factionPlayer.GetRelationTo( functional.Owner );
					if ( functional.Owner != 0 && ( relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare ) )
					{
						found = true;
					}
					else if ( functional.Owner != 0 && relation != MyRelationsBetweenPlayerAndBlock.FactionShare && relation != MyRelationsBetweenPlayerAndBlock.FactionShare )
					{
						return false;
					}
				}
			}

			return found;
		}

		// might not work? -- updated, needs testing
		public static List<long> GetBigOwners( MyObjectBuilder_CubeGrid grid )
		{
			Dictionary<long, int> ownerList = new Dictionary<long, int>( );
			foreach ( MyObjectBuilder_CubeBlock block in grid.CubeBlocks )
			{
				if ( block.Owner == 0 )
					continue;

				if ( ownerList.ContainsKey( block.Owner ) )
					ownerList[ block.Owner ] = ownerList[ block.Owner ] + 1;
				else
					ownerList.Add( block.Owner, 1 );
			}

			int count = ownerList.OrderBy( x => x.Value ).Select( x => x.Value ).FirstOrDefault( );
			return ownerList.OrderBy( x => x.Value ).Where( x => x.Value == count ).Select( x => x.Key ).ToList( );
		}

		public static List<long> GetAllOwners( MyObjectBuilder_CubeGrid grid )
		{
			Dictionary<long, int> ownerList = new Dictionary<long, int>( );
			foreach ( MyObjectBuilder_CubeBlock block in grid.CubeBlocks )
			{
				if ( block.Owner == 0 )
					continue;

				if ( ownerList.ContainsKey( block.Owner ) )
					ownerList[ block.Owner ] = ownerList[ block.Owner ] + 1;
				else
					ownerList.Add( block.Owner, 1 );
			}

			return ownerList.Select( x => x.Key ).ToList( );
		}

		public static bool HasCustomName( MyObjectBuilder_CubeGrid grid, string name, bool exact )
		{
			foreach ( MyObjectBuilder_CubeBlock block in grid.CubeBlocks )
			{
				MyObjectBuilder_TerminalBlock termBlock = block as MyObjectBuilder_TerminalBlock;
				if ( termBlock != null )
				{
					if ( exact )
					{
						if ( termBlock.CustomName != null && termBlock.CustomName == name )
						{
							return true;
						}
					}
					else
					{
						if ( termBlock.CustomName != null && termBlock.CustomName.Contains( name ) )
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public static bool HasOwner( MyObjectBuilder_CubeGrid grid )
		{
			foreach ( MyObjectBuilder_CubeBlock block in grid.CubeBlocks )
			{
				if ( !( block is MyObjectBuilder_TerminalBlock ) )
					continue;

				MyObjectBuilder_TerminalBlock functional = (MyObjectBuilder_TerminalBlock)block;
				if ( functional.Owner != 0 )
					return true;
			}

			return false;
		}


		private static string GetOptionsText( Dictionary<string, string> options )
		{
			return string.Join( ", ", options.Select( o => string.Format( "{0}={1}", o.Key, o.Value ) ) );
		}

		public static bool DoesBlockSupplyPower( MyObjectBuilder_CubeBlock block )
		{
			MyObjectBuilder_BatteryBlock battery = block as MyObjectBuilder_BatteryBlock;
			if ( battery != null )
			{
				if ( battery.CurrentStoredPower > 0f )
					return true;
			}

			MyObjectBuilder_Reactor reactor = block as MyObjectBuilder_Reactor;
			if ( reactor != null )
			{
				if ( reactor.Inventory.Items.Count > 0 )
					return true;
			}

			if ( block is MyObjectBuilder_SolarPanel )
			{
				return true;
			}

			return false;
		}

		public static bool DoesGridHavePowerSupply( MyObjectBuilder_CubeGrid grid )
		{
			return grid.CubeBlocks.Any( DoesBlockSupplyPower );
		}

		public static MyObjectBuilder_CubeGrid SafeGetObjectBuilder( IMyCubeGrid grid )
		{
			MyObjectBuilder_CubeGrid gridBuilder = null;
			try
			{
				gridBuilder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( );
			}
			catch
			{
			}

			return gridBuilder;
		}


		/// <summary>
		/// This only returns one grid per connected grid.  So if a grid has a connector and 4 pistons, it will count as 1 grid, not 5.
		/// </summary>
		/// <param name="grids"></param>
		/// <param name="collect"></param>
		public static void GetConnectedGrids( HashSet<IMyEntity> grids, Func<IMyEntity, bool> collect = null)
		{
			List<IMySlimBlock> currentBlocks = new List<IMySlimBlock>( );
			HashSet<IMyEntity> gridsProcessed = new HashSet<IMyEntity>( );
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );

  
                MyAPIGateway.Entities.GetEntities(entities, collect);

			foreach ( IMyEntity entity in entities )
			{
				if ( !( entity is IMyCubeGrid ) )
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				if ( gridsProcessed.Contains( grid ) )
					continue;

				grids.Add( grid );
				GetGridBlocks( grid, currentBlocks );
				foreach ( IMyCubeGrid connectedGrid in GetConnectedGridList( gridsProcessed, currentBlocks ) )
				{
					gridsProcessed.Add( connectedGrid );
				}
			}
		}

		/// <summary>
		/// Gets all the blocks from all valid connected grids.  So a grid connected to another grid that also has a few pistons with blocks on it will return
		/// all the blocks for the connected grids as well as all the blocks for any connected pistons.  (ug)
		/// </summary>
		/// <param name="gridsProcessed"></param>
		/// <param name="grid"></param>
		/// <param name="allBlocks"></param>
		/// <param name="collect"></param>
		public static void GetAllConnectedBlocks( HashSet<IMyEntity> gridsProcessed, IMyCubeGrid grid, List<IMySlimBlock> allBlocks, Func<IMySlimBlock, bool> collect = null )
		{
			List<IMySlimBlock> currentBlocks = new List<IMySlimBlock>( );
			List<IMyCubeGrid> connectedGrids = new List<IMyCubeGrid>( );
            
			connectedGrids.Add( grid );
			while ( connectedGrids.Count > 0 )
			{
				IMyCubeGrid currentGrid = connectedGrids.First( );
				connectedGrids.Remove( currentGrid );
				if ( gridsProcessed.Contains( currentGrid ) )
					continue;
                
				gridsProcessed.Add( currentGrid );

				GetGridBlocks( currentGrid, currentBlocks );
				connectedGrids.AddRange( GetConnectedGridList( gridsProcessed, currentBlocks ) );
                ///
                
				allBlocks.AddRange( collect != null ? currentBlocks.FindAll( s => collect( s ) ) : currentBlocks );
			}
		}

		public static HashSet<IMyEntity> GetRecursiveGridList( IMyCubeGrid startGrid, HashSet<IMyEntity> ignoreList = null )
		{
			HashSet<IMyEntity> results = new HashSet<IMyEntity>( );
			List<IMySlimBlock> currentBlocks = new List<IMySlimBlock>( );

			results.Add( startGrid );
			GetGridBlocks( startGrid, currentBlocks );
			foreach ( IMyCubeGrid connectedGrid in GetConnectedGridList( results, currentBlocks ) )
			{
				if ( ignoreList != null && ignoreList.Contains( connectedGrid ) )
					continue;

				results.Add( connectedGrid );
				foreach ( IMyEntity entity in GetRecursiveGridList( connectedGrid, results ) )
				{
					results.Add( entity );
				}
			}

			return results;
		}

		private static List<IMyCubeGrid> GetConnectedGridList( HashSet<IMyEntity> checkedGrids, List<IMySlimBlock> blocks )
		{
			List<IMyCubeGrid> connectedGrids = new List<IMyCubeGrid>( );
			foreach ( IMySlimBlock slimBlock in blocks )
			{
				if ( slimBlock.FatBlock != null )
				{
					IMyCubeBlock cubeBlock = slimBlock.FatBlock;

					// Check for Piston
					if ( cubeBlock.BlockDefinition.TypeId == typeof( MyObjectBuilder_PistonBase ) )
					{
						MyObjectBuilder_PistonBase pistonBase = (MyObjectBuilder_PistonBase)cubeBlock.GetObjectBuilderCubeBlock( );
						IMyEntity entity;
						if ( MyAPIGateway.Entities.TryGetEntityById( pistonBase.TopBlockId, out entity ) )
						{
							IMyCubeGrid parent = (IMyCubeGrid)entity.Parent;
							if ( !checkedGrids.Contains( parent ) )
								connectedGrids.Add( parent );
						}
					}
					else if ( cubeBlock.BlockDefinition.TypeId == typeof( MyObjectBuilder_ExtendedPistonBase ) )
					{
						MyObjectBuilder_PistonBase pistonBase = (MyObjectBuilder_PistonBase)cubeBlock.GetObjectBuilderCubeBlock( );
						IMyEntity entity;
						if ( MyAPIGateway.Entities.TryGetEntityById( pistonBase.TopBlockId, out entity ) )
						{
							IMyCubeGrid parent = (IMyCubeGrid)entity.Parent;
							if ( !checkedGrids.Contains( parent ) )
								connectedGrids.Add( parent );
						}
					}
					// Connector    
					else if ( cubeBlock.BlockDefinition.TypeId == typeof( MyObjectBuilder_ShipConnector ) )
					{
						MyObjectBuilder_ShipConnector connector = (MyObjectBuilder_ShipConnector)cubeBlock.GetObjectBuilderCubeBlock( );
						IMyEntity entity;
						if ( MyAPIGateway.Entities.TryGetEntityById( connector.ConnectedEntityId, out entity ) )
						{
							IMyCubeGrid parent = (IMyCubeGrid)entity.Parent;
							if ( !checkedGrids.Contains( parent ) )
								connectedGrids.Add( parent );
						}
					}
					else if ( cubeBlock.BlockDefinition.TypeId == typeof( MyObjectBuilder_MotorAdvancedStator ) )
					{
						MyObjectBuilder_MotorAdvancedStator stator = (MyObjectBuilder_MotorAdvancedStator)cubeBlock.GetObjectBuilderCubeBlock( );
						IMyEntity connectedEntity;
						if ( MyAPIGateway.Entities.TryGetEntityById( (long) stator.RotorEntityId, out connectedEntity ) )
						{
							IMyCubeGrid parent = (IMyCubeGrid)connectedEntity.Parent;
							if ( !checkedGrids.Contains( parent ) )
								connectedGrids.Add( parent );
						}
					}
				}
			}

			return connectedGrids;
		}

		private static void GetGridBlocks( IMyCubeGrid grid, List<IMySlimBlock> blockList, Func<IMySlimBlock, bool> collect = null )
		{
			blockList.Clear( );
			List<IMySlimBlock> blocks = new List<IMySlimBlock>( );
			grid.GetBlocks( blocks, collect );
			blockList.AddRange( blocks );
		}
	}
}
