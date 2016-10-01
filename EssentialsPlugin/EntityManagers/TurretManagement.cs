namespace EssentialsPlugin.EntityManagers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using Sandbox.Game.Weapons;
    using Sandbox.Game.World;
    using Utility;
    using VRage.Game.Entity;
    using VRageMath;

    public class TurretManagement
    {
        private static TurretManagement _instance;

        public static TurretManagement Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TurretManagement( );

                return _instance;
            }
        }

        public void ProcessTurrets( )
        {
            List<MyEntity> entities = new List<MyEntity>( );
            Wrapper.GameAction( ( ) => entities = MyEntities.GetEntities( ).ToList( ) );

            foreach (MyEntity entity in entities)
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                if (grid == null)
                    continue;

                if (DoesGridHaveTarget( grid ))
                    EnableTurrets( grid );
                else
                    DisableTurrets( grid );
            }
        }

        public void EnableAllTurrets( )
        {
            Task.Run( ( ) =>
                      {
                          HashSet<MyEntity> entities = new HashSet<MyEntity>( );
                          Wrapper.GameAction( ( ) => entities = MyEntities.GetEntities( ) );

                          foreach (MyEntity entity in entities)
                          {
                              MyCubeGrid grid = entity as MyCubeGrid;
                              if (grid == null)
                                  continue;

                              EnableTurrets( grid );
                          }
                      } );
        }

        private void DisableTurrets( MyCubeGrid grid )
        {
            int count = 0;
            Wrapper.GameAction( ( ) =>
                                {
                                    HashSet<MySlimBlock> blocks = grid.GetBlocks( );

                                    foreach (MySlimBlock block in blocks)
                                    {
                                        MyLargeTurretBase turret = block.FatBlock as MyLargeTurretBase;
                                        if (turret == null)
                                            continue;

                                        if (!turret.Enabled)
                                            continue;

                                        turret.Enabled = false;
                                        MyEntities.UnregisterForUpdate( turret );
                                        count++;
                                    }
                                } );
            if (count > 0)
                Essentials.Log.Info( $"Disabled {count} turrets on grid {grid.DisplayName}" );
        }

        private void EnableTurrets( MyCubeGrid grid )
        {
            int count = 0;
            Wrapper.GameAction( ( ) =>
                                {
                                    HashSet<MySlimBlock> blocks = grid.GetBlocks( );

                                    foreach (MySlimBlock block in blocks)
                                    {
                                        MyLargeTurretBase turret = block.FatBlock as MyLargeTurretBase;
                                        if (turret == null)
                                            continue;

                                        if (turret.Enabled)
                                            continue;

                                        turret.Enabled = true;
                                        MyEntities.RegisterForUpdate( turret );
                                        count++;
                                    }
                                } );
            if (count > 0)
                Essentials.Log.Info( $"Enabled {count} turrets on grid {grid.DisplayName}" );
        }

        private bool DoesGridHaveTarget( MyCubeGrid grid )
        {
            HashSet<MySlimBlock> blocks = grid.GetBlocks( );
            BoundingSphereD sphere = new BoundingSphereD( Vector3D.Zero, 0 );
            bool hasTurrets = false;
            foreach (MySlimBlock block in blocks)
            {
                MyLargeTurretBase turret = block.FatBlock as MyLargeTurretBase;
                if (turret == null)
                    continue;

                hasTurrets = true;

                BoundingSphereD gunSphere = new BoundingSphereD( turret.PositionComp.GetPosition( ), turret.ShootingRange * 2 );
                if (sphere.Radius == 0)
                    sphere = gunSphere;
                else
                    sphere.Include( gunSphere );
            }

            if (!hasTurrets)
                return false;

            MyEntity[] targets;
            try
            {
                var list = new List<MyEntity>( );
                Wrapper.GameAction( ( ) => list = MyEntities.GetTopMostEntitiesInSphere( ref sphere ) );
                targets = list.ToArray( );
            }
            catch (Exception ex)
            {
                Essentials.Log.Error( ex );
                return true;
            }

            foreach (MyEntity target in targets)
            {
                if (target.EntityId == grid.EntityId)
                    continue;

                if (target is MyVoxelBase)
                    continue;

                MyPlayer player = MySession.Static.Players.GetControllingPlayer( target );
                long controllingIdentity = player?.Identity.IdentityId ?? 0;

                MyCubeGrid targetGrid = target as MyCubeGrid;

                switch (PluginSettings.Instance.DynamicTurretManagementMode)
                {
                    case DynamicTurretManagementMode.All:
                        return true;
                    case DynamicTurretManagementMode.AllButOwner:
                        if (controllingIdentity == 0)
                        {
                            if (targetGrid != null)
                                if (!targetGrid.BigOwners.Intersect( grid.BigOwners ).Any( ))
                                    return true;
                            return true;
                        }
                        if (!grid.SmallOwners.Contains( controllingIdentity ))
                            return true;
                        break;
                    case DynamicTurretManagementMode.NeutralAndEnemy:
                        if (controllingIdentity == 0)
                        {
                            if (targetGrid != null)
                                if (!targetGrid.BigOwners.Intersect( grid.SmallOwners ).Any( ))
                                    return true;
                            return true;
                        }
                        if (!grid.SmallOwners.Contains( controllingIdentity ))
                            return true;
                        break;
                    case DynamicTurretManagementMode.Enemy:
                        if (controllingIdentity == 0)
                        {
                            if (targetGrid != null)
                            {
                                IEnumerable<long> res = targetGrid.BigOwners.Intersect( grid.SmallOwners );
                                if (!res.Any( ))
                                    return true;
                            }
                            break;
                        }
                        if (!grid.SmallOwners.Contains( controllingIdentity ))
                        {
                            Essentials.Log.Debug( "!" );
                            return true;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException( );
                }
            }

            return false;
        }
    }
}
