using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using EssentialsPlugin.Settings;
using SEModAPIInternal.API.Common;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace EssentialsPlugin.Utility
{
    public static class PlayerBlockEnforcement
    {
        private static bool _init;
        public static Dictionary<MyTerminalBlock, long> BlockOwners = new Dictionary<MyTerminalBlock, long>();
        private static Dictionary<ulong, HashSet<string>> disabledPlayers = new Dictionary<ulong, HashSet<string>>();
        private static DateTime _lastRun = DateTime.MinValue;

        public static void Init()
        {
            if ( !PluginSettings.Instance.PlayerBlockEnforcementEnabled )
                return;

            if (_init)
                return;

            _init = true;

            MyEntities.OnEntityAdd += MyEntities_OnEntityAdd;

            HashSet<MyEntity> allEntities = new HashSet<MyEntity>();
            Wrapper.GameAction( () => allEntities = MyEntities.GetEntities() );

            MyEntity[] entitiesCopy = new MyEntity[allEntities.Count];
            allEntities.CopyTo( entitiesCopy );

            Parallel.ForEach( entitiesCopy, ( entity ) =>
                                            {
                                                var grid = entity as MyCubeGrid;
                                                if ( grid == null )
                                                    return;

                                                InitGrid( grid );
                                            } );
            Essentials.Log.Info( "Initialized player block enforcement." );
            ProcessEnforcement();
        }

        private static void InitGrid( MyCubeGrid grid )
        {
            Essentials.Log.Debug( $"intializing on grid {grid.DisplayName}" );
            grid.OnBlockAdded += Grid_OnBlockAdded;
            grid.OnBlockRemoved += Grid_OnBlockRemoved;
            grid.OnMarkForClose += Grid_OnMarkForClose;

            foreach (var item in PluginSettings.Instance.PlayerBlockEnforcementItems)
            {
                switch ( item.Mode )
                {
                    case SettingsBlockEnforcementItem.EnforcementMode.Off:
                        continue;

                    case SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId:
                        foreach (var block in grid.CubeBlocks)
                        {
                            var fatBlock = block?.FatBlock as MyTerminalBlock;
                            if (fatBlock == null || fatBlock.Closed)
                                continue;

                            if ( !fatBlock.BlockDefinition.Id.SubtypeName.Contains( item.BlockSubtypeId ) )
                                continue;

                            fatBlock.OwnershipChanged += FatBlock_OwnershipChanged;

                            if (fatBlock.OwnerId == 0)
                            {
                                if (fatBlock.CubeGrid.BigOwners.Count > 0 && PluginSettings.Instance.PlayerBlockEnforcementChangeOwner)
                                    MyAPIGateway.Utilities.InvokeOnGameThread(() => fatBlock.ChangeBlockOwnerRequest(fatBlock.CubeGrid.BigOwners.First(), MyOwnershipShareModeEnum.Faction));
                                else if (PluginSettings.Instance.PlayerBlockEnforcementChangeOwner)
                                    ChangeOwnershipToNearestPlayer(fatBlock);
                            }

                            if (fatBlock.OwnerId != 0)
                                lock (BlockOwners)
                                    BlockOwners[fatBlock] = fatBlock.OwnerId;
                        }
                        break;

                    case SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId:
                        foreach (var block in grid.CubeBlocks)
                        {
                            var fatBlock = block?.FatBlock as MyTerminalBlock;
                            if (fatBlock == null || fatBlock.Closed)
                                continue;

                            if ( !fatBlock.BlockDefinition.Id.TypeId.ToString().Contains( item.BlockTypeId ) )
                                continue;

                            fatBlock.OwnershipChanged += FatBlock_OwnershipChanged;

                            if (fatBlock.OwnerId == 0)
                            {
                                if (fatBlock.CubeGrid.BigOwners.Count > 0 && PluginSettings.Instance.PlayerBlockEnforcementChangeOwner)
                                    MyAPIGateway.Utilities.InvokeOnGameThread(() => fatBlock.ChangeBlockOwnerRequest(fatBlock.CubeGrid.BigOwners.First(), MyOwnershipShareModeEnum.Faction));
                                else if (PluginSettings.Instance.PlayerBlockEnforcementChangeOwner)
                                    ChangeOwnershipToNearestPlayer(fatBlock);
                            }

                            if (fatBlock.OwnerId != 0)
                                lock (BlockOwners)
                                    BlockOwners[fatBlock] = fatBlock.OwnerId;
                        }
                        break;
                }
            }
        }

        private static void ChangeOwnershipToNearestPlayer( MyTerminalBlock block )
        {
            Wrapper.GameAction( () =>
                                {
                                    try
                                    {
                                        double minDist = 0;
                                        MyPlayer nearest = null;

                                        foreach (var player in MySession.Static.Players.GetOnlinePlayers( ))
                                        {
                                            var dist = Vector3D.DistanceSquared( player.GetPosition( ), block.PositionComp.GetPosition( ) );

                                            if (nearest == null)
                                            {
                                                nearest = player;
                                                minDist = dist;
                                            }
                                            else if (dist < minDist)
                                                nearest = player;
                                        }

                                        if (nearest == null)
                                            return;
                                        MyAPIGateway.Utilities.InvokeOnGameThread( ( ) => block.ChangeBlockOwnerRequest( nearest.Identity.IdentityId, MyOwnershipShareModeEnum.Faction ) );
                                    }
                                    catch (Exception ex)
                                    {
                                        Essentials.Log.Error( ex );
                                    }
                                } );
        }

        private static bool _processing;
        private static void ProcessEnforcement( MyCubeBlock newBlock = null)
        {
            if ( !PluginSettings.Instance.PlayerBlockEnforcementEnabled )
                return;

            if ( _processing )
                return;

            _processing = true;
            
            if ( DateTime.Now - _lastRun < TimeSpan.FromMilliseconds( 100 ) )
            {
                _processing = false;
                return;
            }
            
            _lastRun = DateTime.Now;
            
            Task.Run( () =>
                      {
                          //Essentials.Log.Debug( "process enforcement" );
                          foreach ( var item in PluginSettings.Instance.PlayerBlockEnforcementItems )
                          {
                              string blockSearch;
                              bool searchSubType;
                              switch ( item.Mode )
                              {
                                  case SettingsBlockEnforcementItem.EnforcementMode.Off:
                                      continue;

                                  case SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId:
                                      blockSearch = item.BlockSubtypeId;
                                      searchSubType = true;
                                      break;

                                  case SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId:
                                      blockSearch = item.BlockTypeId;
                                      searchSubType = false;
                                      break;

                                  default:
                                      Essentials.Log.Debug( "exit" );
                                      return;
                              }

                              if ( item.MaxPerGrid == 0 )
                              {
                                  foreach ( var player in MySession.Static.Players.GetOnlinePlayers() )
                                  {
                                      if ( item.AdminExempt && player.IsAdmin )
                                          continue;

                                      Communication.DisableGMenu( player.Client.SteamUserId, blockSearch, searchSubType, false );
                                      disabledPlayers[player.Client.SteamUserId].Add( blockSearch );
                                  }
                              }

                              var ownedBlocks = new Dictionary<long, int>();
                              var entities = new HashSet<MyEntity>();
                              var razeBlocks = new HashSet<MyCubeBlock>();
                              Wrapper.GameAction( () => entities = MyEntities.GetEntities() );
                              MyEntity[] entitiesCopy = new MyEntity[entities.Count];
                              entities.CopyTo( entitiesCopy );
                              
                              foreach ( var entity in entitiesCopy )
                              {
                                  var grid = entity as MyCubeGrid;
                                  if ( grid?.Physics == null || grid.Closed )
                                      continue;
                                  
                                  foreach ( var cubeBlock in grid.GetFatBlocks() )
                                  {
                                      if ( cubeBlock.Closed )
                                          continue;

                                      if ( ( searchSubType && cubeBlock.BlockDefinition.Id.SubtypeId.ToString().Contains( blockSearch ) )
                                           || ( !searchSubType && cubeBlock.BlockDefinition.Id.TypeId.ToString().Contains( blockSearch ) ) )
                                      {
                                          //Essentials.Log.Debug( $"found block: {cubeBlock.BlockDefinition.Id.SubtypeName}" );

                                          if ( MySession.Static.Players.IdentityIsNpc( cubeBlock.OwnerId ) )
                                          {
                                              //Essentials.Log.Debug( "not processing NPC owned block" );
                                              continue;
                                          }

                                          if ( ownedBlocks.ContainsKey( cubeBlock.OwnerId ) )
                                              ownedBlocks[cubeBlock.OwnerId]++;
                                          else
                                              ownedBlocks.Add( cubeBlock.OwnerId, 1 );
                                          
                                          foreach ( var entry in ownedBlocks )
                                          {
                                              if ( entry.Key != cubeBlock.OwnerId )
                                                  continue;

                                              if ( entry.Value > item.MaxPerGrid )
                                              {
                                                  if ( newBlock != null && newBlock.GetType() == cubeBlock.GetType() && !razeBlocks.Contains( newBlock ) )
                                                      razeBlocks.Add( newBlock );
                                                  else
                                                      razeBlocks.Add( cubeBlock );
                                              }
                                          }
                                      }
                                  }
                              }
                              
                              foreach ( var block in razeBlocks )
                              {
                                  Wrapper.GameAction( () => block.CubeGrid.RazeBlock( block.Position ) );
                                  lock ( BlockOwners )
                                      BlockOwners.Remove( (MyTerminalBlock)block );
                              }
                              
                              foreach ( var entry in ownedBlocks )
                              {
                                  var steamId = PlayerMap.Instance.GetSteamIdFromPlayerId( entry.Key );

                                  if ( item.AdminExempt && PlayerManager.Instance.IsUserAdmin( steamId ) )
                                      continue;
                                  
                                  if ( entry.Value > item.MaxPerGrid )
                                  {
                                      Communication.SendPrivateInformation( steamId, item.MaxExceedWarning );
                                      Communication.SendPrivateInformation( steamId, $"You have exceeded the max block count of {blockSearch}. {entry.Value - item.MaxPerGrid} blocks have been deleted, and the block has been removed from your G menu." );
                                      Communication.DisableGMenu( steamId, blockSearch, searchSubType, false );
                                      if ( !disabledPlayers.ContainsKey( steamId ) )
                                          disabledPlayers.Add( steamId, new HashSet<string>() );
                                      disabledPlayers[steamId].Add( blockSearch );
                                      continue;
                                  }
                                  
                                  if ( entry.Value == item.MaxPerGrid )
                                  {
                                      if ( disabledPlayers.ContainsKey( steamId ) && disabledPlayers[steamId].Contains( blockSearch ) )
                                          continue;
                                      Communication.SendPrivateInformation( steamId, item.MaxReachWarning );
                                      Communication.DisableGMenu( steamId, blockSearch, searchSubType, false );
                                      if ( !disabledPlayers.ContainsKey( steamId ) )
                                          disabledPlayers.Add( steamId, new HashSet<string>() );
                                      disabledPlayers[steamId].Add( blockSearch );
                                      continue;
                                  }
                                  
                                  if ( disabledPlayers.ContainsKey( steamId ) )
                                  {
                                      if ( disabledPlayers[steamId].Contains( blockSearch ) )
                                      {
                                          disabledPlayers[steamId].Remove( blockSearch );
                                          Communication.SendPrivateInformation( steamId, $"You have fallen below the limit for {blockSearch}, the block has been re-enabled in your G menu." );
                                          Communication.DisableGMenu( steamId, blockSearch, searchSubType, true );
                                          if ( disabledPlayers[steamId].Count == 0 )
                                              disabledPlayers.Remove( steamId );
                                      }
                                  }
                              }

                              Dictionary<ulong, string> toRemove = new Dictionary<ulong, string>();
                              foreach ( var entry in disabledPlayers )
                              {
                                  if ( !entry.Value.Contains( blockSearch ) )
                                      continue;

                                  var playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId( entry.Key );
                                  if ( ownedBlocks.ContainsKey( playerId ) )
                                      continue;
                                  
                                  toRemove.Add( entry.Key, blockSearch );
                                  Communication.SendPrivateInformation(entry.Key, $"You have fallen below the limit for {blockSearch}, the block has been re-enabled in your G menu.");
                                  Communication.DisableGMenu( entry.Key, blockSearch, searchSubType, true );  
                              }

                              foreach ( var entry in toRemove )
                              {
                                  disabledPlayers[entry.Key].Remove(blockSearch);
                                  if (disabledPlayers[entry.Key].Count == 0)
                                      disabledPlayers.Remove(entry.Key);
                              }
                          }
                      } );
            _processing = false;
        }
        

        #region Event Listeners
        //These events are raised on the game thread
        private static void MyEntities_OnEntityAdd(MyEntity obj)
        {
            var grid = obj as MyCubeGrid;
            if ( grid == null )
                return;

            Task.Run( () => InitGrid( grid ) );
        }

        private static void Grid_OnMarkForClose(MyEntity obj)
        {
            //why the hell doesn't this return the MyCubeGrid?
            var grid = obj as MyCubeGrid;
            if ( grid == null )
                return;


            Task.Run( () =>
                      {
            grid.OnBlockAdded -= Grid_OnBlockAdded;
            grid.OnBlockRemoved -= Grid_OnBlockRemoved;
            grid.OnMarkForClose -= Grid_OnMarkForClose;
                          foreach ( var block in grid.CubeBlocks )
                          {
                              var fatBlock = block?.FatBlock as MyTerminalBlock;
                              if ( fatBlock == null )
                                  continue;

                              lock ( BlockOwners )
                              {
                                  if ( BlockOwners.ContainsKey( fatBlock ) )
                                      BlockOwners.Remove( fatBlock );
                              }
                          }
                      } );
        }

        private static void Grid_OnBlockRemoved( MySlimBlock obj )
        {
            Task.Run( () =>
                      {
                          var fatBlock = obj?.FatBlock as MyTerminalBlock;
                          if ( fatBlock == null )
                              return;

                          lock ( BlockOwners )
                          {
                              if ( BlockOwners.ContainsKey( fatBlock ) )
                                  BlockOwners.Remove( fatBlock );
                          }
                      } );
            ProcessEnforcement();
        }

        private static void Grid_OnBlockAdded(MySlimBlock obj)
        {
                          var fatBlock = obj?.FatBlock as MyTerminalBlock;
                          if ( fatBlock == null )
                              return;
            Task.Run( () =>
                      {

                          if ( fatBlock.OwnerId == 0 && PluginSettings.Instance.PlayerBlockEnforcementChangeOwner )
                              ChangeOwnershipToNearestPlayer( fatBlock );

                          if ( fatBlock.OwnerId == 0 )
                              return;

                          lock ( BlockOwners )
                              BlockOwners[fatBlock] = fatBlock.OwnerId;
                      } );

                          ProcessEnforcement(fatBlock);
        }

        private static void FatBlock_OwnershipChanged(MyTerminalBlock block)
        {
            if ( block?.CubeGrid?.Physics == null || block.Closed )
            {
                //idfk
                Thread.Sleep( 500 );
                if ( block?.CubeGrid?.Physics == null || block.Closed )
                    return;
            }

            Task.Run( () =>
                      {
                          if ( block.OwnerId != 0 )
                          {
                              var owner = MySession.Static.Players.GetPlayerById( new MyPlayer.PlayerId( PlayerMap.Instance.GetSteamIdFromPlayerId( block.OwnerId ) ) );
                              if ( owner != null && MySession.Static.Players.IdentityIsNpc( block.OwnerId ) )
                              {
                                  if ( owner.DisplayName != "Space Pirates" )
                                  {
                                      lock ( BlockOwners )
                                          BlockOwners[block] = block.OwnerId;
                                      return;
                                  }
                              }
                          }

                          lock ( BlockOwners )
                          {
                              if ( BlockOwners.ContainsKey( block ) )
                                  MyAPIGateway.Utilities.InvokeOnGameThread( () => block.ChangeBlockOwnerRequest( BlockOwners[block], MyOwnershipShareModeEnum.Faction ) );
                              else if ( PluginSettings.Instance.PlayerBlockEnforcementChangeOwner )
                              {
                                  ChangeOwnershipToNearestPlayer( block );
                                  if ( block.OwnerId == 0 && block.CubeGrid.BigOwners.Count > 0 )
                                      MyAPIGateway.Utilities.InvokeOnGameThread(()=>block.ChangeBlockOwnerRequest( block.CubeGrid.BigOwners[0], MyOwnershipShareModeEnum.Faction ));

                                  if ( block.OwnerId != 0 )
                                      BlockOwners[block] = block.OwnerId;
                              }
                          }
                      } );

            ProcessEnforcement();
        }

        #endregion
    }
}
