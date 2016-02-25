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
    using Sandbox.Game.World;
    using VRage;
    using VRage.Library.Collections;

    public static class CargoShips
    {
        private static SortedList<Vector3D, float> gravitySphereList = new SortedList<Vector3D, float>( );
        private static bool _init;
        private static List<MySpawnGroupDefinition> spawnGroupsList = new List<MySpawnGroupDefinition>( );
        private static float spawnGroupTotalFrequencies;
        private static List<float> spawnGroupCumulativeFrequencies = new List<float>( );
        private static Random random = new Random( );
        private static List<MyPhysics.HitInfo> m_raycastHits = new List<MyPhysics.HitInfo>( );
        private static float[ ] m_upVecMultipliers = { 1.0f, 1.0f, -1.0f, -1.0f };
        private static float[ ] m_rightVecMultipliers = { 1.0f, -1.0f, -1.0f, 1.0f };
        private static List<MyCubeGrid> m_tmpGridList = new List<MyCubeGrid>( );

        private static void Init( )
        {
            if ( _init )
                return;
            _init = true;

            //cache planet locations and gravity sphere radius
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
            Wrapper.GameAction( ( ) =>
             {
                 //MyAPIGateway.Entities.GetEntities( entities, x => x is MyPlanet );
                 MyAPIGateway.Entities.GetEntities(entities  );
             } );
            
            foreach ( IMyEntity entity in entities )
            {
                if (!( entity is MyPlanet ))
                    continue;

                MyPlanet planet = (MyPlanet) entity;
                gravitySphereList.Add( planet.PositionComp.GetPosition( ), planet.GravityLimit );
                Essentials.Log.Debug( planet.ToString());
                break;
            }
            
            //Communication.DisplayDialog( 76561197996829390, "", "",results );
            //load spawn groups
            var spawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions( );
            foreach ( var spawnGroup in spawnGroups )
            {
                if ( spawnGroup.IsEncounter == false && spawnGroup.IsPirate == false )
                {
                    spawnGroupsList.Add( spawnGroup );
                }
            }

            spawnGroupTotalFrequencies = 0.0f;
            spawnGroupCumulativeFrequencies.Clear( );

            foreach ( var spawnGroup in spawnGroupsList )
            {
                spawnGroupTotalFrequencies += spawnGroup.Frequency;
                spawnGroupCumulativeFrequencies.Add( spawnGroupTotalFrequencies );
            }
        }

        public static bool DoesTrajectoryIntersect( Vector3D start, Vector3D end, float spawnRadius )
        {
            Ray trajectory = new Ray( start, (end - start) );
            return DoesTrajectoryIntersect( trajectory, spawnRadius );
        }

        public static bool DoesTrajectoryIntersect( Ray trajectory, float spawnRadius )
        {
            Init( );
            foreach ( Vector3D center in gravitySphereList.Keys )
            {
                float gravityRadius;
                if ( !gravitySphereList.TryGetValue( center, out gravityRadius ) )
                {
                    Essentials.Log.Info( "Couldn't create bounding sphere around planet at " + center.ToString( ) );
                    return true;
                }

                BoundingSphereD gravitySphere = new BoundingSphereD( center, gravityRadius + spawnRadius );
                return trajectory.Intersects( gravitySphere ).HasValue;
            }
            return true;
        }

        public static MySpawnGroupDefinition PickRandomPrefab( )
        {
            Init( );
            if ( spawnGroupCumulativeFrequencies.Count == 0 )
            {
                Essentials.Log.Info( "Couldn't find random cargo. CumFreq=0" );
                return null;
            }

            float rnd = MyUtils.GetRandomFloat( 0.0f, spawnGroupTotalFrequencies );
            int i = 0;
            while ( i < spawnGroupCumulativeFrequencies.Count )
            {
                if ( rnd <= spawnGroupCumulativeFrequencies[i] )
                    break;

                ++i;
            }

            if ( i >= spawnGroupCumulativeFrequencies.Count )
                i = spawnGroupCumulativeFrequencies.Count - 1;

            return spawnGroupsList[i];
        }

  

        public static void SpawnPrefabWithTrajectory( )
        {

            MySpawnGroupDefinition spawnGroup = PickRandomPrefab( );
            spawnGroup.ReloadPrefabs();


            double spawnDistance = 8000.0f;
            Vector3D playerPosition = Vector3D.Zero;
            bool isWorldLimited = MyEntities.IsWorldLimited();
            if (isWorldLimited)
            {
                spawnDistance = Math.Min(spawnDistance, MyEntities.WorldSafeHalfExtent() - spawnGroup.SpawnRadius);
            }
            else
            {
                // In infinite worlds players can be thousands of kilometers away, so spawn ship around random player
                // so cargo ships will be spawned around every player at some time
                List<IMyPlayer> players = new List<IMyPlayer>( );
                MyAPIGateway.Players.GetPlayers( players );
                // In DS there can be no players connected
                int randomPlayerPosition = 0;
                if (players.Any( ))
                    randomPlayerPosition = random.Next( 0, players.Count - 1 );

                int i = 0;
                foreach (MyPlayer player in players)
                {
                    if (i == randomPlayerPosition)
                    {
                        if (player.Character != null)
                        {
                            playerPosition = player.GetPosition();
                        }
                        break;
                    }
                    i++;
                }

            }
            if (spawnDistance < 0.0f)
            {
                //MySandboxGame.Log.WriteLine("Not enough space in the world to spawn such a huge spawn group!");
                return;
            }

            double forbiddenRadius = 2000.0f;
            BoundingBoxD spawnBox;
            if (isWorldLimited)
            {
                spawnBox = new BoundingBoxD(new Vector3D(playerPosition - spawnDistance), new Vector3D(playerPosition + spawnDistance));
            }
            else
            {
                // We need to extend bouding box so cargo ships aren't spawned near other players
                GetSafeBoundingBoxForPlayers(playerPosition, spawnDistance, out spawnBox);
                // Forbidden radius is sphere around all players in box.
                // Bounding box is generated from players positions so their distance to center shall be same for all players
                forbiddenRadius += spawnBox.HalfExtents.Max() - 2000.0f;
            }

            // Get the direction to the center and deviate it randomly
            Vector3D? origin = MyUtils.GetRandomBorderPosition(ref spawnBox);
            origin = MyEntities.FindFreePlace(origin.Value, spawnGroup.SpawnRadius);
            if (!origin.HasValue)
            {
              //TODO: Can't find place
              Essentials.Log.Info("1"  );
                return;
            }

            // Radius in arc units of the forbidden sphere in the center, when viewed from origin
            float centerArcRadius = (float)Math.Atan(forbiddenRadius / (origin.Value - spawnBox.Center).Length());

            // Generate direction with elevation from centerArcRadius radians to (cAR + N_S_D_S) radians
            Vector3D direction = -Vector3D.Normalize(origin.Value);
            float theta = MyUtils.GetRandomFloat(centerArcRadius, centerArcRadius + 0.5f);
            float phi = MyUtils.GetRandomRadian();
            Vector3D cosVec = Vector3D.CalculatePerpendicularVector(direction);
            Vector3D sinVec = Vector3D.Cross(direction, cosVec);
            cosVec *= (Math.Sin(theta) * Math.Cos(phi));
            sinVec *= (Math.Sin(theta) * Math.Sin(phi));
            direction = direction * Math.Cos(theta) + cosVec + sinVec;

            Vector3D destination = Vector3D.Zero;
            RayD ray = new RayD(origin.Value, direction);
            double? intersection = ray.Intersects(spawnBox);
            Vector3D directionMult;
            if (!intersection.HasValue || intersection.Value < 20000.0f)
            {
                directionMult = direction * 20000.0f;
            }
            else
            {
                directionMult = direction * intersection.Value;
            }
            destination = origin.Value + directionMult;

            Vector3D upVector = Vector3D.CalculatePerpendicularVector(direction);
            Vector3D rightVector = Vector3D.Cross(direction, upVector);
            MatrixD originMatrix = MatrixD.CreateWorld(origin.Value, direction, upVector);
            

            // CH:TODO: Convex cast to detect collision
            // Check ships' path to avoid possible collisions. (TODO: But only if it is said in the definitions)
            
            m_raycastHits.Clear();
            foreach (var shipPrefab in spawnGroup.Prefabs)
            {
                var prefabDef = MyDefinitionManager.Static.GetPrefabDefinition(shipPrefab.SubtypeId);
                Debug.Assert(prefabDef != null);

                Vector3D shipPosition = Vector3.Transform(shipPrefab.Position, originMatrix);
                Vector3D shipDestination = shipPosition + directionMult;
                float radius = prefabDef == null ? 10.0f : prefabDef.BoundingSphere.Radius;

                if (DoesTrajectoryIntersect( shipPosition, shipDestination, spawnGroup.SpawnRadius ))
                {
                    Essentials.Log.Info("2"  );
                    return;
                }

                MyPhysics.CastRay(shipPosition, shipDestination, m_raycastHits, 24);
                if (m_raycastHits.Count > 0)
                {
                    Essentials.Log.Info("3"  );
                    return;
                }

                for (int i = 0; i < 4; ++i)
                {
                    Vector3D shiftVector = upVector * m_upVecMultipliers[i] * radius + rightVector * m_rightVecMultipliers[i] * radius;
                    MyPhysics.CastRay(shipPosition + shiftVector, shipDestination + shiftVector, m_raycastHits, 24);

                    if (m_raycastHits.Count > 0)
                    {
                        Essentials.Log.Info("4"  );
                        return;
                    }
                }
            }
            
            long spawnGroupId = MyPirateAntennas.GetPiratesId();

            // The ships were collision-free. Now spawn them
            foreach (var shipPrefab in spawnGroup.Prefabs)
            {

                // Yes, this could have been saved in the previous loop, but compared to (e.g.) raycasts, this does not take too much time to recalculate
                Vector3D shipPosition = Vector3D.Transform((Vector3D)shipPrefab.Position, originMatrix);
                Vector3D shipDestination = shipPosition + directionMult;
                Vector3D up = Vector3D.CalculatePerpendicularVector(-direction);

                m_tmpGridList.Clear();

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
                    spawningOptions: Sandbox.ModAPI.SpawningOptions.RotateFirstCockpitTowardsDirection |
                                     Sandbox.ModAPI.SpawningOptions.SpawnRandomCargo |
                                     Sandbox.ModAPI.SpawningOptions.DisableDampeners,
                                     ownerId: shipPrefab.ResetOwnership ? spawnGroupId : 0,
                    updateSync: true);
         
               
                m_tmpGridList.Clear();
                
            }



            



            /*
            //TODO: booo magic numbers
            //double spawnDistance = 10000.0f + random.Next( -1000, 1000 );
            
            //spawn the ship near a random player
            //List<IMyPlayer> players = new List<IMyPlayer>( );
            MyAPIGateway.Players.GetPlayers( players );

            IMyPlayer randomPlayer = players[random.Next( 0, players.Count - 1 )];

            Vector3D playerPos = randomPlayer.GetPosition( );

            List<IMyPlayer> closePlayers = new List<IMyPlayer>();

            bool canSpawn = true;
            for (int i = 0; i < 5; ++i)
            {
                Vector3D spawnPos = new Vector3D
                                    {
                                        X = playerPos.X + random.NextDouble( ),
                                        Y = playerPos.Y + random.NextDouble( ),
                                        Z = playerPos.Z + random.NextDouble( )
                                    };

                spawnPos = Vector3D.ClampToSphere( playerPos, spawnDistance );

                foreach (IMyPlayer player in players)
                {
                    if (( player.GetPosition( ) - spawnPos ).Length( ) <= spawnDistance - 100)
                    {
                        canSpawn = false;
                    }
                }
                if (canSpawn)
                {
                    Vector3D? tryPos = null;
                    Vector3D pos = spawnPos;
                    Wrapper.GameAction( ( ) =>
                                        {
                                            //TODO: MAGIC NUMBERS!
                                            tryPos= MyEntities.FindFreePlace( pos, 100 );
                                        });
                    if (tryPos.HasValue)
                    {
                        spawnPos = tryPos.Value;
                        break;
                    }
                }
                canSpawn = false;
            }
            if (!canSpawn)
            {
                Essentials.Log.Info( "Couldn't find a position to spawn ship." );
            }
            */


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