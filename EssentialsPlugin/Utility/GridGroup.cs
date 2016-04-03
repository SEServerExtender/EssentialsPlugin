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
        private readonly HashSet<MySlimBlock> _cubeBlocks; 
        private readonly List<long> _bigOwners; 
        private readonly List<long> _smallOwners;
        private readonly MyCubeGrid _parent;

        public HashSet<MyCubeGrid> Grids
        {
            get { return _grids; }
        }

        public HashSet<MySlimBlock> CubeBlocks
        {
            get {return _cubeBlocks;}
        }

        public int BlocksCount
        {
            get { return _cubeBlocks.Count; }
        }

        public List<long> BigOwners
        {
            get {return _bigOwners;}
        }

        public List<long> SmallOwners
        {
            get {return _smallOwners;}
        }

        public MyCubeGrid Parent
        {
            get { return _parent; }
        }

        public GridGroup( MyCubeGrid grid, GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical )
        {
            List<MyCubeGrid> tmpList = new List<MyCubeGrid>();

            Wrapper.GameAction( ( ) =>tmpList= MyCubeGridGroups.Static.GetGroups( linkType ).GetGroupNodes( grid ) );

            foreach ( MyCubeGrid node in tmpList )
                _grids.Add( node );

            _parent = GetParent( );
            _cubeBlocks = GetCubeBlocks( );
            _bigOwners = GetBigOwners( );
            _smallOwners = GetSmallOwners( );
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

        private MyCubeGrid GetParent( )
        {
            if ( _grids.Count < 1 )
                return null;

            MyCubeGrid result = null;
            foreach ( MyCubeGrid grid in _grids )
            {
                if ( result == null || grid.BlocksCount > result.BlocksCount )
                    result = grid;
            }
            return result;
        }

        private List<long> GetBigOwners( )
        {
            HashSet<long> result = new HashSet<long>( );
            foreach ( long owner in _grids.SelectMany( grid => grid.BigOwners ).Where( x => x > 0 ) )
                result.Add( owner );
            return result.ToList( );
        }

        private List<long> GetSmallOwners( )
        {
            HashSet<long> result = new HashSet<long>( );
            foreach ( long owner in _grids.SelectMany( grid => grid.SmallOwners ).Where( x => x > 0 ) )
                result.Add( owner );
            return result.ToList( );
        }

        private HashSet<MySlimBlock> GetCubeBlocks( )
        {
            HashSet<MySlimBlock> result = new HashSet<MySlimBlock>( );

            foreach ( MyCubeGrid grid in _grids )
            {
                foreach ( MySlimBlock block in grid.CubeBlocks )
                    result.Add( block );
            }
            return result;
        }
    }
}
