using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using VRageMath;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.Utility
{
	public enum RemoveGridTypes
	{
		All,
		Ships,
		Stations
	}

	public static class CubeGrids
	{
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
							MyAPIGateway.Entities.TryGetEntityById( stator.RotorEntityId, out connectedEntity );

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
							MyAPIGateway.Entities.TryGetEntityById( stator.RotorEntityId, out connectedEntity );

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

		private class ScanOptions
		{
			internal bool HasDisplayName { get; set; }
			internal bool HasDisplayNameExact { get; set; }
			internal string DisplayName { get; set; }

			internal bool HasCustomName { get; set; }
			internal bool HasCustomNameExact { get; set; }
			internal string CustomName { get; set; }

			internal ScanBehavior HasFunctional = ScanBehavior.Ignore;
			internal ScanBehavior HasTerminal = ScanBehavior.Ignore;
			internal ScanBehavior HasPower = ScanBehavior.Ignore;
			internal ScanBehavior HasOwner = ScanBehavior.Ignore;
			internal ScanBehavior OwnerOnline = ScanBehavior.Ignore;

			internal bool HasBlockSubType { get; set; }
			internal bool HasBlockSubTypeLimits { get; set; }
			internal bool IncludesBlockType { get; set; }
			internal bool ExcludesBlockType { get; set; }
			internal bool Debug { get; set; }
			internal bool IsOwnedBy { get; set; }
			internal bool Quiet { get; set; }
			internal bool RequireBlockCount { get; set; }
			internal bool RequireBlockCountLess { get; set; }
			internal bool IsBlockSize { get; set; }

			internal Dictionary<string, int> BlockSubTypes { get { return _blockSubTypes; } }
			internal Dictionary<string, int> BlockTypes { get { return _blockTypes; } }
			internal Dictionary<string, int> BlockTypesExcluded { get { return _blockTypesExcluded; } }

			private readonly Dictionary<string, int> _blockSubTypes = new Dictionary<string, int>( );
			private readonly Dictionary<string, int> _blockTypes = new Dictionary<string, int>( );
			private readonly Dictionary<string, int> _blockTypesExcluded = new Dictionary<string, int>( );

			internal long OwnedByPlayerId { get; set; }
			internal int BlockCount { get; set; }
			internal int BlockCountLess { get; set; }
			internal int BlockSize { get; set; }
		}

		private enum ScanBehavior
		{
			Ignore = 0,
			No = 1,
			Yes = 2
		}

		public static HashSet<IMyEntity> ScanGrids( ulong userId, string line )
		{
			string[ ] words = line.Split( new[ ] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries );
			Dictionary<string, string> options = new Dictionary<string, string>( );

			// debug
			// hasdisplayname
			// blocksubtype
			// blocksubtypelimit
			ScanOptions scanOptions = new ScanOptions( );

			for ( int wordIndex = 2; wordIndex < words.Length; wordIndex++ )
			{
				switch ( words[ wordIndex ].ToLowerInvariant( ) )
				{
					case "debug":
						options.Add( "Debug", "true" );
						scanOptions.Debug = true;
						break;
					case "quiet":
						options.Add( "Quiet", "true" );
						scanOptions.Quiet = true;
						break;
					case "ownership":
						options.Add( "Ownership", "true" );
						scanOptions.HasOwner = ScanBehavior.Yes;
						break;
					case "noownership":
						options.Add( "Ownership", "false" );
						scanOptions.HasOwner = ScanBehavior.No;
						break;
					case "functional":
						options.Add( "Functional", "true" );
						scanOptions.HasFunctional = ScanBehavior.Yes;
						break;
					case "nofunctional":
						options.Add( "Functional", "false" );
						scanOptions.HasFunctional = ScanBehavior.No;
						break;
					case "terminal":
						options.Add( "Terminal", "true" );
						scanOptions.HasTerminal = ScanBehavior.Yes;
						break;
					case "noterminal":
						options.Add( "Terminal", "false" );
						scanOptions.HasTerminal = ScanBehavior.No;
						break;
					case "power":
						options.Add( "Has Power", "true" );
						scanOptions.HasPower = ScanBehavior.Yes;
						break;
					case "nopower":
						options.Add( "Has Power", "false" );
						scanOptions.HasPower = ScanBehavior.No;
						break;
					case "online":
						options.Add( "Online", "true" );
						scanOptions.OwnerOnline = ScanBehavior.Yes;
						break;
					case "notonline":
						options.Add( "Online", "false" );
						scanOptions.OwnerOnline = ScanBehavior.No;
						break;
					case "hasdisplayname":
						ParseDisplayNameArgument( ref wordIndex, words, options, scanOptions );
						break;
					case "hascustomname":
						ParseCustomNameArgument( ref wordIndex, words, options, scanOptions );
						break;
					case "includesblocksubtype":
						ParseIncludesBlockSubtypeArgument( ref wordIndex, words, options, scanOptions );
						break;
					case "excludesblocksubtype":
						ParseExcludesBlockSubtypeArgument( ref wordIndex, words, options, scanOptions );
						break;
					case "ownedby":
						ParseOwnedbyArgument( ref wordIndex, words, options, scanOptions );
						break;
					case "includesblocktype":
						ParseIncludesBlockTypeArgument( ref wordIndex, words, options, scanOptions );
						break;
					case "excludesblocktype":
						ParseExcludesBlockTypeArgument( ref wordIndex, words, options, scanOptions );
						break;
					case "blockcount":
						ParseBlockCountArgument( ref wordIndex, words, options, scanOptions );
						break;
					case "blockcountlessthan":
						ParseBlockCountLessThanArgument( ref wordIndex, words, options, scanOptions );
						break;
					case "blocksize":
						ParseBlocksizeArgument( ref wordIndex, words, options, scanOptions );
						break;
				}
			}

			if ( options.Count < 1 && scanOptions.Quiet )
			{
				Communication.SendPrivateInformation( userId, "No options supplied for quiet scan, cancelling due to possible error" );
				return new HashSet<IMyEntity>( );
			}

			if ( words.Length - 3 > options.Count )
			{
				Communication.SendPrivateInformation( userId, "Possible problem with your parameters (options provided is larger than options found).  Not returning any results in case of error" );

				return new HashSet<IMyEntity>( );
			}

			if ( !scanOptions.Quiet )
				Communication.SendPrivateInformation( userId, string.Format( "Scanning for ships with options: {0}", GetOptionsText( options ) ) );

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
			Wrapper.GameAction( ( ) =>
			{
				MyAPIGateway.Entities.GetEntities( entities, x => x is IMyCubeGrid );
			} );

			HashSet<IMyEntity> entitiesToConfirm = new HashSet<IMyEntity>( );
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

				if ( PluginSettings.Instance.LoginEntityWhitelist.Contains( entity.EntityId.ToString( ) ) || PluginSettings.Instance.LoginEntityWhitelist.Contains( entity.DisplayName ) )
					continue;


				if ( scanOptions.HasDisplayName && scanOptions.DisplayName != string.Empty )
				{
					if ( !scanOptions.HasDisplayNameExact && entity.DisplayName.Contains( scanOptions.DisplayName ) )
						entitiesToConfirm.Add( entity );

					if ( scanOptions.HasDisplayNameExact && entity.DisplayName.Equals( scanOptions.DisplayName ) )
						entitiesToConfirm.Add( entity );
				}
				else if ( scanOptions.HasCustomName && HasCustomName( gridBuilder, scanOptions.CustomName, scanOptions.HasCustomNameExact ) )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( scanOptions.HasCustomName )
				{
					continue;
				}
				else if ( scanOptions.IsOwnedBy && scanOptions.OwnedByPlayerId > 0 && GetAllOwners( gridBuilder ).Contains( scanOptions.OwnedByPlayerId ) )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( !scanOptions.IsOwnedBy && scanOptions.HasOwner == ScanBehavior.Ignore )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( !scanOptions.IsOwnedBy && scanOptions.HasOwner == ScanBehavior.Yes && HasOwner( gridBuilder ) )
				{
					entitiesToConfirm.Add( entity );
				}
				else if ( !scanOptions.IsOwnedBy && scanOptions.HasOwner == ScanBehavior.No && !HasOwner( gridBuilder ) )
				{
					entitiesToConfirm.Add( entity );
				}
			}

			Dictionary<string, int> subTypeDict = new Dictionary<string, int>( );
			Dictionary<string, int> typeDict = new Dictionary<string, int>( );
			List<string> checkList = new List<string>( );
			//int blocks = 0;
			foreach ( IMyEntity entity in entitiesToConfirm )
			{
				subTypeDict.Clear( );
				typeDict.Clear( );
				checkList.Clear( );
				MyObjectBuilder_CubeGrid grid = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( );

				if ( scanOptions.OwnerOnline == ScanBehavior.No )
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
				else if ( scanOptions.OwnerOnline == ScanBehavior.Yes )
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
					if ( scanOptions.HasFunctional != ScanBehavior.Ignore )
					{
						if ( block is MyObjectBuilder_FunctionalBlock )
						{
							//							if (debug && !found)
							//								Communication.SendPrivateInformation(userId, string.Format("Found grid '{0}' ({1}) which has a functional block.  BlockCount={2}", entity.DisplayName, entity.EntityId, ((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder()).CubeBlocks.Count));

							if ( !checkList.Contains( "functional" ) )
								checkList.Add( "functional" );
						}
					}

					if ( scanOptions.HasTerminal != ScanBehavior.Ignore )
					{
						if ( block is MyObjectBuilder_TerminalBlock )
						{
							//if (debug && !found)
							//	Communication.SendPrivateInformation(userId, string.Format("Found grid '{0}' ({1}) which has a terminal block.  BlockCount={2}", entity.DisplayName, entity.EntityId, ((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder()).CubeBlocks.Count));

							if ( !checkList.Contains( "terminal" ) )
								checkList.Add( "terminal" );
						}
					}

					if ( scanOptions.HasPower != ScanBehavior.Ignore )
					{
						if ( DoesBlockSupplyPower( block ) )
						{
							//if (debug && !found)
							//	Communication.SendPrivateInformation(userId, string.Format("Found grid '{0}' ({1}) which has power.  BlockCount={2}", entity.DisplayName, entity.EntityId, ((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder()).CubeBlocks.Count));

							if ( !checkList.Contains( "power" ) )
								checkList.Add( "power" );
						}
					}

					if ( scanOptions.HasBlockSubType || scanOptions.HasBlockSubTypeLimits )
					{
						string subTypeName = block.GetId( ).SubtypeName;
						if ( subTypeDict.ContainsKey( subTypeName ) )
							subTypeDict[ subTypeName ] = subTypeDict[ subTypeName ] + 1;
						else
							subTypeDict.Add( subTypeName, 1 );
					}

					if ( scanOptions.IncludesBlockType || scanOptions.ExcludesBlockType )
					{
						string typeName = block.GetId( ).TypeId.ToString( );
						if ( typeDict.ContainsKey( typeName ) )
							typeDict[ typeName ] = typeDict[ typeName ] + 1;
						else
							typeDict.Add( typeName, 1 );
					}
				}

				if ( scanOptions.HasFunctional != ScanBehavior.Ignore )
				{
					if ( !checkList.Contains( "functional" ) && scanOptions.HasFunctional == ScanBehavior.Yes )
						found = false;

					if ( checkList.Contains( "functional" ) && scanOptions.HasFunctional == ScanBehavior.No )
						found = false;
				}

				if ( scanOptions.HasTerminal != ScanBehavior.Ignore )
				{
					if ( !checkList.Contains( "terminal" ) && scanOptions.HasTerminal == ScanBehavior.Yes )
						found = false;

					if ( checkList.Contains( "terminal" ) && scanOptions.HasTerminal == ScanBehavior.No )
						found = false;
				}

				if ( scanOptions.HasPower != ScanBehavior.Ignore )
				{
					if ( !checkList.Contains( "power" ) && scanOptions.HasPower == ScanBehavior.Yes )
						found = false;

					if ( checkList.Contains( "power" ) && scanOptions.HasPower == ScanBehavior.No )
						found = false;
				}

				if ( scanOptions.HasBlockSubType )
				{
					found = ApplyBlockSubTypeFilter( userId, subTypeDict, scanOptions, entity, found );
				}

				if ( scanOptions.IncludesBlockType )
				{
					found = ApplyBlockInclusionFilter( userId, typeDict, scanOptions, entity, found );
				}

				if ( scanOptions.HasBlockSubTypeLimits && found )
				{
					found = ApplyBlockSubTypeExclusionFilter( userId, subTypeDict, scanOptions, found, entity );
				}

				if ( scanOptions.ExcludesBlockType && found )
					found = ApplyBlockExclusionFilter( userId, typeDict, scanOptions, entity );

				if ( scanOptions.RequireBlockCount && found && grid.CubeBlocks.Count < scanOptions.BlockCount )
				{
					found = false;
				}

				if ( scanOptions.RequireBlockCountLess && found && grid.CubeBlocks.Count >= scanOptions.BlockCountLess )
				{
					found = false;
				}

				if ( scanOptions.IsBlockSize && found && scanOptions.BlockSize == 0 && grid.GridSizeEnum != MyCubeSize.Small )
				{
					found = false;
				}

				if ( scanOptions.IsBlockSize && found && scanOptions.BlockSize == 1 && grid.GridSizeEnum != MyCubeSize.Large )
				{
					found = false;
				}

				if ( scanOptions.IsBlockSize && found && scanOptions.BlockSize == 2 && ( grid.GridSizeEnum != MyCubeSize.Large || grid.GridSizeEnum == MyCubeSize.Large && !grid.IsStatic ) )
				{
					found = false;
				}

				if ( scanOptions.IsBlockSize && found && scanOptions.BlockSize == 3 && ( grid.GridSizeEnum != MyCubeSize.Large || grid.GridSizeEnum == MyCubeSize.Large && grid.IsStatic ) )
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

				if ( !scanOptions.Quiet )
					Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) (Owner: {2}) which has specified parameters.  BlockCount={3}", entity.DisplayName, entity.EntityId, ownerName, ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );
			}

			if ( !scanOptions.Quiet )
				Communication.SendPrivateInformation( userId, string.Format( "Found {0} grids", entitiesFound.Count ) );

			return entitiesFound;
		}

		private static void ParseBlocksizeArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			options.Add( "Requires Cubegrid Block Size", "true" );
			if ( wordIndex + 1 < words.Length )
			{
				switch ( words[ wordIndex + 1 ].ToLowerInvariant( ) )
				{
					case "small":
						scanOptions.IsBlockSize = true;
						scanOptions.BlockSize = 0;
						options.Add( "Cubegrid Block Size: ", "small" );
						break;
					case "large":
						scanOptions.IsBlockSize = true;
						scanOptions.BlockSize = 1;
						options.Add( "Cubegrid Block Size: ", "large" );
						break;
					case "station":
						scanOptions.IsBlockSize = true;
						scanOptions.BlockSize = 2;
						options.Add( "Cubegrid Block Size: ", "station" );
						break;
					case "largeship":
						scanOptions.IsBlockSize = true;
						scanOptions.BlockSize = 3;
						options.Add( "Cubegrid Block Size: ", "largeship" );
						break;
				}
				wordIndex++;
			}
		}

		private static void ParseBlockCountLessThanArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			int count;
			if ( wordIndex + 1 < words.Length && int.TryParse( words[ wordIndex + 1 ], out count ) )
			{
				scanOptions.RequireBlockCount = true;
				options.Add( "Requires Block Count Less Than", "true" );
				options.Add( "Block Count:", count.ToString( ) );
				scanOptions.BlockCount = count;
				wordIndex++;
			}
		}

		private static void ParseBlockCountArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			int count;
			if ( wordIndex + 1 < words.Length && int.TryParse( words[ wordIndex + 1 ], out count ) )
			{
				scanOptions.RequireBlockCount = true;
				options.Add( "Requires Block Count", "true" );
				options.Add( "Block Count:", count.ToString( ) );
				scanOptions.BlockCount = count;
				wordIndex++;
			}
		}

		private static void ParseOwnedbyArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			if ( wordIndex + 1 < words.Length )
			{
				scanOptions.IsOwnedBy = true;
				if ( PlayerMap.Instance.GetPlayerItemsFromPlayerName( words[ wordIndex + 1 ] ).Count > 0 )
				{
					scanOptions.OwnedByPlayerId = PlayerMap.Instance.GetPlayerItemsFromPlayerName( words[ wordIndex + 1 ] ).First( ).PlayerId;
				}
				options.Add( "Owned By", words[ wordIndex + 1 ] );
				wordIndex++;
			}
		}

		private static void ParseExcludesBlockTypeArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			if ( wordIndex + 1 >= words.Length )
			{
				return;
			}
			scanOptions.ExcludesBlockType = true;
			options.Add( "Excludes Block Type", "true" );
			int count;
			if ( wordIndex + 2 < words.Length && int.TryParse( words[ wordIndex + 2 ], out count ) )
			{
				scanOptions.BlockTypesExcluded.Add( words[ wordIndex + 1 ], count );
				options.Add( "Excludes Block Type Count: " + words[ wordIndex + 1 ], count.ToString( ) );
				wordIndex += 2;
			}
			else if ( wordIndex + 1 < words.Length )
			{
				scanOptions.BlockTypesExcluded.Add( words[ wordIndex + 1 ], 1 );
				options.Add( "Excludes Block Type Count: " + words[ wordIndex + 1 ], "1" );
				wordIndex++;
			}
		}

		private static void ParseIncludesBlockTypeArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			if ( wordIndex + 1 >= words.Length )
			{
				return;
			}
			scanOptions.IncludesBlockType = true;
			options.Add( "Includes Block Type", "true" );
			int count;
			if ( wordIndex + 2 < words.Length && int.TryParse( words[ wordIndex + 2 ], out count ) )
			{
				scanOptions.BlockTypes.Add( words[ wordIndex + 1 ], count );
				options.Add( "Includes Block Type Count: " + words[ wordIndex + 1 ], count.ToString( ) );
				wordIndex += 2;
			}
			else if ( wordIndex + 1 < words.Length )
			{
				scanOptions.BlockTypes.Add( words[ wordIndex + 1 ], 1 );
				options.Add( "Includes Block Type Count: " + words[ wordIndex + 1 ], "1" );
				wordIndex++;
			}
		}

		private static void ParseExcludesBlockSubtypeArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			if ( wordIndex + 1 >= words.Length )
			{
				return;
			}
			scanOptions.HasBlockSubTypeLimits = true;
			options.Add( "Exclude Has Sub Block Type", "true" );
			int count;
			if ( wordIndex + 2 < words.Length && int.TryParse( words[ wordIndex + 2 ], out count ) )
			{
				scanOptions.BlockSubTypes.Add( words[ wordIndex + 1 ], count );
				options.Add( "Sub Block Type Limit: " + words[ wordIndex + 1 ], count.ToString( ) );
				wordIndex += 2;
			}
			else if ( wordIndex + 1 < words.Length )
			{
				scanOptions.BlockSubTypes.Add( words[ wordIndex + 1 ], 1 );
				options.Add( "Sub Block Type Limit: " + words[ wordIndex + 1 ], "1" );
				wordIndex++;
			}
		}

		private static void ParseIncludesBlockSubtypeArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			if ( wordIndex + 1 >= words.Length )
			{
				return;
			}
			scanOptions.HasBlockSubType = true;
			options.Add( "Has Sub Block Type", "true" );
			int count;
			if ( wordIndex + 2 < words.Length && int.TryParse( words[ wordIndex + 2 ], out count ) )
			{
				scanOptions.BlockSubTypes.Add( words[ wordIndex + 1 ], count );
				options.Add( "Sub Block Type: " + words[ wordIndex + 1 ], count.ToString( ) );
				wordIndex += 2;
			}
			else if ( wordIndex + 1 < words.Length )
			{
				scanOptions.BlockSubTypes.Add( words[ wordIndex + 1 ], 1 );
				options.Add( "Sub Block Type: " + words[ wordIndex + 1 ], "1" );
				wordIndex++;
			}
		}

		private static void ParseDisplayNameArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			if ( wordIndex + 1 >= words.Length )
				return;
			scanOptions.HasDisplayName = true;
			scanOptions.DisplayName = words[ wordIndex + 1 ];
			options.Add( "Matches Display Name Text", string.Format( "true:{0}", scanOptions.DisplayName ) );
			if ( wordIndex + 2 < words.Length && words[ wordIndex + 2 ].ToLowerInvariant( ) == "exact" )
			{
				scanOptions.HasDisplayNameExact = true;
				options.Add( "Matches Display Exactly", "true" );
				wordIndex++;
			}
			wordIndex++;
		}

		private static void ParseCustomNameArgument( ref int wordIndex, string[ ] words, Dictionary<string, string> options, ScanOptions scanOptions )
		{
			if ( wordIndex + 1 >= words.Length )
				return;
			scanOptions.HasCustomName = true;
			scanOptions.CustomName = words[ wordIndex + 1 ];
			options.Add( "Matches Custom Name Text", "true:" + scanOptions.CustomName );
			if ( wordIndex + 2 < words.Length && words[ wordIndex + 2 ].ToLowerInvariant( ) == "exact" )
			{
				scanOptions.HasCustomNameExact = true;
				options.Add( "Matches Custom Name Exactly", "true" );
				wordIndex++;
			}
			wordIndex++;
		}

		private static bool ApplyBlockSubTypeExclusionFilter( ulong userId, Dictionary<string, int> subTypeDict, ScanOptions scanOptions, bool found, IMyEntity entity )
		{
			foreach ( KeyValuePair<string, int> pairBlockTypesInGrid in subTypeDict )
			{
				foreach ( KeyValuePair<string, int> pairBlockTypesFilter in scanOptions.BlockSubTypes )
				{
					if ( pairBlockTypesInGrid.Key.ToLower( ).Contains( pairBlockTypesFilter.Key.ToLower( ) ) )
					{
						if ( pairBlockTypesInGrid.Value >= pairBlockTypesFilter.Value )
						{
							if ( found )
							{
								found = false;
							}

							if ( scanOptions.Debug && !scanOptions.Quiet )
							{
								Communication.SendPrivateInformation( userId,
																	  string.Format( "Exclusion: Found grid '{0}' ({1}) which excludes block type of {3} at {4}.  BlockCount={2}",
																					 entity.DisplayName,
																					 entity.EntityId,
																					 ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count,
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

		private static bool ApplyBlockSubTypeFilter( ulong userId, Dictionary<string, int> subTypeDict, ScanOptions scanOptions, IMyEntity entity, bool found )
		{
			bool hasType = false;
			foreach ( KeyValuePair<string, int> pairBlockTypesInGrid in subTypeDict )
			{
				foreach ( KeyValuePair<string, int> pairBlockTypesFilter in scanOptions.BlockSubTypes )
				{
					if ( pairBlockTypesInGrid.Key.ToLower( ).Contains( pairBlockTypesFilter.Key.ToLower( ) ) )
					{
						if ( pairBlockTypesInGrid.Value >= pairBlockTypesFilter.Value )
						{
							if ( scanOptions.Debug && !scanOptions.Quiet )
							{
								Communication.SendPrivateInformation( userId,
																	  string.Format( "Found grid '{0}' ({1}) which contains at least {4} of block type {3} ({5}).  BlockCount={2}",
																					 entity.DisplayName,
																					 entity.EntityId,
																					 ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count,
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
				if ( scanOptions.Debug && !scanOptions.Quiet )
				{
					Communication.SendPrivateInformation( userId,
														  string.Format( "Found grid '{0}' ({1}) which does not contain block type.  BlockCount={2}",
																		 entity.DisplayName,
																		 entity.EntityId,
																		 ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );
				}

				found = false;
			}
			return found;
		}

		private static bool ApplyBlockInclusionFilter( ulong userId, Dictionary<string, int> typeDict, ScanOptions scanOptions, IMyEntity entity, bool found )
		{
			bool hasType = false;
			foreach ( KeyValuePair<string, int> pairBlockTypesInGrid in typeDict )
			{
				foreach ( KeyValuePair<string, int> pairBlockTypesFilter in scanOptions.BlockTypes )
				{
					if ( pairBlockTypesInGrid.Key.ToLower( ).Contains( pairBlockTypesFilter.Key.ToLower( ) ) )
					{
						if ( pairBlockTypesInGrid.Value >= pairBlockTypesFilter.Value )
						{
							if ( scanOptions.Debug && !scanOptions.Quiet )
							{
								Communication.SendPrivateInformation( userId,
																	  string.Format( "Found grid '{0}' ({1}) which contains at least {4} of block type {3} ({5}).  BlockCount={2}",
																					 entity.DisplayName,
																					 entity.EntityId,
																					 ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count,
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
				if ( scanOptions.Debug && !scanOptions.Quiet )
				{
					Communication.SendPrivateInformation( userId,
														  string.Format( "Found grid '{0}' ({1}) which does not contain block type.  BlockCount={2}",
																		 entity.DisplayName,
																		 entity.EntityId,
																		 ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );
				}

				found = false;
			}
			return found;
		}

		private static bool ApplyBlockExclusionFilter( ulong userId, Dictionary<string, int> typeDict, ScanOptions scanOptions, IMyEntity entity )
		{
			bool found = true;
			foreach ( KeyValuePair<string, int> blockTypesInGrid in typeDict )
			{
				foreach ( KeyValuePair<string, int> pairBlockTypesFilter in scanOptions.BlockTypesExcluded )
				{
					if ( blockTypesInGrid.Key.ToLower( ).Contains( pairBlockTypesFilter.Key.ToLower( ) ) )
					{
						if ( blockTypesInGrid.Value >= pairBlockTypesFilter.Value )
						{
							if ( found )
							{
								found = false;
							}

							if ( scanOptions.Debug && !scanOptions.Quiet )
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
					string ownerName = string.Empty;
					if ( GetBigOwners( gridBuilder ).Count > 0 )
					{
						ownerId = GetBigOwners( gridBuilder ).First( );
						ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId( ownerId ).Name;
					}

					Logging.WriteLineAndConsole( "Cleanup", string.Format( "Cleanup Removed Grid - Id: {0} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName, ownerId, ownerName ) );
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

		public static bool DoesGridHaveFourBeacons( MyObjectBuilder_CubeGrid grid )
		{
			int count = 0;
			foreach ( MyObjectBuilder_CubeBlock block in grid.CubeBlocks )
			{
				if ( block is MyObjectBuilder_Beacon )
					count++;

				if ( count >= 4 )
					return true;
			}

			return false;
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
		public static void GetConnectedGrids( HashSet<IMyEntity> grids, Func<IMyEntity, bool> collect = null )
		{
			List<IMySlimBlock> currentBlocks = new List<IMySlimBlock>( );
			HashSet<IMyEntity> gridsProcessed = new HashSet<IMyEntity>( );
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );

			MyAPIGateway.Entities.GetEntities( entities, collect );
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
						if ( MyAPIGateway.Entities.TryGetEntityById( stator.RotorEntityId, out connectedEntity ) )
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
