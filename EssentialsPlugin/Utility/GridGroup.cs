namespace EssentialsPlugin.Utility
{
    using System.Collections.Generic;
    using System.Linq;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using VRage.Game.Entity;

    public class GridGroup
    {
        private readonly HashSet<MyCubeGrid> _grids = new HashSet<MyCubeGrid>( );
        private readonly HashSet<MySlimBlock> _cubeBlocks = new HashSet<MySlimBlock>(); 
        private readonly List<MyCubeBlock> _fatBlocks = new List<MyCubeBlock>(); 
        private readonly List<long> _bigOwners = new List<long>(); 
        private readonly List<long> _smallOwners = new List<long>();
        private readonly MyCubeGrid _parent;

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

        public GridGroup( MyCubeGrid grid, GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical )
        {
            List<MyCubeGrid> tmpList = new List<MyCubeGrid>();

            Wrapper.GameAction( ( ) => tmpList = MyCubeGridGroups.Static.GetGroups( linkType ).GetGroupNodes( grid ) );

            foreach ( MyCubeGrid node in tmpList )
                _grids.Add( node );

            GetParent( ref _parent );
            GetCubeBlocks( ref _cubeBlocks );
            _GetFatBlocks( ref _fatBlocks );
            GetBigOwners( ref _bigOwners);
            GetSmallOwners( ref _smallOwners );
        }

        public static HashSet<GridGroup> GetGroups( HashSet<MyEntity> entities, GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical )
        {
            HashSet<GridGroup> result = new HashSet<GridGroup>();

            foreach ( MyEntity entity in entities.Where( x => x is MyCubeGrid  ) )
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                if ( grid == null )
                    continue;

                if ( !result.Any( x => x.Grids.Contains( grid ) ) )
                    result.Add( new GridGroup( grid, linkType ) );
            }

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
                                        if (grid?.Physics == null || grid.Closed)
                                            continue;

                                        grid.Close( );
                                    }
                                } );
        }

        private void GetParent( ref MyCubeGrid parent )
        {
            if ( _grids.Count < 1 )
                return;
            
            foreach ( MyCubeGrid grid in _grids )
            {
                if ( parent == null || (grid.BlocksCount > parent.BlocksCount) )
                    parent = grid;
            }
        }

        private void GetBigOwners(ref List<long> bigOwners)
        {
            bigOwners.Clear(  );
            Dictionary<long, int> owners = new Dictionary<long, int>();
            //foreach ( long ownerId in _grids.SelectMany( grid => grid.BigOwners ).Where( x => x > 0 ) )
            //    owners[ownerId] = 0;

            foreach ( var grid in _grids )
            {
                foreach ( var ownerId in grid.BigOwners )
                {
                    if ( ownerId > 0 && !bigOwners.Contains( ownerId ))
                        bigOwners.Add( ownerId );
                }
            }
            return;

            foreach ( MyCubeBlock block in _fatBlocks )
            {
                if ( owners.ContainsKey( block.OwnerId ) )
                    owners[block.OwnerId]++;
            }

            int maxCount = owners.Values.Max( );
            foreach ( long owner in owners.Keys )
            {
                if(owners[owner] == maxCount)
                    bigOwners.Add( owner );
            }
        }

        private void GetSmallOwners( ref List<long> smallOwners )
        {
            smallOwners.Clear(  );
            HashSet<long> result = new HashSet<long>( );
            foreach ( long owner in _grids.SelectMany( grid => grid.SmallOwners ).Where( x => x > 0 ) )
                result.Add( owner );
            smallOwners = result.ToList( );
        }

        private void GetCubeBlocks( ref HashSet<MySlimBlock> blocks )
        {
            foreach ( MyCubeGrid grid in _grids )
                blocks.UnionWith( grid.CubeBlocks );
        }

        private void _GetFatBlocks( ref List<MyCubeBlock> blocks )
        {
            blocks.Clear(  );
            blocks =  _cubeBlocks.Select( b => b?.FatBlock ).Where( f => f != null ).ToList(  );
        }
    }
}
