namespace EssentialsPlugin.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using VRage.Game.Entity;
    using VRage.ModAPI;
    using VRageMath;

    public class GridGroup
    {
        private HashSet<MyCubeGrid> _grids = new HashSet<MyCubeGrid>( );
        private HashSet<MySlimBlock> _cubeBlocks = new HashSet<MySlimBlock>(); 
        private List<MyCubeBlock> _fatBlocks = new List<MyCubeBlock>(); 
        private List<long> _bigOwners = new List<long>(); 
        private List<long> _smallOwners = new List<long>();
        private MyCubeGrid _parent;
        private GridLinkTypeEnum _linkType;

        public HashSet<MyCubeGrid> Grids
        {
            get { return _grids; }
        }

        public HashSet<MySlimBlock> CubeBlocks
        {
            get {return _cubeBlocks;}
        }

        public List<MyCubeBlock> GetFatBlocks()
        {
            return _fatBlocks;
        } 

        /// <summary>
        /// The number of blocks in the group
        /// </summary>
        public int BlocksCount
        {
            get { return _cubeBlocks.Count; }
        }

        /// <summary>
        /// Players which own a majority of all blocks in the group
        /// </summary>
        public List<long> BigOwners
        {
            get {return _bigOwners;}
        }

        /// <summary>
        /// Players which own any blocks in the group
        /// </summary>
        public List<long> SmallOwners
        {
            get {return _smallOwners;}
        }

        /// <summary>
        /// Largest grid in the group
        /// </summary>
        public MyCubeGrid Parent
        {
            get { return _parent; }
        }

        public GridLinkTypeEnum LinkType
        {
            get { return _linkType; }
        }

        public GridGroup( MyCubeGrid grid, GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical )
        {
            _linkType = linkType;

            if ( grid.InScene )
            {
                List<MyCubeGrid> tmpList = new List<MyCubeGrid>( );

                //find the group containing this grid with the given link type
                Wrapper.GameAction( ( ) => tmpList = MyCubeGridGroups.Static.GetGroups( linkType ).GetGroupNodes( grid ) );
                
                _grids.UnionWith( tmpList );
            }
            //HACK: Manually create a group for out of scene grids because pulling them from the server crashes
            else
            {
                //use the old method to filter out grids with pisons or rotors, for safety
                HashSet<IMyEntity> thisEntity = new HashSet<IMyEntity>();
                HashSet<IMyEntity> returnSet = new HashSet<IMyEntity>();
                thisEntity.Add( (IMyEntity)grid );
                CubeGrids.GetGridsUnconnected( returnSet, thisEntity );
                
                if ( returnSet.Count > 0 )
                    _grids.Add( grid );
                else
                    return;
            }

            //populate our internal lists
            ComputeParent( );
            ComputeCubeBlocks( );
            ComputeFatBlocks( );
            ComputeSmallOwners( );
            ComputeBigOwners( );
        }
        
        public static HashSet<GridGroup> GetGroups( HashSet<MyEntity> entities, GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical )
        {
            HashSet<GridGroup> result = new HashSet<GridGroup>();
            
            //Create a copy of the entities list;
            //group processing can take so long that the internal list of entities can change
            //which is Bad.
            MyEntity[] entitiesCopy = new MyEntity[entities.Count];
            entities.CopyTo( entitiesCopy );

                //on large servers this can run into the tens of seconds, so parallelize it
            Parallel.ForEach( entitiesCopy, ( entity ) =>
                                            {
                                                MyCubeGrid grid = entity as MyCubeGrid;

                                                if (grid?.Physics == null || grid.Closed)
                                                    return;

                                                lock (result)
                                                    if (result.Any( x => x.Grids.Contains( grid ) ))
                                                        return;

                                                var newGroup = new GridGroup( grid, linkType );

                                                lock (result)
                                                    result.Add( newGroup );
                                            } );
            
            return result;
        }

        public static HashSet<GridGroup> GetAllGroups( GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical )
        {
            HashSet<MyEntity> entities = new HashSet<MyEntity>();
            Wrapper.GameAction( () => entities = MyEntities.GetEntities(  ) );
            return GetGroups( entities, linkType );
        }

        public void Close( )
        {
            Wrapper.GameAction( ( ) =>
                                {
                                    foreach (MyCubeGrid grid in _grids)
                                    {
                                        if (grid.Physics == null || grid.Closed)
                                            continue;

                                        grid.Close( );
                                    }
                                } );
        }

        public void Stop( )
        {
            foreach ( MyCubeGrid grid in _grids )
            {
                Wrapper.GameAction( ( ) => grid.Stop(  ) );
            }
        }

        public void UpdateGroup()
        {
            HashSet<MyCubeGrid> newGrids = new HashSet<MyCubeGrid>();

            foreach (var newGrid in MyCubeGridGroups.Static.GetGroups( _linkType ).GetGroupNodes( _parent ))
                newGrids.Add( newGrid );

            if (_grids != newGrids)
            {
                _grids = newGrids;

                //populate our internal lists
                ComputeParent();
                ComputeCubeBlocks();
                ComputeFatBlocks();
                ComputeSmallOwners();
                ComputeBigOwners();
            }
        }

        private void ComputeParent(  )
        {
            if ( _grids.Count < 1 )
                return;
            
            foreach ( MyCubeGrid grid in _grids )
            {
                if ( _parent == null || (grid.BlocksCount > _parent.BlocksCount) )
                    _parent = grid;
            }
        }

        private void ComputeBigOwners( )
        {
            _bigOwners.Clear(  );
            Dictionary<long, int> owners = new Dictionary<long, int>();

            foreach ( MyCubeBlock block in _fatBlocks )
            {
                if (block.OwnerId == 0) //block owned by nobody, don't process it
                    continue;

                if ( owners.ContainsKey( block.OwnerId ) )
                    owners[block.OwnerId]++;
                else
                    owners.Add( block.OwnerId, 1 );
            }

            int mostBlocks = 0;
            foreach ( var owner in owners )
            {
                if (owner.Value > mostBlocks)
                    mostBlocks = owner.Value;
            }

            foreach (var owner in owners)
            {
                if(owner.Value == mostBlocks)
                    _bigOwners.Add( owner.Key );
            }
        }

        private void ComputeSmallOwners( )
        {
            _smallOwners.Clear(  );
            foreach (MyCubeGrid grid in _grids)
            {
                foreach (long owner in grid.SmallOwners)
                {
                    if (!_smallOwners.Contains( owner ))
                        _smallOwners.Add( owner );
                }
            }
        }

        private void ComputeCubeBlocks( )
        {
            _cubeBlocks.Clear(  );
            foreach ( MyCubeGrid grid in _grids )
                _cubeBlocks.UnionWith( grid.CubeBlocks );
        }

        private void ComputeFatBlocks(  )
        {
            _fatBlocks.Clear(  );
            foreach (MyCubeGrid grid in _grids)
            {
                foreach(MyCubeBlock block in grid.GetFatBlocks())
                    _fatBlocks.Add( block );
            }
        }
    }
}
