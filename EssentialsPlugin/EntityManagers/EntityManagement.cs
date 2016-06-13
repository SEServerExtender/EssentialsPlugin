namespace EssentialsPlugin.EntityManagers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Remoting;
    using System.Threading;
    using EssentialsPlugin.ProcessHandlers;
    using EssentialsPlugin.Utility;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Ingame;
    using SEModAPIInternal.API.Common;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;
    using IMyProductionBlock = Sandbox.ModAPI.Ingame.IMyProductionBlock;
    using Sandbox.Game.Entities;
    using Sandbox.Game.World;
    using Sandbox.Game.Entities.Blocks;
    using Sandbox.Game.Entities.Cube;
    using SpaceEngineers.Game.ModAPI.Ingame;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    
    public class EntityManagement
    {
        private static volatile bool _checkReveal;
        private static volatile bool _checkConceal;
        private static bool _oldInit;
        private static readonly List<long> RemovedGrids = new List<long>( );
        private static readonly List<ulong> Online = new List<ulong>( );

        public static HashSet<MyEntity> UnregisteredEntities = new HashSet<MyEntity>();


        public static void CheckAndConcealEntities( )
		{
			if ( _checkConceal )
				return;

			_checkConceal = true;
			try
			{
			    List<MyPlayer> players;

				try
				{
				    players = MySession.Static.Players.GetOnlinePlayers().ToList();
				}
				catch ( Exception ex )
				{
					Essentials.Log.Error( ex, "Error getting players list.  Check and Conceal failed: {0}");
					return;
				}
                
                foreach ( GridGroup group in GridGroup.GetAllGroups( GridLinkTypeEnum.Logical ) )
			    {
                    //we're using grid groups so that multi-part pirate ships don't lose pieces
                    if(!PluginSettings.Instance.DynamicConcealPirates)
                    {
                        if ( group.Parent.GetOwner() == "Space Pirates" )
                        {
                            if (PluginSettings.Instance.DynamicShowMessages)
                                Essentials.Log.Info( $"Not concealing pirate owned grid {group.Parent.EntityId} -> {group.Parent.DisplayName}.");
                            continue;
                        }
                    }
			        foreach ( MyCubeGrid grid in group.Grids )
			        {
			            if ( grid.Physics == null ) //projection
			                continue;

			            if ( UnregisteredEntities.Contains( grid ) )
			                continue;

			            if ( grid.GridSizeEnum != MyCubeSize.Small )
			            {
			                if ( grid.IsStatic && !PluginSettings.Instance.ConcealIncludeStations )
			                    continue;
			                if ( !PluginSettings.Instance.ConcealIncludeLargeGrids )
                                continue;
                        }

			            if ( players.Any( x => Vector3D.Distance( x.GetPosition(), grid.PositionComp.GetPosition() ) < PluginSettings.Instance.DynamicConcealDistance ) )
                            continue;

                        if ( ProcessDockingZone.ZoneCache.Any( x => Vector3D.Distance( x.GetPosition(), grid.PositionComp.GetPosition() ) < 100 ) )
                            continue;

			            if ( CheckConcealBlockRules( grid ) )
			                continue;

			            ConcealEntity( grid );
			        }
                }
			}
			catch ( Exception ex )
			{
				Essentials.Log.Error( ex );
			}
			finally
			{
				_checkConceal = false;
			}
		}

		private static bool CheckConcealBlockRules( MyCubeGrid grid )
		{
			var blocks = grid.GetFatBlocks();

			int beaconCount = 0;

		    var players = MySession.Static.Players.GetOnlinePlayers();

			foreach ( MyCubeBlock cubeBlock in blocks )
			{
			    var beacon = cubeBlock as IMyBeacon;
			    if ( beacon != null)
				{
					//beaconCount++;
					// Keep this return here, as 4 beacons always means true
                    //DON'T TELL ME WHAT TO DO
					//if ( beaconCount >= 4 )
					//{
					//	return true;
					//}

				    if ( !beacon.Enabled )
				        continue;

				    if ( players.Any( x => Vector3D.Distance( x.GetPosition(), beacon.GetPosition() ) < beacon.Radius ) )
				        return true;
				}

			    var antenna = cubeBlock as IMyRadioAntenna;
				if ( antenna != null )
				{
				    if ( !antenna.Enabled )
				        continue;

				    if ( players.Any( x => Vector3D.Distance( x.GetPosition(), antenna.GetPosition() ) < antenna.Radius ) )
				        return true;
				}

			    var medical = cubeBlock as IMyMedicalRoom;
				if ( medical != null )
				{
					if ( !medical.Enabled )
						continue;
                    
					if ( PluginSettings.Instance.DynamicConcealIncludeMedBays )
					{
						lock ( Online )
						{
							foreach ( ulong connectedPlayer in Online )
							{
								long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId( connectedPlayer );

                                if (medical.HasPlayerAccess(playerId))
									return true;
						    }
						}
					}
					else
					{
						return true;
					}
				}
                
			    var cryo = cubeBlock as MyCryoChamber;
			    if ( cryo?.Pilot != null )
			        return true;

			    var production = cubeBlock as IMyProductionBlock;
			    if ( production != null && PluginSettings.Instance.DynamicConcealProduction )
			    {
					if ( !production.Enabled )
						continue;

					if ( !production.IsQueueEmpty )
						return true;
				}

				if ( PluginSettings.Instance.DynamicConcealIgnoreSubTypeList.Any( subType => cubeBlock.BlockDefinition.Id.SubtypeName.Contains( subType ) ) )
				{
				    return true;
				}
			}

			return false;
		}
        
		private static void ConcealEntity( MyEntity entity )
		{
			int pos = 0;
            try
			{
                pos = 1;
			    long ownerId = 0;
			    if ( ( (MyCubeGrid)entity ).BigOwners.Count > 0 )
			        ownerId = ( (MyCubeGrid)entity ).BigOwners[0];

				string ownerName = PlayerMap.Instance.GetPlayerNameFromPlayerId( ownerId );
                
			    if ( PluginSettings.Instance.DynamicShowMessages )
			        Essentials.Log.Info($"{entity.EntityId} {ownerId} {ownerName}" );

				pos = 2;
                if ( entity.Physics != null )
				{
					entity.Physics.LinearVelocity = Vector3.Zero;
					entity.Physics.AngularVelocity = Vector3.Zero;
				}

				//pos = 3;
                //if ( UnregisteredEntities.Contains( entity ) )
				//{
				//	Essentials.Log.Info( $"Concealing - Id: {entity.EntityId} DUPE FOUND - Display: {entity.DisplayName} OwnerId: {ownerId} OwnerName: {ownerName}");
				//    Wrapper.GameAction( entity.Close );
                //}
                pos = 4;
				//else
                //{
                    UnregisteredEntities.Add( entity );
                    Wrapper.GameAction( () => UnregisterHierarchy( entity ) );
                    if ( PluginSettings.Instance.DynamicShowMessages )
                        Essentials.Log.Info( $"Concealed - Id: {entity.EntityId} -> Display: {entity.DisplayName} OwnerId: {ownerId} OwnerName: {ownerName}" );

                //}
			}
			catch ( Exception ex )
			{
				Essentials.Log.Error( ex, $"Failure while concealing entity {pos}." );
			}
		}

        [Obsolete]
        public static void CheckAndRevealEntitiesObsolete()
        {

            if (_checkReveal)
                return;

            _checkReveal = true;
            try
            {
                DateTime start = DateTime.Now;
                double br = 0f;
                double re = 0f;

                List<IMyPlayer> players = new List<IMyPlayer>();
                HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
                MyAPIGateway.Players.GetPlayers(players);
                Wrapper.GameAction(() =>
                {
                    MyAPIGateway.Entities.GetEntities(entities);
                });

                Dictionary<IMyEntity, string> entitiesToReveal = new Dictionary<IMyEntity, string>();
                //HashSet<IMyEntity> entitiesToReveal = new HashSet<IMyEntity>();
                foreach (IMyEntity entity in entities)
                {
                    if (entity.MarkedForClose)
                        continue;

                    if (!(entity is IMyCubeGrid))
                        continue;

                    if (entity.InScene)
                        continue;

                    Wrapper.GameAction( () => RevealEntityObsolete( new KeyValuePair<IMyEntity, string>( entity, "Obsolete Reveal" ) ) );
                }

                DateTime reStart = DateTime.Now;
                re += (DateTime.Now - reStart).TotalMilliseconds;

                if ((DateTime.Now - start).TotalMilliseconds > 2000 && PluginSettings.Instance.DynamicShowMessages)
                    Essentials.Log.Info("Completed Reveal Check: {0}ms (br: {1}ms, re: {2}ms)", (DateTime.Now - start).TotalMilliseconds, br, re);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.StartsWith("Collection was modified"))
                    Essentials.Log.Trace(ex);
            }
            catch (Exception ex)
            {
                Essentials.Log.Error(ex);
            }
            finally
            {
                _checkReveal = false;
            }
        }

        public static void CheckAndRevealEntities( )
		{
            if ( !_oldInit )
            {
                _oldInit = true;
                //TODO: Remove this eventually
                //run the old reveal code in case there are any grids still concealed by the old stuff
                CheckAndRevealEntitiesObsolete();
                return;
            }

			if ( _checkReveal )
				return;

			_checkReveal = true;
			try
			{
				foreach ( MyEntity entity in UnregisteredEntities)
				{
					if ( entity.Closed || entity.MarkedForClose )
						continue;

					if ( !( entity is MyCubeGrid ) )
						continue;
                    
					MyCubeGrid grid = (MyCubeGrid)entity;
					bool found = false;
					string currentReason = string.Empty;
					foreach ( MyPlayer player in MySession.Static.Players.GetOnlinePlayers() )
					{
						double distance;
						if ( Entity.GetDistanceBetweenGridAndPlayer( grid, player, out distance ) )
						{
							if ( distance < PluginSettings.Instance.DynamicConcealDistance )
							{
								found = true;
								currentReason = $"{player.DisplayName} distance to grid: {distance}";
							}
						}
					}

					if ( !found )
					{
						if ( CheckRevealBlockRules( grid, out currentReason ) )
						{
							found = true;
						}
					}

					if ( found )
					{
					    Wrapper.GameAction( () => ReregisterHierarchy( entity ) );

                        if (PluginSettings.Instance.DynamicShowMessages)
                            Essentials.Log.Info("Revealed - Id: {0} -> Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}",
                                             entity.EntityId,
                                             entity.DisplayName.Replace("\r", "").Replace("\n", ""),
                                             grid.BigOwners.FirstOrDefault(),
                                             PlayerMap.Instance.GetPlayerNameFromPlayerId(grid.BigOwners.FirstOrDefault()),
                                             currentReason);
                    }
				}
			}
			catch ( InvalidOperationException ex )
			{
				if ( ex.Message.StartsWith( "Collection was modified" ) )
					Essentials.Log.Trace( ex );
			}
			catch ( Exception ex )
			{
				Essentials.Log.Error( ex );
			}
			finally
			{
				_checkReveal = false;
			}
		}

		private static bool CheckRevealBlockRules( MyCubeGrid grid, out string reason )
		{
			reason = "";

		    var players = MySession.Static.Players.GetOnlinePlayers();

			foreach ( var cubeBlock in grid.GetFatBlocks() )
			{
			    var beacon = cubeBlock as IMyBeacon;
				if ( beacon != null )
				{
					if ( !beacon.Enabled )
						continue;

				    foreach ( var player in players )
				    {
				        var distance = Vector3D.Distance( player.GetPosition(), beacon.GetPosition() );

                        if ( distance < beacon.Radius )
				        {
				            reason = $"{player.DisplayName} distance to beacon broadcast: {distance}";
				            return true;
				        }
				    }
				}

			    var antenna = cubeBlock as IMyRadioAntenna;
				if ( antenna != null )
				{
					if ( !antenna.Enabled )
						continue;
                    
				    foreach ( var player in players )
				    {
				        var distance = Vector3D.Distance( player.GetPosition(), antenna.GetPosition() );

				        if ( distance < antenna.Radius )
				        {
				            reason = $"{player.DisplayName} distance to antenna broadcast: {distance}";
				            return true;
				        }
				    }
				}

			    var medical = cubeBlock as IMyMedicalRoom;
				if ( medical != null )
				{
					if ( !medical.Enabled || !medical.IsFunctional )
				        continue;
                    
					if ( PluginSettings.Instance.DynamicConcealIncludeMedBays )
					{
						lock ( Online )
						{
							foreach ( ulong connectedPlayer in Online )
							{
								long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId( connectedPlayer );

                                if ( medical.OwnerId == playerId )
								{
									reason = $"Grid has medbay and player is logged in - playerid: {playerId}";
                                    return true;
                                }

								if ( medical.HasPlayerAccess( playerId ) )
								{
									reason = $"Grid has medbay and player has access - playerid: {playerId}";
                                    return true;
                                }
							}
						}
                    }
					else
					{
						reason = "Grid has medbay and conceal can not include medbays";
                        return true;
                    }
                }

			    var cryo = cubeBlock as MyCryoChamber;
			    if ( cryo != null )
			    {
			        if ( cryo.Pilot == null )
			            continue;

			        if ( !cryo.IsFunctional )
			            continue;

			        reason = $"Grid has cryopod and player is inside - player: {cryo.Pilot.DisplayName}";
			        return true;
			    }

                //don't check conceal settings for production blocks, we always want them to reveal
			    var production = cubeBlock as MyProductionBlock;
			    if ( production != null )
			    {
			        if ( !production.Enabled )
			            continue;

			        if ( !production.IsQueueEmpty )
			        {
			            reason = "Grid has production facility that has a queue";
			            return true;
			        }
			    }
			}

			return false;
		}
        
        [Obsolete]
        public static void RevealEntityObsolete( KeyValuePair<IMyEntity, string> item )
		{
			IMyEntity entity = item.Key;
			string reason = item.Value;
			//Wrapper.GameAction(() =>
			//{
			MyObjectBuilder_CubeGrid builder = CubeGrids.SafeGetObjectBuilder( (IMyCubeGrid)entity );
			if ( builder == null )
				return;

            if ( entity.InScene )
                return;

			IMyCubeGrid grid = (IMyCubeGrid)entity;
			long ownerId = 0;
			string ownerName = string.Empty;
			if ( CubeGrids.GetBigOwners( builder ).Count > 0 )
			{
				ownerId = CubeGrids.GetBigOwners( builder ).First( );
				ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId( ownerId ).Name;
			}
			/*
			entity.InScene = true;
			entity.CastShadows = true;
			entity.Visible = true;
			*/

			builder.PersistentFlags = ( MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.CastShadows );
			MyAPIGateway.Entities.RemapObjectBuilder( builder );
			//builder.EntityId = 0;

			if ( RemovedGrids.Contains( entity.EntityId ) )
			{
				Essentials.Log.Info( "Revealing - Id: {0} DUPE FOUND Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}",
									 entity.EntityId,
									 entity.DisplayName.Replace( "\r", "" ).Replace( "\n", "" ),
									 ownerId,
									 ownerName,
									 reason );
			    entity.Close( );
			    //MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( entity ) );
			}
			else
			{
					IMyEntity newEntity = MyAPIGateway.Entities.CreateFromObjectBuilder( builder );
					if ( newEntity == null )
					{
						Essentials.Log.Warn( "CreateFromObjectBuilder failed: {0}", builder.EntityId );
						return;
					}

				    newEntity.Physics.Enabled = false;

					RemovedGrids.Add( entity.EntityId );
				    entity.Physics.Enabled = false;
				    entity.Close( );

                    Thread.Sleep( 50 );
				    newEntity.Physics.Enabled = true;
                    newEntity.InScene = true;
                    MyAPIGateway.Entities.AddEntity( newEntity );
                    //MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( newEntity ) );
                    newEntity.Physics.LinearVelocity = Vector3.Zero;
                    newEntity.Physics.AngularVelocity = Vector3.Zero;

                    if ( PluginSettings.Instance.DynamicShowMessages )
						Essentials.Log.Info( "Revealed - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {5}",
										 entity.EntityId,
										 entity.DisplayName.Replace( "\r", "" ).Replace( "\n", "" ),
										 ownerId,
										 ownerName,
										 newEntity.EntityId,
										 reason );
				
			}
			//});
		}

		static public void RevealAll( )
		{
		    while ( _checkReveal )
		    {
		    }

		    _checkReveal = true;

            CheckAndRevealEntitiesObsolete();

		    foreach ( var entity in UnregisteredEntities.ToArray() )
		    {
                if (PluginSettings.Instance.DynamicShowMessages)
                    Essentials.Log.Info("Revealed - Id: {0} -> Display: {1} OwnerId: {2} OwnerName: {3}  Reason: {4}",
                                     entity.EntityId,
                                     entity.DisplayName.Replace("\r", "").Replace("\n", ""),
                                     ((MyCubeGrid)entity).BigOwners.FirstOrDefault(),
                                     PlayerMap.Instance.GetPlayerNameFromPlayerId(((MyCubeGrid)entity).BigOwners.FirstOrDefault()),
                                     "Force reveal");

		        Wrapper.GameAction( () => ReregisterHierarchy( entity ) );
		    }

		    _checkReveal = false;
		}

	
		public static void SetOnline( ulong steamId, bool online )
		{
			lock ( Online )
			{
				if ( online )
				{
					if ( !Online.Contains( steamId ) )
					{
						Online.Add( steamId );
					}
				}
				else
				{
					if ( Online.Contains( steamId ) )
					{
						Online.Remove( steamId );
					}
				}
			}
		}

        private static void UnregisterHierarchy( MyEntity entity )
        {
            if ( entity.Hierarchy == null )
                return;

            //if ( UnregisteredEntities.Contains( entity ) )
            //    return;

            foreach ( var child in entity.Hierarchy.Children )
            {
                MyEntity childEntity = (MyEntity)child.Container.Entity;
                UnregisterHierarchy( childEntity );
                MyEntities.UnregisterForUpdate( childEntity );
                childEntity.RemoveFromGamePruningStructure();

                //child.Container.Entity.InScene = false;

                if ( !PluginSettings.Instance.DynamicConcealPhysics )
                    continue;

                if (child.Container.Entity.Physics != null)
                    child.Container.Entity.Physics.Enabled = false;
            }

            UnregisteredEntities.Add( entity );
        }

        private static void ReregisterHierarchy( MyEntity entity )
        {
            if ( entity.Hierarchy == null )
                return;
            
            foreach (var child in entity.Hierarchy.Children)
            {
                MyEntity childEntity = (MyEntity)child.Container.Entity;
                ReregisterHierarchy(childEntity);
                MyEntities.RegisterForUpdate(childEntity);
                childEntity.AddToGamePruningStructure();

                //child.Container.Entity.InScene = true;

                if (!PluginSettings.Instance.DynamicConcealPhysics)
                    continue;

                if (child.Container.Entity.Physics != null)
                    child.Container.Entity.Physics.Enabled = true;
            }

            UnregisteredEntities.Remove( entity );
        }
    }
}