using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssentialsPlugin.Editors
{
    using Sandbox.Game.Entities;
    using Sandbox.Game.World;
    using VRage.Game.Entity;

    public class GridListItem
    {
        public GridListItem( MyCubeGrid grid )
        {
            Grid = grid;
        }

        public MyCubeGrid Grid;
        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString( )
        {
            return $"{Grid.DisplayName ?? ""}: {Grid.EntityId}";
        }
    }

    public class FactionListItem
    {
        public FactionListItem(MyFaction faction)
        {
            Faction = faction;
        }

        public MyFaction Faction;

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString( )
        {
            return $"{Faction.Tag??"ERROR"}: {Faction.Name??Faction.FactionId.ToString(  )}";
        }
    }
}
