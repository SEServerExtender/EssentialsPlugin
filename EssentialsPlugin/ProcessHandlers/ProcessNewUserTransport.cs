namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using VRage.Game;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;
	using VRageMath;

	public class ProcessNewUserTransport : ProcessHandlerBase
	{
		private readonly List<ulong> _newUserList;
		private readonly Random _random;
		private bool _init;

		public ProcessNewUserTransport( )
		{
			_random = new Random( );
			_init = false;
			_newUserList = new List<ulong>( );
		}

		private void Init( )
		{
			List<IMyVoxelBase> voxels = new List<IMyVoxelBase>( );
			MyAPIGateway.Session.VoxelMaps.GetInstances( voxels );
			Essentials.Log.Info( "Current Voxel Count: {0}", voxels.Count );

			// Cache asteroids
			/*
			if (PluginSettings.Instance.NewUserTransportSpawnType == NewUserTransportSpawnPoint.Asteroids)
			{
				Log.Info(string.Format("Voxel Caching Initializing"));
				Thread thread = new Thread((Object state) =>
				{
					List<VoxelMap> voxels = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();
					Thread.Sleep(10000);
					voxels = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();

					Log.Info(string.Format("Starting Voxel Caching .. This might take awhile: {0} voxels", voxels.Count));
					int count = 0;
					foreach (VoxelMap voxel in voxels)
					{
						DateTime start = DateTime.Now;
						Log.Info(string.Format("Caching Voxel: {0}", voxel.Name));
						int voxelMaterialCount = voxel.Materials.Count;
						Log.Info(string.Format("Caching Voxel: {0} - {1} (Took: {2}s)", voxel.Name, voxelMaterialCount, (DateTime.Now - start).TotalSeconds));
						count++;
					}
					Log.Info(string.Format("Completed Voxel Caching: {0}", count));

					m_ready = true;

				});
				thread.Priority = ThreadPriority.BelowNormal;
				thread.IsBackground = true;
				thread.Start();
			}
			 */

			/*
			ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
			{				
				List<VoxelMap> voxels = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();
				Thread.Sleep(10000);
				voxels = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();

				Log.Info("Starting Voxel Caching .. This might take awhile");
				foreach (VoxelMap voxel in voxels)
				{
					DateTime start = DateTime.Now;
					int voxelMaterialCount = voxel.Materials.Count;
					Log.Info(string.Format("Caching Voxel: {0} - {1} (Took: {2}s)", voxel.Name, voxelMaterialCount, (DateTime.Now - start).TotalSeconds));
				}
				Log.Info("Completed Voxel Caching");

				m_ready = true;
			}));
			 */
		}

		public override int GetUpdateResolution( )
		{
			return 500;
		}

		public override void Handle( )
		{
			if ( !PluginSettings.Instance.NewUserTransportEnabled )
				return;

			if ( !_init )
			{
				_init = true;
				Init( );
			}

			/*
			if (!m_ready)
				return;

			if (MyAPIGateway.Players == null)
				return;

			List<IMyPlayer> players = new List<IMyPlayer>();
			bool result = false;
//			Wrapper.GameAction(() =>
//			{
				try
				{
					MyAPIGateway.Players.GetPlayers(players, null);
					result = true;
				}
				catch (Exception ex)
				{
					Log.Info(string.Format("Transport(): Unable to get player list: {0}", ex.ToString()));
				}
//			});

			if (!result)
				return;

			lock (_newUserList)
			{
				for (int r = _newUserList.Count - 1; r >= 0; r--)
				{
					ulong steamId = _newUserList[r];

					IMyPlayer player = players.FirstOrDefault(x => x.SteamUserId == steamId && x.Controller != null && x.Controller.ControlledEntity != null);
					if (player != null)
					{
						Log.Info(string.Format("Player entered game, starting movement."));
						_newUserList.RemoveAt(r);

						// In Game
						IMyEntity playerEntity = player.Controller.ControlledEntity.Entity;
						// Player spawned in space suit, ug.
						if (!(playerEntity.GetTopMostParent() is IMyCubeGrid))
							continue;

						IMyEntity entityToTransport = playerEntity.GetTopMostParent();
						MyObjectBuilder_CubeGrid cubeGrid = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entityToTransport);
						if (cubeGrid == null)
							continue;

						Vector3D validPosition = Vector3D.Zero;
						Vector3D asteroidPosition = Vector3D.Zero;

						if(PluginSettings.Instance.NewUserTransportSpawnType == NewUserTransportSpawnPoint.Asteroids)
							FindViableAsteroid(out validPosition, out asteroidPosition);
						else if(PluginSettings.Instance.NewUserTransportSpawnType == NewUserTransportSpawnPoint.Origin)
							validPosition = MathUtility.RandomPositionFromPoint(Vector3D.Zero, PluginSettings.Instance.NewUserTransportDistance);

						if(validPosition == Vector3D.Zero)
						{
							Log.Info("Could not find a valid asteroid to drop off a new user.");
							continue;
						}

						Log.Info(string.Format("Attempting to move user to: {0}", General.Vector3DToString(validPosition)));

						Wrapper.GameAction(() =>
						{
							if(player.Controller != null && player.Controller.ControlledEntity != null)
								player.Controller.ControlledEntity.Use();
						});

						Thread.Sleep(100);
						cubeGrid.PositionAndOrientation = new MyPositionAndOrientation(validPosition, Vector3.Forward, Vector3.Up);
						List<MyObjectBuilder_EntityBase> list = new List<MyObjectBuilder_EntityBase>();
						list.Add(cubeGrid);

						IMyEntity newEntity = null;
						Wrapper.GameAction(() =>
						{
							BaseEntityNetworkManager.BroadcastRemoveEntity(entityToTransport);
							MyAPIGateway.Entities.RemapObjectBuilder(cubeGrid);
							newEntity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(cubeGrid);
							MyAPIGateway.Multiplayer.SendEntitiesCreated(list);
						});


							/*
						CubeGridEntity gridEntity = new CubeGridEntity(cubeGrid);
						gridEntity.PositionAndOrientation = CubeGrids.CreatePositionAndOrientation(validPosition, asteroidPosition);
						SectorObjectManager.Instance.AddEntity(gridEntity);
													    */

			/*
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(cubeGrid);
			});
			*/

			//Communication.SendPrivateInformation(steamId, string.Format("Welcome {0}.  We are moving you closer to an asteroid ... please stand by ...", PlayerMap.Instance.GetPlayerNameFromSteamId(steamId)));

			//CubeGridEntity grid = new CubeGridEntity((MyObjectBuilder_CubeGrid)entityToTransport.GetObjectBuilder(), entityToTransport);
			//if (!CubeGrids.WaitForLoadingEntity(grid))
			//	continue;
			/*
			foreach(CubeBlockEntity block in grid.CubeBlocks)
			{
				if(block is CockpitEntity)
				{
					block.IntegrityPercent = 0.1f;
					Log.Info(string.Format("Removing User From Cockpit: {0}", steamId));
				}
			}
			*/

			/*

//						Wrapper.GameAction(() =>
//						{
				// This should boot them out of their ship: it does not, it kills them :(
//							MyAPIGateway.Entities.RemoveEntity(entityToTransport);
				MyAPIGateway.Entities.RemapObjectBuilder(cubeGrid);
//						});

			CubeGridEntity gridEntity = new CubeGridEntity(cubeGrid);
			gridEntity.PositionAndOrientation = CubeGrids.CreatePositionAndOrientation(validPosition, asteroidPosition);
			SectorObjectManager.Instance.AddEntity(gridEntity);
			//Communication.SendPrivateInformation(steamId, string.Format("You have been moved!  You should be within {0} meters of an asteroid.", PluginSettings.Instance.NewUserTransportDistance));
		}
	}
}
			 */

			base.Handle( );
		}

		public override void OnPlayerJoined( ulong remoteUserId )
		{
			if ( !PluginSettings.Instance.NewUserTransportEnabled )
				return;

			if ( PlayerMap.Instance.GetPlayerIdsFromSteamId( remoteUserId ).Any( ) && !PluginSettings.Instance.NewUserTransportMoveAllSpawnShips )
				return;

			lock ( _newUserList )
			{
				_newUserList.Add( remoteUserId );
				Essentials.Log.Info( "New User Transport Queued: {0}", remoteUserId );
			}

			base.OnPlayerJoined( remoteUserId );
		}

		public override void OnPlayerLeft( ulong remoteUserId )
		{
			if ( !PluginSettings.Instance.NewUserTransportEnabled )
				return;

			lock ( _newUserList )
			{
				if ( _newUserList.Exists( x => x == remoteUserId ) )
				{
					Essentials.Log.Info( "Queued Transport Removed: {0}", remoteUserId );
					_newUserList.RemoveAll( x => x == remoteUserId );
				}
			}

			base.OnPlayerLeft( remoteUserId );
		}

		public override void OnEntityAdd( IMyEntity entity )
		{
			if ( !PluginSettings.Instance.NewUserTransportEnabled )
				return;

			TransportPlayer( entity );

			base.OnEntityAdd( entity );
		}

		private void TransportPlayer( IMyEntity entityToTransport )
		{
			if ( entityToTransport is IMyCharacter )
			{
				MyObjectBuilder_Character c = (MyObjectBuilder_Character)entityToTransport.GetObjectBuilder( );
				if ( c.Health < 1 )
					return;

				Thread.Sleep( 50 );
				BoundingSphereD sphere = new BoundingSphereD( entityToTransport.GetTopMostParent( ).GetPosition( ), 300 );
				List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere( ref sphere );

				bool found = entities.Any( testEntity => testEntity != entityToTransport );

				if ( found )
					return;

				MoveEntity( entityToTransport );
			}
			else if ( entityToTransport is IMyCubeGrid )
			{
				foreach ( string name in PluginSettings.Instance.NewUserTransportSpawnShipNames )
				{
					if ( entityToTransport.DisplayName.ToLower( ).Contains( name.ToLower( ) ) )
					{
						if ( PluginSettings.Instance.DynamicClientConcealEnabled )
						{
							//ClientEntityManagement.SyncFix.Add(entityToTransport.EntityId, DateTime.Now);
						}

						MoveEntity( entityToTransport );
						break;
					}
				}
			}
		}

		private void MoveEntity( IMyEntity entity )
		{
			Thread.Sleep( 100 );
			Vector3D validPosition = Vector3D.Zero;
			Vector3D asteroidPosition = Vector3D.Zero;

			if ( PluginSettings.Instance.NewUserTransportSpawnType == NewUserTransportSpawnPoint.Asteroids )
				FindViableAsteroid( out validPosition, out asteroidPosition );
			else if ( PluginSettings.Instance.NewUserTransportSpawnType == NewUserTransportSpawnPoint.Origin )
				validPosition = MathUtility.RandomPositionFromPoint( Vector3D.Zero, PluginSettings.Instance.NewUserTransportDistance );

			if ( validPosition == Vector3D.Zero )
			{
				Essentials.Log.Info( "Could not find a valid asteroid to drop off a new user." );
				return;
			}

			//Log.Info(string.Format("Attempting to move a character to: {0}", General.Vector3DToString(validPosition)));
			List<IMyPlayer> players = new List<IMyPlayer>( );
			MyAPIGateway.Players.GetPlayers( players );
			IMyPlayer targetPlayer = null;
			foreach ( IMyPlayer player in players )
			{
				if ( player.Controller == null || player.Controller.ControlledEntity == null || player.Controller.ControlledEntity.Entity == null )
					continue;

				if ( player.Controller.ControlledEntity.Entity.GetTopMostParent( ) == entity )
				{
					targetPlayer = player;
					break;
				}
			}

			if ( targetPlayer == null )
			{
				//Log.Info(string.Format("Unable to find target player for entityToTransport"));
				return;
			}

			if ( PlayerMap.Instance.GetPlayerIdsFromSteamId( targetPlayer.SteamUserId ).Any( ) && !PluginSettings.Instance.NewUserTransportMoveAllSpawnShips )
			{
				Essentials.Log.Info( "Not a new user, skipping" );
				return;
			}

			Essentials.Log.Info( "Moving player {0} to '{1}'", targetPlayer.DisplayName, validPosition );
			Communication.MoveMessage( targetPlayer.SteamUserId, "normal", validPosition );
		}

		private void FindViableAsteroid( out Vector3D validPosition, out Vector3D asteroidPosition )
		{
			validPosition = Vector3D.Zero;
			asteroidPosition = Vector3D.Zero;
            /*
			List<VoxelMap> voxelMaps = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>( );
			for ( int r = 0; r < voxelMaps.Count; r++ )
			{
				int choice = _random.Next( 0, voxelMaps.Count - r );
				VoxelMap voxelMap = voxelMaps[ choice ];
				voxelMaps.RemoveAt( choice );

				if ( PluginSettings.Instance.NewUserTransportAsteroidDistance > 0 && Vector3D.Distance( voxelMap.Position, Vector3D.Zero ) > PluginSettings.Instance.NewUserTransportAsteroidDistance )
					continue;

				Essentials.Log.Info( "Found asteroid with viable materials: {0} - {1}", voxelMap.Name, voxelMap.Materials.Count );
				asteroidPosition = voxelMap.Position;
				validPosition = MathUtility.RandomPositionFromPoint( asteroidPosition, PluginSettings.Instance.NewUserTransportDistance );
				break;
                
				//*
				if (voxelMap.Materials.Count > 3)
				{
					Log.Info(string.Format("Found asteroid with viable materials: {0} - {1}", voxelMap.Name, voxelMap.Materials.Count()));
					asteroidPosition = voxelMap.Position;
					validPosition = MathUtility.RandomPositionFromPoint(asteroidPosition, PluginSettings.Instance.NewUserTransportDistance);
					break;
				}
				 */
			//}
		}
	}
}

