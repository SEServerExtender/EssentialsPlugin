namespace EssentialsPlugin.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EssentialsPlugin.Settings;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using Sandbox.Game.World;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using VRage.Game;
    using VRage.Game.Entity.EntityComponents;
    using VRage.Game.ModAPI;

    public class Protection
    {
        private static Protection _instance;
        private bool _init = false;
        private DateTime _lastLog;

        public static Protection Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new Protection();
                return _instance;
            }
        }

        public void Init( )
        {
            if ( _init )
                return;

            _init = true;
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler( 0, DamageHandler );
        }

        private void DamageHandler( object target, ref MyDamageInformation info )
        {
            if ( !PluginSettings.Instance.ProtectedEnabled )
                return;

            MySlimBlock block = target as MySlimBlock;
            if ( block == null )
                return;
            
            MyCubeGrid grid = block.CubeGrid;
            
            foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
            {
                if ( !item.Enabled || !item.ProtectDamage || item.EntityId != grid.EntityId )
                    continue;

                if ( DateTime.Now - _lastLog > TimeSpan.FromSeconds( 1 ) )
                {
                    _lastLog = DateTime.Now;
                    Essentials.Log.Info( $"Protected entity {grid.DisplayName}:{grid.EntityId}." );
                }
                    if ( !item.LogOnly )
                        info.Amount = 0;
            }
        }
        
        public bool CheckPlayerExempt(ProtectedItem.ProtectionSettings settings, MyCubeGrid grid, ulong remoteUserId )
        {
            if ( settings.AllExempt )
                return true;
            
            if (settings.AdminExempt && PlayerManager.Instance.IsUserAdmin(remoteUserId))
                return true;
            
            long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId( remoteUserId );

            if (settings.BigOwnerExempt)
            {
                //fast check to see if the player's current live identity is an owner
                if (grid.BigOwners.Contains(playerId))
                    return true;

                //check old, dead identities. this is much slower
                var playerIds = PlayerMap.Instance.GetPlayerIdsFromSteamId(remoteUserId, false);
                
                foreach (var owner in grid.BigOwners)
                    if (playerIds.Contains(owner))
                        return true;
            }

            if ( settings.SmallOwnerExempt )
            {
                //fast check to see if the player's current live identity is an owner
                if ( grid.SmallOwners.Contains( playerId ) )
                    return true;

                //check old, dead identities. this is much slower
                var playerIds = PlayerMap.Instance.GetPlayerIdsFromSteamId( remoteUserId, false );
                
                foreach ( var owner in grid.SmallOwners )
                    if ( playerIds.Contains( owner ) )
                        return true;
            }
            
            if ( settings.FactionExempt && grid.BigOwners.Count > 0)
            {
                var fac = MySession.Static.Factions.GetPlayerFaction( grid.BigOwners[0] );
                if ( fac != null && fac.IsMember( playerId ) )
                    return true;
            }

            if (settings.Factions == null)
                return false;

            foreach ( var facId in settings.Factions )
            {
                var fac = MySession.Static.Factions.TryGetFactionById( facId );
                if ( fac != null && fac.IsMember( playerId ) )
                    return true;
            }
            
            //if ( settings.ExemptSteamIds.Contains( remoteUserId.ToString() ) )
            //    return true;
                
            return false;
        }
    }
}

