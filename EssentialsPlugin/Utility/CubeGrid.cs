namespace EssentialsPlugin.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
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
    using Sandbox.Game.Gui;
    using Sandbox.Game.World;
    using Sandbox.ModAPI.Ingame;
    using SEModAPI.API;
    using SEModAPIExtensions.API;
    using SpaceEngineers.Game.ModAPI.Ingame;
    using VRage.Animations;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.Library.Utils;
    using IMyReactor = Sandbox.ModAPI.IMyReactor;

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
            List<MyEntity> entitiesToMove = new List<MyEntity>( );
			BoundingSphereD sphere = new BoundingSphereD( startPosition, radius );
            Wrapper.GameAction( ( ) => entitiesToMove = MyEntities.GetEntitiesInSphere(ref sphere) );
            List<MyEntity> entitiesToRemove = new List<MyEntity>( );
			int count = 0;

			Wrapper.GameAction( ( ) =>
			{
				foreach ( MyEntity entity in entitiesToMove )
				{
					if ( !( entity is MyCubeGrid ) )
						continue;

				    MyCubeGrid grid = (MyCubeGrid)entity;
					MyObjectBuilder_CubeGrid cubeGrid = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( );
					if ( removeType == RemoveGridTypes.Ships && cubeGrid.IsStatic )
						continue;

					if ( removeType == RemoveGridTypes.Stations && !cubeGrid.IsStatic )
						continue;

					entitiesToRemove.Add( entity );
					Communication.SendPrivateInformation( userId, string.Format( "Deleting entity '{0}' at {1}", entity.DisplayName, General.Vector3DToString( entity.PositionComp.GetPosition( ) ) ) );
					count++;
				}
			} );

			for ( int r = entitiesToRemove.Count - 1; r >= 0; r-- )
			{
				MyEntity entity = entitiesToRemove[ r ];
			    Wrapper.GameAction( ( ) => entity.Close( ) );
			    //MyAPIGateway.Entities.RemoveEntity(entity);
			    //CubeGridEntity gridEntity = new CubeGridEntity( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ), entity );
			    //gridEntity.Dispose( );
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

        public static MyEntity Find(string displayName)
		{
			HashSet<MyEntity> entities = new HashSet<MyEntity>( );

			Wrapper.GameAction( ( ) => entities = MyEntities.GetEntities( ) );

            //return entities.FirstOrDefault( entity => entity.DisplayName.ToLower( ).Contains( displayName.ToLower( ) ) );
            return entities.FirstOrDefault( entity => entity is MyCubeGrid && entity.DisplayName.ToLower( ).Contains( displayName.ToLower( ) ) );
		}
        
	    public static MyCubeGrid GetLargestInGroup( List<MyCubeGrid> group )
	    {
	        MyCubeGrid result = null;
	        foreach ( MyCubeGrid grid in group )
	        {
	            if ( result == null || grid.BlocksCount > result.BlocksCount )
	                result = grid;
	        }
	        return result;
	    }

	    public static void CloseGroups( HashSet<List<MyCubeGrid>> groups )
	    {
	        foreach ( var group in groups )
	        {
	            foreach ( MyCubeGrid grid in group )
	            {
	                Wrapper.GameAction( ()=> grid.Close(  ) );
	            }
	        }
	    }

	    public static HashSet<MyEntity> ScanCleanup( ulong userId, string[] words )
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
	        GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical;

	        options.Add( "Requires Functional", "true" );
	        options.Add( "Requires Terminal", "true" );
	        options.Add( "Requires Valid Power", "true" );
	        options.Add( "Matches Display Name Text", "false" );
	        options.Add( "Ignore Ownership", "false" );
	        options.Add( "Requires Ownership", "false" );
	        options.Add( "Debug", "false" );
	        options.Add( "Has Sub Block Type", "false" );
	        options.Add( "Has Sub Block Type Limits", "false" );
	        options.Add( "Physical connection", "false" );

	        string displayName = "";
	        Dictionary<string, int> blockSubTypes = new Dictionary<string, int>( );

	        if ( words.Any( ) )
	        {
	            if ( words.FirstOrDefault( x => x.ToLower( ) == "debug" ) != null )
	            {
	                options["Debug"] = "true";
	                debug = true;
	            }

	            if ( words.SingleOrDefault( x => x.ToLower( ) == "ignoreownership" ) != null )
	            {
	                options["Ignore Ownership"] = "true";
	                ignoreOwnership = true;
	            }

	            if ( words.SingleOrDefault( x => x.ToLower( ) == "isowned" ) != null )
	            {
	                options["Requires Ownership"] = "true";
	                requiresOwner = true;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "ignorefunctional" ) != null )
	            {
	                options["Requires Functional"] = "false";
	                requiresFunctional = false;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "ignoreterminal" ) != null )
	            {
	                options["Requires Terminal"] = "false";
	                requiresTerminal = false;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "ignorepower" ) != null )
	            {
	                options["Requires Valid Power"] = "ignore";
	                requiresPower = false;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ) != null )
	            {
	                hasDisplayName = true;
	                displayName = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ).Split( ':' )[1];
	                options["Matches Display Name Text"] = "true:" + displayName;

	                if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ).Split( ':' ).Length > 2 && words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ).Split( ':' )[2] == "exact" )
	                    hasDisplayNameExact = true;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasblocksubtype:" ) ) != null )
	            {
	                string[] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasblocksubtype:" ) ).Split( ':' );
	                hasBlockSubType = true;
	                options["Has Sub Block Type"] = "true";

	                if ( parts.Length < 3 )
	                {
	                    blockSubTypes.Add( parts[1], 1 );
	                    options.Add( "Sub Block Type: " + parts[1], "1" );
	                }
	                else
	                {
	                    int count = 1;
	                    int.TryParse( parts[2], out count );
	                    blockSubTypes.Add( parts[1], count );
	                    options.Add( "Sub Block Type: " + parts[1], count.ToString( ) );
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "limitblocksubtype:" ) ) != null )
	            {
	                string[] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "limitblocksubtype:" ) ).Split( ':' );
	                hasBlockSubTypeLimits = true;
	                options["Has Sub Block Type Limits"] = "true";

	                if ( parts.Length < 3 )
	                {
	                    blockSubTypes.Add( parts[1], 1 );
	                    options.Add( "Sub Block Type Limit: " + parts[1], "1" );
	                }
	                else
	                {
	                    int count = 1;
	                    int.TryParse( parts[2], out count );
	                    blockSubTypes.Add( parts[1], count );
	                    options.Add( "Sub Block Type Limit: " + parts[1], count.ToString( ) );
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "physical" ) ) != null )
	            {
	                linkType = GridLinkTypeEnum.Physical;
	            }
	        }

	        Communication.SendPrivateInformation( userId, string.Format( "Scanning for ships with options: {0}", GetOptionsText( options ) ) );

	        HashSet<MyEntity> entities = new HashSet<MyEntity>( );
	        Wrapper.GameAction( ( ) => entities = MyEntities.GetEntities( ) );

	        HashSet<MyEntity> entitiesToConfirm = new HashSet<MyEntity>( );
	        HashSet<MyEntity> entitiesFound = new HashSet<MyEntity>( );
	        foreach ( MyEntity entity in entities.Where( x => x is MyCubeGrid ) )
	        {
	            MyCubeGrid grid = entity as MyCubeGrid;
	            if ( grid == null )
	                continue;

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
	            else if ( requiresOwner && grid.BigOwners.Count > 0 )
	            {
	                entitiesToConfirm.Add( entity );
	            }
	            else if ( !requiresOwner && grid.BigOwners.Count == 0 )
	            {
	                entitiesToConfirm.Add( entity );
	            }
	        }

	        Dictionary<string, int> subTypeDict = new Dictionary<string, int>( );

	        HashSet<GridGroup> groupsFound = GridGroup.GetGroups( entitiesToConfirm, linkType );

	        foreach ( var group in groupsFound )
	        {
	            foreach ( MyCubeGrid grid in group.Grids )
	            {
	                subTypeDict.Clear( );
	                bool found = false;
	                foreach ( MySlimBlock slimBlock in grid.CubeBlocks )
	                {
	                    if ( slimBlock?.FatBlock == null )
	                        continue;

	                    MyCubeBlock block = slimBlock.FatBlock;

	                    if ( requiresFunctional )
	                    {
	                        if ( block is MyFunctionalBlock )
	                        {
	                            if ( debug && !found )
	                                Communication.SendPrivateInformation( userId, $"Found grid '{grid.DisplayName}' ({grid.EntityId}) which has a functional block.  BlockCount={( (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( ) ).CubeBlocks.Count}" );

	                            found = true;
	                        }
	                    }

	                    if ( requiresTerminal )
	                    {
	                        if ( block is MyTerminalBlock )
	                        {
	                            if ( debug && !found )
	                                Communication.SendPrivateInformation( userId, $"Found grid '{grid.DisplayName}' ({grid.EntityId}) which has a terminal block.  BlockCount={( (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( ) ).CubeBlocks.Count}" );

	                            found = true;
	                        }
	                    }


	                    if ( requiresPower )
	                    {
	                        if ( DoesBlockSupplyPower( block ) )
	                        {
	                            if ( debug && !found )
	                                Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which has power.  BlockCount={2}", grid.DisplayName, grid.EntityId, ( (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( ) ).CubeBlocks.Count ) );

	                            found = true;
	                        }
	                    }

	                    if ( hasBlockSubType || hasBlockSubTypeLimits )
	                    {
	                        string subTypeName = ((IMyCubeBlock)block).BlockDefinition.SubtypeName;
	                        if ( subTypeDict.ContainsKey( subTypeName ) )
	                            subTypeDict[subTypeName] = subTypeDict[subTypeName] + 1;
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
	                                        Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which contains at least {4} of block type {3} ({5}).  BlockCount={2}", grid.DisplayName, grid.EntityId, ( (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( ) ).CubeBlocks.Count, p.Key, s.Value, p.Value ) );

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

	                                    Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which is over limit of block type {3} at {4}.  BlockCount={2}", grid.DisplayName, grid.EntityId, ( (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( ) ).CubeBlocks.Count, s.Key, p.Value ) );
	                                    break;
	                                }
	                            }
	                        }
	                    }
	                }

	                if ( !found )
	                    entitiesFound.Add( grid );
	            }
	        }

	foreach ( MyEntity entity in entitiesFound )
			{
				Communication.SendPrivateInformation( userId, string.Format( "Found grid '{0}' ({1}) which has unconnected and has parameters specified.  BlockCount={2}", entity.DisplayName, entity.EntityId, ( (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder( ) ).CubeBlocks.Count ) );
			}

			Communication.SendPrivateInformation( userId, string.Format( "Found {0} grids considered to be trash", entitiesFound.Count ) );
			return entitiesFound;
		}

	    public enum StateEnum
	    {
	        Ignore,
            RequiresNo,
            RequiresYes
	    }

	    public static HashSet<GridGroup> ScanGrids( ulong userId, string[] words )
	    {
	        Dictionary<string, string> options = new Dictionary<string, string>( );

	        // 0 - ignore 1 - no 2 - yes
	        // functional
	        // terminal
	        // ownership
	        // power
	        var functional = StateEnum.Ignore;
	        var terminal = StateEnum.Ignore;
	        var power = StateEnum.Ignore;
	        var owner = StateEnum.Ignore;
	        var online = StateEnum.Ignore;

            // debug
            // hasdisplayname
            // blocksubtype
            // blocksubtypelimit
            bool hasDisplayName = false;
	        bool hasDisplayNameExact = false;
	        bool displayNameGroup = false;
	        bool includesBlockSubType = false;
	        bool excludesBlockSubType = false;
	        bool includesBlockType = false;
	        bool excludesBlockType = false;
	        bool debug = false;
	        bool isOwnedBy = false;
	        bool quiet = false;
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
	        bool testStatic = false;
	        MyCubeSize blockSize = MyCubeSize.Large;
	        GridLinkTypeEnum connectionType = GridLinkTypeEnum.Logical;

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

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "physical" ) != null )
	            {
	                options.Add( "Physical connection", "true" );
	                connectionType = GridLinkTypeEnum.Physical;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "logical" ) != null )
	            {
	                options.Add( "Logical connection", "true" );
	                connectionType = GridLinkTypeEnum.Logical;
	            }

	            if ( words.SingleOrDefault( x => x.ToLower( ) == "ownership" ) != null )
	            {
	                options.Add( "Ownership", "true" );
	                owner = StateEnum.RequiresYes;
	            }

	            if ( words.SingleOrDefault( x => x.ToLower( ) == "noownership" ) != null )
	            {
	                options.Add( "Ownership", "false" );
	                owner = StateEnum.RequiresNo;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "functional" ) != null )
	            {
	                options.Add( "Functional", "true" );
	                functional = StateEnum.RequiresYes;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "nofunctional" ) != null )
	            {
	                options.Add( "Functional", "false" );
	                functional = StateEnum.RequiresNo;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "terminal" ) != null )
	            {
	                options.Add( "Terminal", "true" );
	                terminal = StateEnum.RequiresYes;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "noterminal" ) != null )
	            {
	                options.Add( "Terminal", "false" );
	                terminal = StateEnum.RequiresNo;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "power" ) != null )
	            {
	                options.Add( "Has Power", "true" );
	                power = StateEnum.RequiresYes;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "nopower" ) != null )
	            {
	                options.Add( "Has Power", "false" );
	                power = StateEnum.RequiresNo;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "notonline" ) != null )
	            {
	                options.Add( "Online", "false" );
	                online = StateEnum.RequiresNo;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ) == "online" ) != null )
	            {
	                options.Add( "Online", "true" );
	                online = StateEnum.RequiresYes;
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ) != null )
	            {
	                string[] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hasdisplayname:" ) ).Split( ':' );
	                if ( parts.Length > 1 )
	                {
	                    hasDisplayName = true;
	                    displayName = parts[1];
	                    options.Add( "Matches Display Name Text", "true:" + displayName );
                        //Console.WriteLine("Here: {0}", parts[2]);
                        if (parts.Length > 2 && parts[2].ToLower() == "exact")
                        {
                            hasDisplayNameExact = true;
                            options.Add("Matches Display Name Exactly", "true");
                        }
                        if (parts.Length > 2 && parts[2].ToLower() == "group")
                        {
                            displayNameGroup = true;
                            options.Add("Grid Can Be Grouped", "true");
                        }
                    }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hascustomname:" ) ) != null )
	            {
	                string[] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "hascustomname:" ) ).Split( ':' );
	                if ( parts.Length > 1 )
	                {
	                    hasCustomName = true;
	                    customName = parts[1];
	                    options.Add( "Matches Custom Name Text", "true:" + customName );
	                    //Console.WriteLine("Here: {0}", parts[2]);
	                    if ( parts.Length > 2 && parts[2].ToLower( ) == "exact" )
	                    {
	                        hasCustomNameExact = true;
	                        options.Add( "Matches Custom Name Exactly", "true" );
	                    }
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "includesblocksubtype:" ) ) != null )
	            {
	                string[] parts =
	                    words.FirstOrDefault( x => x.ToLower( ).StartsWith( "includesblocksubtype:" ) ).Split( ':' );
	                includesBlockSubType = true;
	                options.Add( "Has Sub Block Type", "true" );

	                if ( parts.Length < 3 )
	                {
	                    blockSubTypes.Add( parts[1], 1 );
	                    options.Add( "Sub Block Type: " + parts[1], "1" );
	                }
	                else
	                {
	                    int count = 1;
	                    int.TryParse( parts[2], out count );
	                    blockSubTypes.Add( parts[1], count );
	                    options.Add( "Sub Block Type: " + parts[1], count.ToString( ) );
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "excludesblocksubtype:" ) ) != null )
	            {
	                string[] parts =
	                    words.FirstOrDefault( x => x.ToLower( ).StartsWith( "excludesblocksubtype:" ) ).Split( ':' );
	                excludesBlockSubType = true;
	                options.Add( "Exclude Has Sub Block Type", "true" );

	                if ( parts.Length < 3 )
	                {
	                    blockSubTypes.Add( parts[1], 1 );
	                    options.Add( "Sub Block Type Limit: " + parts[1], "1" );
	                }
	                else
	                {
	                    int count = 1;
	                    int.TryParse( parts[2], out count );
	                    blockSubTypes.Add( parts[1], count );
	                    options.Add( "Sub Block Type Limit: " + parts[1], count.ToString( ) );
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "ownedby:" ) ) != null )
	            {
	                string[] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "ownedby:" ) ).Split( ':' );
	                if ( parts.Length > 1 )
	                {
	                    isOwnedBy = true;
	                    string ownedBy = parts[1];
	                    var player = MySession.Static.Players.GetAllIdentities( ).FirstOrDefault( x => x.DisplayName.Contains( ownedBy, StringComparison.CurrentCultureIgnoreCase ) );
                        if (player!=null)
	                        ownedByPlayerId = player.IdentityId;

	                    options.Add( "Owned By", ownedBy );
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "includesblocktype:" ) ) != null )
	            {
	                string[] parts =
	                    words.FirstOrDefault( x => x.ToLower( ).StartsWith( "includesblocktype:" ) ).Split( ':' );
	                includesBlockType = true;
	                options.Add( "Includes Block Type", "true" );

	                if ( parts.Length < 3 )
	                {
	                    blockTypes.Add( parts[1], 1 );
	                    options.Add( "Includes Block Type Count: " + parts[1], "1" );
	                }
	                else
	                {
	                    int count = 1;
	                    int.TryParse( parts[2], out count );
	                    blockTypes.Add( parts[1], count );
	                    options.Add( "Includes Block Type Count: " + parts[1], count.ToString( ) );
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "excludesblocktype:" ) ) != null )
	            {
	                string[] parts =
	                    words.FirstOrDefault( x => x.ToLower( ).StartsWith( "excludesblocktype:" ) ).Split( ':' );
	                excludesBlockType = true;
	                options.Add( "Excludes Block Type", "true" );

	                if ( parts.Length < 3 )
	                {
	                    blockTypesExcluded.Add( parts[1], 1 );
	                    options.Add( "Excludes Block Type Count: " + parts[1], "1" );
	                }
	                else
	                {
	                    int count = 1;
	                    int.TryParse( parts[2], out count );
	                    blockTypesExcluded.Add( parts[1], count );
	                    options.Add( "Excludes Block Type Count: " + parts[1], count.ToString( ) );
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blockcount:" ) ) != null )
	            {
	                string[] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blockcount:" ) ).Split( ':' );
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
	                    int.TryParse( parts[1], out count );
	                    blockCount = count;
	                    options.Add( "Block Count:", blockCount.ToString( ) );
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blockcountlessthan:" ) ) != null )
	            {
	                string[] parts =
	                    words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blockcountlessthan:" ) ).Split( ':' );
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
	                    int.TryParse( parts[1], out count );
	                    blockCountLess = count;
	                    options.Add( "Block Count:", blockCount.ToString( ) );
	                }
	            }

	            if ( words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blocksize:" ) ) != null )
	            {
	                string[] parts = words.FirstOrDefault( x => x.ToLower( ).StartsWith( "blocksize:" ) ).Split( ':' );

	                if ( parts[1].ToLower( ) == "small" )
	                {
	                    options.Add( "Is Block Size", "small" );
	                    isBlockSize = true;
	                    blockSize = MyCubeSize.Small;
	                }
	                else if ( parts[1].ToLower( ) == "large" )
	                {
	                    options.Add( "Is Block Size", "large" );
	                    isBlockSize = true;
	                    blockSize = MyCubeSize.Large;
	                }
	                else if ( parts[1].ToLower( ) == "station" )
	                {
	                    options.Add( "Is Block Size", "station" );
	                    isBlockSize = true;
	                    blockSize = MyCubeSize.Large;
	                    testStatic = true;
	                }
	                else if ( parts[1].ToLower( ) == "largeship" )
	                {
	                    options.Add( "Is Block Size", "largeship" );
	                    isBlockSize = true;
	                    blockSize = MyCubeSize.Large;
	                }
	            }

	        }

	        if ( options.Count < 1 && quiet )
	        {
	            Communication.SendPrivateInformation( userId,
	                                                  "No options supplied for quiet scan, cancelling due to possible error" );
	            return new HashSet<GridGroup>( );
	        }

	        if ( words.Length > options.Count )
	        {
	            Communication.SendPrivateInformation( userId,
	                                                  "Possible problem with your parameters (options provided is larger than options found).  Not returning any results in case of error" );
	            return new HashSet<GridGroup>( );
	        }

	        if ( !quiet )
	            Communication.SendPrivateInformation( userId,
	                                                  $"Scanning for ships with options: {GetOptionsText( options )}" );
            
	        HashSet<GridGroup> groups = GridGroup.GetAllGroups( connectionType );
	        HashSet<GridGroup> groupsFound = new HashSet<GridGroup>( );
            List<Task> scanTasks = new List<Task>();
            
            /*
            * Invert scan logic here.
            * If we find a group that fails any of the checks, break.
            * Everything else implicitly goes into the found list.
            * 
            * On large servers this can run into the tens of seconds,
            * so parallelize it.
            */
	        foreach ( GridGroup group in groups )
	        {
	            if ( group?.Parent == null )
	                continue;

	            scanTasks.Add( Task.Run( ( ) =>
	                                     {
	                                         if ( power == StateEnum.RequiresYes && !DoesGroupHavePowerSupply( group ) )
	                                             return;
	                                         if ( power == StateEnum.RequiresNo && DoesGroupHavePowerSupply( group ) )
	                                             return;

	                                         if ( owner == StateEnum.RequiresYes && group.SmallOwners.Count == 0 || ( group.SmallOwners.Count == 1 && group.SmallOwners[0] == 0 ) )
	                                             return;
	                                         if ( owner == StateEnum.RequiresNo && group.SmallOwners.Count > 0 )
	                                             return;

	                                         if ( functional == StateEnum.RequiresYes && !IsGroupFunctional( group ) )
	                                             return;
	                                         if ( functional == StateEnum.RequiresNo && IsGroupFunctional( group ) )
	                                             return;

	                                         if ( terminal == StateEnum.RequiresYes && !DoesGroupHaveTerminal( group ) )
	                                             return;
	                                         if ( terminal == StateEnum.RequiresNo && DoesGroupHaveTerminal( group ) )
	                                             return;

	                                         if ( online == StateEnum.RequiresYes && !AreOwnersOnline( group ) )
	                                             return;
	                                         if ( online == StateEnum.RequiresNo && AreOwnersOnline( group ) )
	                                             return;

	                                         if ( hasDisplayName && !DoesGroupHaveDisplayName( group, displayName, hasDisplayNameExact, displayNameGroup ) )
	                                             return;

	                                         if ( hasCustomName && !DoesGroupHaveCustomName( group, customName, hasCustomNameExact ) )
	                                             return;

	                                         if ( isBlockSize && !IsGroupGridSize( group, blockSize, testStatic ) )
	                                             return;

	                                         if ( isOwnedBy && !( group.BigOwners.Count == 1 && group.BigOwners[0] == ownedByPlayerId ) )
	                                             return;

	                                         if ( requireBlockCount && !( group.BlocksCount > blockCount ) )
	                                             return;

	                                         if ( requireBlockCountLess && !( group.BlocksCount < blockCountLess ) )
	                                             return;

	                                         if ( excludesBlockType && blockTypesExcluded.Any( x => DoesGroupHaveBlockType( group, x.Key, x.Value ) ) )
	                                             return;

	                                         if ( includesBlockType && !blockTypes.Any( x => DoesGroupHaveBlockType( group, x.Key, x.Value ) ) )
	                                             return;
                                             
	                                         if ( includesBlockSubType && !blockSubTypes.Any( x => DoesGroupHaveBlockSubtype( group, x.Key, x.Value ) ) )
	                                             return;

	                                         if ( excludesBlockSubType && blockSubTypes.Any( x => DoesGroupHaveBlockSubtype( group, x.Key, x.Value ) ) )
	                                             return;

	                                         lock ( groupsFound )
	                                         {
	                                             groupsFound.Add( group );
	                                         }
	                                     } ) );
	        }
	        Task.WaitAll( scanTasks.ToArray(  ) );
            
	        int gridCount = 0;
	        int groupCount = groupsFound.Count;
	        foreach (GridGroup group in groupsFound)
	        {
	            if (!quiet)
	                Communication.SendPrivateInformation( userId, $"Found group with parent {group.Parent.DisplayName}, owner: {group.Parent.GetOwner(  ) ?? "none"}" );

	            gridCount += group.Grids.Count;
	        }

	        //if (!quiet)
	            Communication.SendPrivateInformation( userId, $"Found {gridCount} grids in {groupCount} groups" );

	        return groupsFound;
	    }

		public static bool IsFullOwner( MyCubeGrid grid, long ownerId, IMyPlayer factionPlayer = null )
		{
			bool found = false;
			foreach ( MySlimBlock slimBlock in grid.CubeBlocks )
			{
				MyTerminalBlock functional = slimBlock?.FatBlock as MyFunctionalBlock;
			    if ( functional == null )
			        continue;

				if ( factionPlayer == null )
				{
					if ( functional.OwnerId != 0)

                        if ( functional.OwnerId != ownerId)
                            return false;

						found = true;
				}
				else
				{
					MyRelationsBetweenPlayerAndBlock relation = factionPlayer.GetRelationTo( functional.OwnerId );
					if ( functional.OwnerId != 0 && ( relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare ) )
					{
						found = true;
					}
					else if ( functional.OwnerId != 0 && relation != MyRelationsBetweenPlayerAndBlock.FactionShare && relation != MyRelationsBetweenPlayerAndBlock.FactionShare )
					{
						return false;
					}
				}
			}

			return found;
		}

		public static bool HasCustomName( MyCubeGrid grid, string name, bool exact )
		{
			foreach (MySlimBlock block in grid.CubeBlocks )
			{
			    MyTerminalBlock termBlock = block?.FatBlock as MyTerminalBlock;
			    if (termBlock == null) continue;
			    if ( exact )
			    {
			        if ( termBlock.CustomName != null && termBlock.CustomName.ToString(  ) == name )
			        {
			            return true;
			        }
			    }
			    else
			    {
			        if ( termBlock.CustomName != null && termBlock.CustomName.ToString(  ).Contains( name ) )
			        {
			            return true;
			        }
			    }
			}

			return false;
		}

		private static string GetOptionsText( Dictionary<string, string> options )
		{
			return string.Join( ", ", options.Select( o => string.Format( "{0}={1}", o.Key, o.Value ) ) );
		}

		public static bool DoesBlockSupplyPower( MyCubeBlock block )
		{
		    if (block == null)
		        return false;

		    if ( !block.IsFunctional )
		        return false;

		    if (block is IMyBatteryBlock)
		    {
		        if (((IMyBatteryBlock)block).CurrentStoredPower > 0f)
		            return true;
		    }

		    if (block is IMyReactor)
		    {
		        //reactors in creative mode provide power without uranium
		        bool found = false;

                //check creative mode before entering the game thread
		        if ( Server.Instance.Config.GameMode == MyGameModeEnum.Creative )
		            return true;

		        Wrapper.GameAction( ( ) =>
		                            {
		                                if ( ((IMyReactor)block).GetInventory( 0 ).GetItems( ).Count > 0 )
		                                    found = true;
		                            } );
		        if ( found )
		            return true;
		    }

		    if ( block is IMySolarPanel )
            {
                return true;
            }

            return false;
		}

		public static bool DoesGridHavePowerSupply( MyCubeGrid grid )
		{
		    return grid.GetFatBlocks(  ).Any( DoesBlockSupplyPower );
		}

        public static bool DoesGroupHavePowerSupply(GridGroup group )
        {
            return group.GetFatBlocks().Any( DoesBlockSupplyPower );
        }

	    public static bool IsGroupFunctional( GridGroup group )
	    {
	        foreach ( MyCubeBlock fatBlock in group.GetFatBlocks() )
	        {
	            MyFunctionalBlock block = fatBlock as MyFunctionalBlock;
	            if ( block == null )
	                continue;
	            if ( block.IsFunctional )
	                return true;
	        }
	        return false;
	    }

	    public static bool DoesGroupHaveTerminal( GridGroup group )
	    {
	        return group.CubeBlocks.Any( x => (x?.FatBlock as MyTerminalBlock) != null );
	    }

	    public static bool AreOwnersOnline( GridGroup group )
	    {
	        foreach ( MyPlayer player in MySession.Static.Players.GetOnlinePlayers(  ) )
	        {
	            if ( group.SmallOwners.Contains(player.Identity.IdentityId) )
	                return true;
	        }
	        return false;
	    }

	    public static bool DoesGroupHaveCustomName( GridGroup group, string customName, bool exact = false )
	    {
	        if ( !exact )
	            return group.CubeBlocks.Any( x => x?.FatBlock?.DisplayNameText != null && x.FatBlock.DisplayNameText.Contains( customName, StringComparison.CurrentCultureIgnoreCase));
	        else
	            return group.CubeBlocks.Any( x => x?.FatBlock?.DisplayNameText != null && x.FatBlock.DisplayNameText.Equals( customName,StringComparison.CurrentCultureIgnoreCase ));
	    }

	    public static bool DoesGroupHaveDisplayName( GridGroup group, string displayName, bool exact = false, bool grouped = false )
	    {
	        if ( !grouped && group.Grids.Count > 1 )
	            return false;

	        if ( !exact )
	            return group.Grids.Any( x => x?.DisplayName != null && x.DisplayName.Contains( displayName, StringComparison.CurrentCultureIgnoreCase ) );
	        else
	            return group.Grids.Any( x => x?.DisplayName != null && x.DisplayName.Equals( displayName,StringComparison.CurrentCultureIgnoreCase ));
                
	    }

	    public static bool IsGroupGridSize( GridGroup group, MyCubeSize size, bool isStatic = false )
	    {
	        if( isStatic )
	            return group.Grids.Any( x => x?.Physics !=null && x.Physics.IsStatic );
            else
	            return group.Grids.All( x => x.GridSizeEnum == size );
	    }

	    public static bool DoesGroupHaveBlockSubtype( GridGroup group, string subtype, int count )
	    {
            int result = 0;

            foreach (MyCubeBlock block in group.GetFatBlocks())
            {
                if (block.BlockDefinition.Id.SubtypeName.ToLower().Contains(subtype, StringComparison.CurrentCultureIgnoreCase))
                    result++;
                if (result >= count)
                    return true;
            }
	        return false;
	    }

        public static bool DoesGroupHaveBlockType(GridGroup group, string type, int count)
        {
            int result = 0;

            foreach (MyCubeBlock block in group.GetFatBlocks())
            {
                if (block.BlockDefinition.Id.TypeId.ToString(  ).ToLower().Contains( type, StringComparison.CurrentCultureIgnoreCase ))
                    result++;

                if (result >= count)
                    return true;
            }
            return false;
        }

        //TODO: KILL THESE
        //these are left over to retain concealment compatiblity until I can rewrite that mess
        public static void GetGridsUnconnected(HashSet<IMyEntity> connectedList, HashSet<IMyEntity> entitiesToConfirm)
        {
            foreach (IMyEntity entity in entitiesToConfirm)
            {
                if (!(entity is IMyCubeGrid))
                    continue;

                IMyCubeGrid grid = (IMyCubeGrid)entity;
                MyObjectBuilder_CubeGrid gridBuilder = SafeGetObjectBuilder(grid);
                if (gridBuilder == null)
                    continue;

                bool result = false;
                foreach (MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
                {
                    if (block.TypeId == typeof(MyObjectBuilder_ShipConnector))
                    {
                        MyObjectBuilder_ShipConnector connector = (MyObjectBuilder_ShipConnector)block;
                        if (connector.Connected)
                        {
                            IMyEntity connectedEntity;
                            MyAPIGateway.Entities.TryGetEntityById(connector.ConnectedEntityId, out connectedEntity);

                            if (connectedEntity != null)
                            {
                                result = true;
                                break;
                            }
                        }
                    }

                    if (block.TypeId == typeof(MyObjectBuilder_PistonBase))
                    {
                        result = true;
                        break;
                    }

                    if (block.TypeId == typeof(MyObjectBuilder_ExtendedPistonBase))
                    {
                        result = true;
                        break;
                    }

                    if (block.TypeId == typeof(MyObjectBuilder_PistonTop))
                    {
                        result = true;
                        break;
                    }

                    if (block.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator))
                    {
                        MyObjectBuilder_MotorAdvancedStator stator = (MyObjectBuilder_MotorAdvancedStator)block;
                        if (stator.RotorEntityId != 0)
                        {
                            IMyEntity connectedEntity = null;

                            if (stator.RotorEntityId != null)
                                MyAPIGateway.Entities.TryGetEntityById((long)stator.RotorEntityId, out connectedEntity);

                            if (connectedEntity != null)
                            {
                                result = true;
                                break;
                            }
                        }
                    }

                    if (block.TypeId == typeof(MyObjectBuilder_MotorAdvancedRotor))
                    {
                        result = true;
                        break;
                    }

                    if (block.TypeId == typeof(MyObjectBuilder_MotorStator))
                    {
                        MyObjectBuilder_MotorStator stator = (MyObjectBuilder_MotorStator)block;
                        if (stator.RotorEntityId != 0)
                        {
                            IMyEntity connectedEntity = null;

                            if (stator.RotorEntityId != null)
                                MyAPIGateway.Entities.TryGetEntityById((long)stator.RotorEntityId, out connectedEntity);

                            if (connectedEntity != null)
                            {
                                result = true;
                                break;
                            }
                        }
                    }

                    if (block.TypeId == typeof(MyObjectBuilder_MotorRotor))
                    {
                        result = true;
                        break;
                    }
                }

                if (!result)
                    connectedList.Add(entity);
            }
        }

        public static MyObjectBuilder_CubeGrid SafeGetObjectBuilder(IMyCubeGrid grid)
        {
            MyObjectBuilder_CubeGrid gridBuilder = null;
            try
            {
                gridBuilder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder();
            }
            catch
            {
            }

            return gridBuilder;
        }

        public static List<long> GetBigOwners(MyObjectBuilder_CubeGrid grid)
        {
            Dictionary<long, int> ownerList = new Dictionary<long, int>();
            foreach (MyObjectBuilder_CubeBlock block in grid.CubeBlocks)
            {
                if (block.Owner == 0)
                    continue;

                if (ownerList.ContainsKey(block.Owner))
                    ownerList[block.Owner] = ownerList[block.Owner] + 1;
                else
                    ownerList.Add(block.Owner, 1);
            }

            int count = ownerList.OrderBy(x => x.Value).Select(x => x.Value).FirstOrDefault();
            return ownerList.OrderBy(x => x.Value).Where(x => x.Value == count).Select(x => x.Key).ToList();
        }

        public static bool GetOwner(MyObjectBuilder_CubeGrid grid, out long ownerId)
        {
            ownerId = 0;
            foreach (MyObjectBuilder_CubeBlock block in grid.CubeBlocks)
            {
                if (!(block is MyObjectBuilder_TerminalBlock))
                    continue;

                MyObjectBuilder_TerminalBlock functional = (MyObjectBuilder_TerminalBlock)block;
                if (functional.Owner != 0)
                {
                    ownerId = functional.Owner;
                    return true;
                }
            }

            return false;
        }

    }
}