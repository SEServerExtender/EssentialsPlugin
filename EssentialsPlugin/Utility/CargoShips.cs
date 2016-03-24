namespace EssentialsPlugin.Utility
{
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using VRage.ModAPI;
    using VRage.Utils;
    using VRageMath;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Sandbox;
    using Sandbox.Engine.Physics;
    using Sandbox.Game.GameSystems;
    using Sandbox.Game.World;
    using SEModAPI.API;
    using VRage;
    using VRage.Game.ModAPI;
    using VRage.Library.Collections;

    public static class CargoShips
    {
        private static List<BoundingSphereD> gravitySphereList = new List<BoundingSphereD>( );
        private static bool _init;

        public const float NEUTRAL_SHIP_SPAWN_DISTANCE = 8000.0f;
        public const float NEUTRAL_SHIP_FORBIDDEN_RADIUS = 2000.0f;
        public const float NEUTRAL_SHIP_DIRECTION_SPREAD = 0.5f;
        public const float NEUTRAL_SHIP_MINIMAL_ROUTE_LENGTH = 10000.0f;
        public static TimeSpan NEUTRAL_SHIP_RESCHEDULE_TIME = TimeSpan.FromSeconds( 10 ); // If spawning does not succeed, retry in 10 seconds
        public static TimeSpan NEUTRAL_SHIP_MIN_TIME = TimeSpan.FromMinutes( 13 ); // Re-spawn time = 13-17 minutes
        public static TimeSpan NEUTRAL_SHIP_MAX_TIME = TimeSpan.FromMinutes( 17 );

        private static List<MyPhysics.HitInfo> m_raycastHits = new List<MyPhysics.HitInfo>( );
        private static List<MyCubeGrid> m_tmpGridList = new List<MyCubeGrid>( );

        private static List<float> m_spawnGroupCumulativeFrequencies = new List<float>( );
        private static float m_spawnGroupTotalFrequencies = 0.0f;
        private static float[ ] m_upVecMultipliers = { 1.0f, 1.0f, -1.0f, -1.0f };
        private static float[ ] m_rightVecMultipliers = { 1.0f, -1.0f, -1.0f, 1.0f };

        private static List<MySpawnGroupDefinition> m_spawnGroups = new List<MySpawnGroupDefinition>( );


        private static void Init( )
        {
            if ( _init )
                return;
            _init = true;
            Essentials.Log.Info( "Init cargo ships" );

            //load spawn groups
             var spawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
            foreach (var spawnGroup in spawnGroups)
            {
                if (spawnGroup.IsEncounter == false && spawnGroup.IsPirate == false)
                {
                    m_spawnGroups.Add(spawnGroup);
                }
            }

            m_spawnGroupTotalFrequencies = 0.0f;
            m_spawnGroupCumulativeFrequencies.Clear();

            foreach (var spawnGroup in m_spawnGroups)
            {
                m_spawnGroupTotalFrequencies += spawnGroup.Frequency;
                m_spawnGroupCumulativeFrequencies.Add(m_spawnGroupTotalFrequencies);
            }
        }

        public static bool DoesTrajectoryIntersect( Vector3D start, Vector3D end, float spawnRadius )
        {
            Ray trajectory = new Ray( start, (end - start) );
            return DoesTrajectoryIntersect( trajectory, spawnRadius );
        }

        public static bool DoesTrajectoryIntersect( Ray trajectory, float spawnRadius )
        {
            if ( ExtenderOptions.IsDebugging )
                Essentials.Log.Info( "Checking gravity intersect" );

            foreach ( IMyGravityProvider provider in MyGravityProviderSystem.NaturalGravityProviders )
            {
                MyPlanet planet = provider as MyPlanet;
                if ( planet == null )
                    continue;
                BoundingSphereD gravitySphere = new BoundingSphereD( planet.PositionComp.GetPosition(  ), planet.GravityLimit + spawnRadius);
                
                float? intersect = trajectory.Intersects( gravitySphere );
                if ( intersect.HasValue && intersect.Value > 0 )
                    return true;
            }
            /*
            foreach ( BoundingSphereD sphere in gravitySphereList )
            {
                if ( trajectory.Intersects( sphere ).HasValue )
                    return true;
            }
            */
            return false;
        }

        public static bool IsPointInGravity( Vector3D testPoint )
        {
            if ( ExtenderOptions.IsDebugging )
                Essentials.Log.Info( "Checking point gravity intersect" );
            
            return !Vector3D.IsZero( MyGravityProviderSystem.CalculateNaturalGravityInPoint( testPoint, true ) );

            /*
            foreach ( BoundingSphereD sphere in gravitySphereList )
            {
                if ( sphere.Contains( testPoint ) == ContainmentType.Contains)
                    return true;
            }

            return false;
            */
        }
        
        public static void SpawnCargoShip()
        {
            Init(  );
            Wrapper.GameAction( ( ) =>
                                {
                                    if ( ExtenderOptions.IsDebugging )
                                        Essentials.Log.Info( "Spawn cargo ship" );
                                    // Select a spawn group to spawn
                                    MySpawnGroupDefinition spawnGroup = PickRandomSpawnGroup( );
                                    if ( spawnGroup == null )
                                    {
                                        return;
                                    }

                                    spawnGroup.ReloadPrefabs( );
                                    
                                    double spawnDistance = NEUTRAL_SHIP_SPAWN_DISTANCE;
                                    Vector3D playerPosition = Vector3D.Zero;
                                    bool isWorldLimited = MyEntities.IsWorldLimited( );
                                    int numPlayers = 0;

                                    if ( isWorldLimited )
                                    {
                                        spawnDistance = Math.Min( spawnDistance, MyEntities.WorldSafeHalfExtent( ) - spawnGroup.SpawnRadius );
                                    }
                                    else
                                    {
                                        // In infinite worlds players can be thousands of kilometers away, so spawn ship around random player
                                        // so cargo ships will be spawned around every player at some time
                                        var players = MySession.Static.Players.GetOnlinePlayers( );
                                        // In DS there can be no players connected
                                        numPlayers = Math.Max( 0, players.Count - 1 );
                                        int randomPlayerPosition = MyUtils.GetRandomInt( 0, numPlayers );
                                        int i = 0;
                                        foreach ( var player in players )
                                        {
                                            if ( i == randomPlayerPosition )
                                            {
                                                if ( player.Character != null )
                                                {
                                                    playerPosition = player.GetPosition( );
                                                }
                                                break;
                                            }
                                            i++;
                                        }

                                    }
                                    
                                    if ( spawnDistance < 0.0f )
                                    {
                                        if ( ExtenderOptions.IsDebugging )
                                            Essentials.Log.Info( "Not enough space in the world to spawn such a huge spawn group!" );
                                        return;
                                    }

                                    double forbiddenRadius = NEUTRAL_SHIP_FORBIDDEN_RADIUS;
                                    BoundingBoxD spawnBox;
                                    if ( isWorldLimited )
                                    {
                                        spawnBox = new BoundingBoxD( new Vector3D( playerPosition - spawnDistance ), new Vector3D( playerPosition + spawnDistance ) );
                                    }
                                    else
                                    {
                                        // We need to extend bouding box so cargo ships aren't spawned near other players
                                        GetSafeBoundingBoxForPlayers( playerPosition, spawnDistance, out spawnBox );
                                        // Forbidden radius is sphere around all players in box.
                                        // Bounding box is generated from players positions so their distance to center shall be same for all players
                                        forbiddenRadius += spawnBox.HalfExtents.Max( ) - NEUTRAL_SHIP_FORBIDDEN_RADIUS;
                                    }
                                    // Get the direction to the center and deviate it randomly
                                    Vector3D? origin = MyUtils.GetRandomBorderPosition( ref spawnBox );

                                    origin = MyEntities.FindFreePlace( origin.Value, spawnGroup.SpawnRadius );
                                    if ( !origin.HasValue )
                                    {
                                        if ( ExtenderOptions.IsDebugging )
                                            Essentials.Log.Info( "Couldn't find free place for cargo spawn" );
                                        return;
                                    }
                                    
                                    // Radius in arc units of the forbidden sphere in the center, when viewed from origin
                                    float centerArcRadius = (float)Math.Atan( forbiddenRadius / ( origin.Value - spawnBox.Center ).Length( ) );
                                    
                                    // Generate direction with elevation from centerArcRadius radians to (cAR + N_S_D_S) radians
                                    Vector3D direction = -Vector3D.Normalize( origin.Value );
                                    float theta = MyUtils.GetRandomFloat( centerArcRadius, centerArcRadius + NEUTRAL_SHIP_DIRECTION_SPREAD );
                                    float phi = MyUtils.GetRandomRadian( );
                                    Vector3D cosVec = Vector3D.CalculatePerpendicularVector( direction );
                                    Vector3D sinVec = Vector3D.Cross( direction, cosVec );
                                    cosVec *= ( Math.Sin( theta ) * Math.Cos( phi ) );
                                    sinVec *= ( Math.Sin( theta ) * Math.Sin( phi ) );
                                    direction = direction * Math.Cos( theta ) + cosVec + sinVec;
                                    
                                    Vector3D destination = Vector3D.Zero;
                                    RayD ray = new RayD( origin.Value, direction );
                                    double? intersection = ray.Intersects( spawnBox );
                                    Vector3D directionMult;
                                    if ( !intersection.HasValue || intersection.Value < NEUTRAL_SHIP_MINIMAL_ROUTE_LENGTH )
                                    {
                                        directionMult = direction * NEUTRAL_SHIP_MINIMAL_ROUTE_LENGTH;
                                    }
                                    else
                                    {
                                        directionMult = direction * intersection.Value;
                                    }
                                    destination = origin.Value + directionMult;
                                    
                                    Vector3D upVector = Vector3D.CalculatePerpendicularVector( direction );
                                    Vector3D rightVector = Vector3D.Cross( direction, upVector );
                                    MatrixD originMatrix = MatrixD.CreateWorld( origin.Value, direction, upVector );
                                    
                                    // CH:TODO: Convex cast to detect collision
                                    // Check ships' path to avoid possible collisions. (TODO: But only if it is said in the definitions)
                                    m_raycastHits.Clear( );
                                    foreach ( var shipPrefab in spawnGroup.Prefabs )
                                    {
                                        var prefabDef = MyDefinitionManager.Static.GetPrefabDefinition( shipPrefab.SubtypeId );
                                        Debug.Assert( prefabDef != null );

                                        Vector3D shipPosition = Vector3.Transform( shipPrefab.Position, originMatrix );
                                        Vector3D shipDestination = shipPosition + directionMult;
                                        float radius = prefabDef == null ? 10.0f : prefabDef.BoundingSphere.Radius;

                                        if ( IsPointInGravity( shipPosition ) )
                                        {
                                            if(ExtenderOptions.IsDebugging)
                                                Essentials.Log.Info( "Failed to spawn cargo ship: Spawn location is in gravity well" );
                                            return;
                                        }
                                        if ( IsPointInGravity( shipDestination ) )
                                        {
                                            if(ExtenderOptions.IsDebugging)
                                                Essentials.Log.Info( "Failed to spawn cargo ship: Destination is in gravity well" );
                                        }
                                        if ( DoesTrajectoryIntersect( shipPosition, shipDestination, spawnGroup.SpawnRadius ) )
                                        {
                                            if ( ExtenderOptions.IsDebugging )
                                                Essentials.Log.Info( "Failed to spawn cargo ship: Ship path intersects gravity well" );
                                            return;
                                        }

                                        MyPhysics.CastRay( shipPosition, shipDestination, m_raycastHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer );
                                        if ( m_raycastHits.Count > 0 )
                                        {
                                            if ( ExtenderOptions.IsDebugging )
                                                Essentials.Log.Info("Failed to spawn cargo ship: Ship path intersects another object");
                                            return;
                                        }

                                        for ( int i = 0; i < 4; ++i )
                                        {
                                            Vector3D shiftVector = upVector * m_upVecMultipliers[i] * radius + rightVector * m_rightVecMultipliers[i] * radius;
                                            MyPhysics.CastRay( shipPosition + shiftVector, shipDestination + shiftVector, m_raycastHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer );

                                            if ( m_raycastHits.Count > 0 )
                                            {
                                                if(ExtenderOptions.IsDebugging)
                                                    Essentials.Log.Info("Failed to spawn cargo ship: Ship path intersects another object");
                                                return;
                                            }
                                        }
                                    }

                                    long spawnGroupId = MyPirateAntennas.GetPiratesId( );

                                    // The ships were collision-free. Now spawn them
                                    foreach ( var shipPrefab in spawnGroup.Prefabs )
                                    {
                                        // Yes, this could have been saved in the previous loop, but compared to (e.g.) raycasts, this does not take too much time to recalculate
                                        Vector3D shipPosition = Vector3D.Transform( (Vector3D)shipPrefab.Position, originMatrix );
                                        Vector3D shipDestination = shipPosition + directionMult;
                                        Vector3D up = Vector3D.CalculatePerpendicularVector( -direction );

                                        m_tmpGridList.Clear( );

                                        // CH: We don't want a new identity for each ship anymore. We should handle that in a better way...
                                        /*if (shipPrefab.ResetOwnership)
                {
                    if (spawnGroupId == 0)
                    {
                        //This is not an NPC so that it doesn't show up in assign ownership drop down menu
                        MyIdentity spawnGroupIdentity = Sync.Players.CreateNewIdentity("Neutral NPC");
                        spawnGroupId = spawnGroupIdentity.IdentityId;
                    }
                }*/

                                        // Deploy ship
                                        MyPrefabManager.Static.SpawnPrefab(
                                            resultList: m_tmpGridList,
                                            prefabName: shipPrefab.SubtypeId,
                                            position: shipPosition,
                                            forward: direction,
                                            up: up,
                                            initialLinearVelocity: shipPrefab.Speed * direction,
                                            beaconName: shipPrefab.BeaconText,
                                            spawningOptions: VRage.Game.ModAPI.SpawningOptions.RotateFirstCockpitTowardsDirection |
                                                             VRage.Game.ModAPI.SpawningOptions.SpawnRandomCargo |
                                                             VRage.Game.ModAPI.SpawningOptions.DisableDampeners,
                                            ownerId: shipPrefab.ResetOwnership ? spawnGroupId : 0,
                                            updateSync: true );

                                        /*
                foreach (var grid in m_tmpGridList)
                {
                    var cockpit = grid.GetFirstBlockOfType<MyCockpit>();
                    if (cockpit != null)
                    {
                        MySimpleAutopilot ai = new MySimpleAutopilot(shipDestination, (Vector3)direction);
                        cockpit.AttachAutopilot(ai);
                        break;
                    }
                }
                */
                                        m_tmpGridList.Clear( );
                                    }
                                } );
        }

        private static MySpawnGroupDefinition PickRandomSpawnGroup( )
        {
            if ( m_spawnGroupCumulativeFrequencies.Count == 0 )
            {
                return null;
            }

            float rnd = MyUtils.GetRandomFloat( 0.0f, m_spawnGroupTotalFrequencies );
            int i = 0;
            while ( i < m_spawnGroupCumulativeFrequencies.Count )
            {
                if ( rnd <= m_spawnGroupCumulativeFrequencies[i] )
                    break;

                ++i;
            }

            Debug.Assert( i < m_spawnGroupCumulativeFrequencies.Count, "Could not sample a spawn group" );
            if ( i >= m_spawnGroupCumulativeFrequencies.Count )
                i = m_spawnGroupCumulativeFrequencies.Count - 1;

            ProfilerShort.End( );
            return m_spawnGroups[i];
        }

        private static void GetSafeBoundingBoxForPlayers( Vector3D start, double spawnDistance, out BoundingBoxD output )
        {
            double tolerance = 10.0f;
            BoundingSphereD sphere = new BoundingSphereD( start, tolerance );

            var players = MySession.Static.Players.GetOnlinePlayers( );
            bool tryIncludeOtherPlayers = true;

            // We have to try adding other players until the bounding sphere stays the same
            while ( tryIncludeOtherPlayers )
            {
                tryIncludeOtherPlayers = false;
                foreach ( var player in players )
                {
                    Vector3D playerPosition = player.GetPosition( );
                    double distanceFromSphere = (sphere.Center - playerPosition).Length( ) - sphere.Radius;

                    if ( distanceFromSphere <= 0.0 )
                        continue;
                    if ( distanceFromSphere > spawnDistance * 2.0f )
                        continue;

                    sphere.Include( new BoundingSphereD( playerPosition, tolerance ) );
                    tryIncludeOtherPlayers = true;
                }
            }

            sphere.Radius += spawnDistance;
            output = new BoundingBoxD( sphere.Center - new Vector3D( sphere.Radius ), sphere.Center + new Vector3D( sphere.Radius ) );

            var entities = MyEntities.GetEntitiesInAABB( ref output );
            foreach ( var entity in entities )
            {
                if ( entity is MyCubeGrid )
                {
                    var cubeGrid = entity as MyCubeGrid;
                    if ( cubeGrid.IsStatic )
                    {
                        Vector3D gridPosition = cubeGrid.PositionComp.GetPosition( );

                        // If grid is close to picked player we need to include it's "safe" bounding box for spawning ships,
                        // so cargo ships don't spawn near it.

                        output.Include( new BoundingBoxD( new Vector3D( gridPosition - spawnDistance ), new Vector3D( gridPosition + spawnDistance ) ) );
                    }
                }
            }
            entities.Clear( );
        }
        
    }
}