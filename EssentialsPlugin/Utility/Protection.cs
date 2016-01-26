namespace EssentialsPlugin.Utility
{
    using System;
    using EssentialsPlugin.Settings;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.ModAPI;
    using Sandbox.Game.Weapons;
    using SEModAPIInternal.API.Common;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
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
                    //TODO: figure out how to get owner of a grinder
                    /*
                    if ( info.Type == MyDamageType.Grind || info.Type == MyDamageType.Weld )
                    {
                        
                        IMyEntity weapon;
                        if ( !MyAPIGateway.Entities.TryGetEntityById( info.AttackerId, out weapon ) )
                            return;

                        
                        Wrapper.GameAction( ( ) =>
                         {
                             if(weapon.GetObjectBuilder() == MyObjectBuilder_AngleGrinderDefinition )
                            {
                                var grinder = (MyObjectBuilder_AngleGrinderDefinition)weapon;
                                grinder.own
                         } );


                        if ( PlayerManager.Instance.IsUserAdmin( steamId ) || grid.BigOwners.Contains( info.AttackerId ) )
                            return;
                    }
                    //grid owners and admins can grind or weld protected grids
                    */

                    //else
                    //{
                        info.Amount = 0;
                        if ( DateTime.Now - _lastLog > TimeSpan.FromSeconds( 1 ) )
                        {
                            _lastLog = DateTime.Now;
                            Essentials.Log.Info( "Protected entity {0}.", grid.DisplayName );
                            //Essentials.Log.Info( "Protected entity \"{0}\" from player \"{1}\".", grid.DisplayName, PlayerMap.Instance.GetFastPlayerNameFromSteamId( steamId ) );
                        }
                    //}
                }
            }
        }
    }
}

