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

    public class GridGroup
    {
        private readonly HashSet<MyCubeGrid> _grids = new HashSet<MyCubeGrid>( );
        private HashSet<MySlimBlock> _cubeBlocks = new HashSet<MySlimBlock>(); 
        private List<MyCubeBlock> _fatBlocks = new List<MyCubeBlock>(); 
        private List<long> _bigOwners = new List<long>(); 
        private List<long> _smallOwners = new List<long>();
        private MyCubeGrid _parent;

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
            //HACK: Manually create a group for out of scene grids because pulling them from the server crashes
            if ( grid.InScene )
            {
                List<MyCubeGrid> tmpList = new List<MyCubeGrid>( );

                //find the group containing this grid with the given link type
                Wrapper.GameAction( ( ) => tmpList = MyCubeGridGroups.Static.GetGroups( linkType ).GetGroupNodes( grid ) );

                foreach ( MyCubeGrid node in tmpList )
                    _grids.Add( node );
            }
            else
            {
                _grids.Add( grid );
            }

            //populate our internal lists
            GetParent( );
            GetCubeBlocks( );
            _GetFatBlocks( );
            GetBigOwners( );
            GetSmallOwners( );
        }
        
        public static HashSet<GridGroup> GetGroups( HashSet<MyEntity> entities, GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical )
        {
            HashSet<GridGroup> result = new HashSet<GridGroup>();
            
            //Create a copy of the entities list;
            //group processing can take so long that the internal list of entities can change
            //which is Bad.
            MyEntity[] entitiesCopy = new MyEntity[entities.Count];
            entities.CopyTo( entitiesCopy );
            List<Task> groupTasks = new List<Task>();

            foreach ( MyEntity entity in entitiesCopy )
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                
                if ( grid?.Physics == null || grid.Closed )
                    continue;
                
                //on large servers this can run into the tens of seconds, so parallelize it
                groupTasks.Add( Task.Run( ( ) =>
                                          {
                                              if ( result.Any( x => x.Grids.Contains( grid ) ) )
                                                  return;

                                              lock ( result )
                                              {
                                                  result.Add( new GridGroup( grid, linkType ) );
                                              }
                                          } ) );
            }

            Task.WaitAll( groupTasks.ToArray(  ) );
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

        private void GetParent(  )
        {
            if ( _grids.Count < 1 )
                return;
            
            foreach ( MyCubeGrid grid in _grids )
            {
                if ( _parent == null || (grid.BlocksCount > _parent.BlocksCount) )
                    _parent = grid;
            }
        }

        private void GetBigOwners( )
        {
            //TODO: Actually process the list of owners to get the big owner of the entire group
            _bigOwners.Clear(  );
            Dictionary<long, int> owners = new Dictionary<long, int>();
            //foreach ( long ownerId in _grids.SelectMany( grid => grid.BigOwners ).Where( x => x > 0 ) )
            //    owners[ownerId] = 0;

            foreach ( var grid in _grids )
            {
                foreach ( var ownerId in grid.BigOwners )
                {
                    if (!_bigOwners.Contains( ownerId ) && ownerId > 0 )
                        _bigOwners.Add( ownerId );
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
                    _bigOwners.Add( owner );
            }
        }

        private void GetSmallOwners( )
        {
            _smallOwners.Clear(  );
            HashSet<long> result = new HashSet<long>( );
            foreach ( long owner in _grids.SelectMany( grid => grid.SmallOwners ).Where( x => x > 0 ) )
                result.Add( owner );
            _smallOwners = result.ToList( );
        }

        private void GetCubeBlocks( )
        {
            _cubeBlocks.Clear(  );
            foreach ( MyCubeGrid grid in _grids )
                _cubeBlocks.UnionWith( grid.CubeBlocks );
        }

        private void _GetFatBlocks(  )
        {
            _fatBlocks.Clear(  );
            _fatBlocks =  _cubeBlocks.Select( b => b?.FatBlock ).Where( f => f != null ).ToList(  );
        }
    }
}
