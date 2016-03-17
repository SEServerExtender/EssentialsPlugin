namespace EssentialsPlugin.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using EssentialsPlugin.Settings;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using Sandbox.ModAPI;
    using Sandbox.Game.Weapons;
    using Sandbox.ModAPI.Interfaces;
    using SEModAPIInternal.API.Common;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.Game.ModAPI.Interfaces;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRage.Utils;

    public static class Protection
    {
        private static bool _init = false;
        private static DateTime _lastLog;
        private static SortedList<IMySlimBlock, float> toDamage = new SortedList<IMySlimBlock, float>( );

        public static void Init( )
        {
            if ( _init )
                return;

            _init = true;
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler( 0, DamageHandler );
        }

        private static void DamageHandler( object target, ref MyDamageInformation info )
        {
            if ( !PluginSettings.Instance.ProtectedEnabled )
                return;

            IMySlimBlock block = target as IMySlimBlock;
            if ( block == null )
                return;
            
            IMyCubeGrid grid = block.CubeGrid;
            
            ulong steamId = PlayerMap.Instance.GetSteamId( info.AttackerId );
            
            foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
            {
                if ( item.Enabled && item.EntityId == grid.EntityId )
                {
                    info.Amount = 0;
                    if ( DateTime.Now - _lastLog > TimeSpan.FromSeconds( 1 ) )
                    {
                        _lastLog = DateTime.Now;
                        Essentials.Log.Info( "Protected entity {0}.", grid.DisplayName );
                    }
                }
            }

        }
    }
}

