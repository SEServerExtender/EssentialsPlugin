namespace EssentialsPlugin.Utility
{
    using System.Collections.Generic;
    using System.Linq;
    using Sandbox.Game.Entities;

    public class GridGroup
    {
        private List<MyCubeGrid> _nodes = new List<MyCubeGrid>();

        public List<MyCubeGrid> Nodes
        {
            get { return _nodes; }
            set { _nodes = value; }
        }

        public MyCubeGrid Parent()
        {
            if (_nodes.Count < 1)
                return null;

            MyCubeGrid result = null;
            foreach (MyCubeGrid grid in _nodes)
            {
                if (result == null || grid.BlocksCount > result.BlocksCount)
                    result = grid;
            }
            return result;
        }

        public void GetGroup(MyCubeGrid grid, GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical)
        {
            Wrapper.GameAction(() => _nodes = MyCubeGridGroups.Static.GetGroups(linkType).GetGroupNodes(grid));
        }

        public List<long> BigOwners()
        {
            HashSet<long> result = new HashSet<long>();
            foreach ( long owner in _nodes.SelectMany( grid => grid.BigOwners ) )
                result.Add( owner );
            return result.ToList( );
        }

        public List<long> SmallOwners( )
        {
            HashSet<long> result = new HashSet<long>();
            foreach (long owner in _nodes.SelectMany(grid => grid.SmallOwners))
                result.Add(owner);
            return result.ToList();
        } 
    }
}
