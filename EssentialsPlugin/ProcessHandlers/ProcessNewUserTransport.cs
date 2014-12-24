using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssentialsPlugin.Utility;
using Sandbox.ModAPI;
using SEModAPIInternal.API.Common;
using Sandbox.Common.ObjectBuilders;
using VRageMath;
using System.Threading;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

namespace EssentialsPlugin.ProcessHandler
{
	public class ProcessNewUserTransport : ProcessHandlerBase
	{
		private List<ulong> m_newUserList;
		private volatile bool m_ready = false;
		private Random m_random;
		private bool m_init;

		public ProcessNewUserTransport()
		{
			m_random = new Random();
			m_init = false;
			m_newUserList = new List<ulong>();
		}

		private void Init()
		{
			// Cache asteroids
			Thread thread = new Thread((Object state) =>
			{
				List<VoxelMap> voxels = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();
				Thread.Sleep(10000);
				voxels = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();

				Logging.WriteLineAndConsole("Starting Voxel Caching .. This might take awhile");
				foreach (VoxelMap voxel in voxels)
				{
					DateTime start = DateTime.Now;
					int voxelMaterialCount = voxel.Materials.Count;
					Logging.WriteLineAndConsole(string.Format("Caching Voxel: {0} - {1} (Took: {2}s)", voxel.Name, voxelMaterialCount, (DateTime.Now - start).TotalSeconds));
				}
				Logging.WriteLineAndConsole("Completed Voxel Caching");

				m_ready = true;

			});
			thread.Priority = ThreadPriority.BelowNormal;
			thread.IsBackground = true;
			thread.Start();

			/*
			ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
			{				
				List<VoxelMap> voxels = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();
				Thread.Sleep(10000);
				voxels = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();

				Logging.WriteLineAndConsole("Starting Voxel Caching .. This might take awhile");
				foreach (VoxelMap voxel in voxels)
				{
					DateTime start = DateTime.Now;
					int voxelMaterialCount = voxel.Materials.Count;
					Logging.WriteLineAndConsole(string.Format("Caching Voxel: {0} - {1} (Took: {2}s)", voxel.Name, voxelMaterialCount, (DateTime.Now - start).TotalSeconds));
				}
				Logging.WriteLineAndConsole("Completed Voxel Caching");

				m_ready = true;
			}));
			 */ 
		}

		public override int GetUpdateResolution()
		{
			return 500;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.NewUserTransportEnabled)
				return;

			if (!m_init)
			{ 
				m_init = true;
				Init();
			}

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
					Logging.WriteLineAndConsole(string.Format("Transport(): Unable to get player list: {0}", ex.ToString()));
				}
//			});

			if (!result)
				return;

			lock (m_newUserList)
			{
				for (int r = m_newUserList.Count - 1; r >= 0; r--)
				{
					ulong steamId = m_newUserList[r];

					IMyPlayer player = players.FirstOrDefault(x => x.SteamUserId == steamId && x.Controller != null && x.Controller.ControlledEntity != null);
					if (player != null)
					{
						Logging.WriteLineAndConsole(string.Format("Player entered game, starting movement."));
						m_newUserList.RemoveAt(r);

						// In Game
						IMyEntity playerEntity = player.Controller.ControlledEntity.Entity;
						// Player spawned in space suit, ug.
						if (!(playerEntity.GetTopMostParent() is IMyCubeGrid))
							continue;

						IMyEntity entity = playerEntity.GetTopMostParent();
						MyObjectBuilder_CubeGrid cubeGrid = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entity);
						if (cubeGrid == null)
							continue;

						Vector3D validPosition = Vector3D.Zero;
						Vector3D asteroidPosition = Vector3D.Zero;
						FindViableAsteroid(out validPosition, out asteroidPosition);

						if(validPosition == Vector3D.Zero)
						{
							Logging.WriteLineAndConsole("Could not find a valid asteroid to drop off a new user.");
							continue;
						}

						Communication.SendPrivateInformation(steamId, string.Format("Welcome {0}.  We are moving you closer to an asteroid ... please stand by ...", PlayerMap.Instance.GetPlayerNameFromSteamId(steamId)));

						CubeGridEntity grid = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
						if (!CubeGrids.WaitForLoadingEntity(grid))
							continue;

						foreach(CubeBlockEntity block in grid.CubeBlocks)
						{
							if(block is CockpitEntity)
							{
								block.IntegrityPercent = 0.1f;
								Logging.WriteLineAndConsole(string.Format("Removing User From Cockpit: {0}", steamId));
							}
						}
						
						Thread.Sleep(500);
						grid.Dispose();

//						Wrapper.GameAction(() =>
//						{
							// This should boot them out of their ship: it does not, it kills them :(
//							MyAPIGateway.Entities.RemoveEntity(entity);
							MyAPIGateway.Entities.RemapObjectBuilder(cubeGrid);
//						});

						CubeGridEntity gridEntity = new CubeGridEntity(cubeGrid);
						gridEntity.PositionAndOrientation = CubeGrids.CreatePositionAndOrientation(validPosition, asteroidPosition);
						SectorObjectManager.Instance.AddEntity(gridEntity);
						Communication.SendPrivateInformation(steamId, string.Format("You have been moved!  You should be within {0} meters of an asteroid.", PluginSettings.Instance.NewUserTransportDistance));
					}
				}
			}

			base.Handle();
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			if (!PluginSettings.Instance.NewUserTransportEnabled)
				return;

			if (PlayerMap.Instance.GetPlayerIdsFromSteamId(remoteUserId).Count() > 0 && !PluginSettings.Instance.NewUserTransportMoveAllSpawnShips)
				return;

			lock (m_newUserList)
			{
				m_newUserList.Add(remoteUserId);
				Logging.WriteLineAndConsole(string.Format("New User Transport Queued: {0}", remoteUserId));
			}

			base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerLeft(ulong remoteUserId)
		{
			if (!PluginSettings.Instance.NewUserTransportEnabled)
				return;

			lock (m_newUserList)
			{
				if (m_newUserList.Exists(x => x == remoteUserId))
				{
					Logging.WriteLineAndConsole(string.Format("Queued Transport Removed: {0}", remoteUserId));
					m_newUserList.RemoveAll(x => x == remoteUserId);
				}
			}

			base.OnPlayerLeft(remoteUserId);
		}

		private void FindViableAsteroid(out Vector3D validPosition, out Vector3D asteroidPosition)
		{
			validPosition = Vector3D.Zero;
			asteroidPosition = Vector3D.Zero;

			List<VoxelMap> voxelMaps = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();
			for(int r = 0; r < voxelMaps.Count; r++)
			{
				int choice = m_random.Next(0, voxelMaps.Count - r);
				VoxelMap voxelMap = voxelMaps[choice];
				voxelMaps.RemoveAt(choice);

				if (PluginSettings.Instance.NewUserTransportAsteroidDistance > 0 && Vector3D.Distance(voxelMap.Position, Vector3D.Zero) > PluginSettings.Instance.NewUserTransportAsteroidDistance)
					continue;

				if (voxelMap.Materials.Count > 3)
				{
					Logging.WriteLineAndConsole(string.Format("Found asteroid with viable materials: {0} - {1}", voxelMap.Name, voxelMap.Materials.Count()));
					asteroidPosition = voxelMap.Position;
					validPosition = MathUtility.RandomPositionFromPoint(asteroidPosition, PluginSettings.Instance.NewUserTransportDistance);
					break;
				}
			}
		}
	}
}

