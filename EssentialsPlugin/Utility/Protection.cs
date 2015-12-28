namespace EssentialsPlugin.Utility
{
    using System;
    using EssentialsPlugin.Settings;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;

    public class Protection
    {
        private static bool _init = false;
        private static DateTime _lastLog;

        public static void Init( )
        {
            if ( _init )
                return;

                _init = true;
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler( 0, DamageHandler );
        }

        public static void DamageHandler( object target, ref MyDamageInformation info )
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
                    if ( info.Type == MyDamageType.Grind || info.Type == MyDamageType.Weld )
                    {
                        if ( PlayerManager.Instance.IsUserAdmin( steamId ) || grid.BigOwners.Contains( info.AttackerId ) )
                            return;
                    }
                    //grid owners and admins can grind or weld protected grids

                    else
                    {
                        info.Amount = 0;
                        if ( DateTime.Now - _lastLog > TimeSpan.FromSeconds( 1 ) )
                        {
                            _lastLog = DateTime.Now;
                            Essentials.Log.Info( "Protected entity \"{0}\" from player \"{1}\".", grid.DisplayName, PlayerMap.Instance.GetFastPlayerNameFromSteamId( steamId ) );
                        }
                    }
                }
            }
        }
    }
}

