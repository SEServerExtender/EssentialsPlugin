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
            RegisterGridHandlers();
        }

        public void RegisterGridHandlers()
        {
            foreach ( var item in PluginSettings.Instance.ProtectedItems )
            {
                var grid = MyEntities.GetEntityById( item.EntityId ) as MyCubeGrid;
                if ( grid == null )
                {
                    Essentials.Log.Error( $"Error getting entity in Protection.RegisterGridHandlers. ID: {item.EntityId}" );
                    continue;
                }
                
                grid.OnBlockAdded -= OnBlockAdded;
                grid.OnBlockAdded += OnBlockAdded;
            }
        }

        private void OnBlockAdded( MySlimBlock block )
        {
            var protectionItem = PluginSettings.Instance.ProtectedItems.FirstOrDefault( x => x.EntityId == block.CubeGrid.EntityId );
            if ( protectionItem == null || !protectionItem.Enabled || !protectionItem.ProtectBockAdd )
                return;

            if ( block.OwnerId != 0 )
            {
                var steamId = PlayerMap.Instance.GetSteamIdFromPlayerId( block.OwnerId );
                //if ( PlayerManager.Instance.IsUserAdmin( steamId ) )
                //    return;

                Communication.Notification( steamId, MyFontEnum.Red, 5, protectionItem.ProtectBlockWarning ?? "You cannot add blocks to this grid!" );
            }
            MyAPIGateway.Utilities.InvokeOnGameThread(()=>block.CubeGrid.RazeBlock( block.Position ));
            Essentials.Log.Info( $"Removed block from protected grid {protectionItem.EntityId}. Block owner: {PlayerMap.Instance.GetPlayerNameFromPlayerId( block.OwnerId )}" );
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

