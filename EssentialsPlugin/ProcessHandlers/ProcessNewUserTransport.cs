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
using EssentialsPlugin.Utility;

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
		}

		public override int GetUpdateResolution()
		{
			return 250;
		}

		public override void Handle()
		{
			if (!m_init)
			{ 
				m_init = true;
				Init();
			}

			if (!m_ready)
				return;

			if (!PluginSettings.Instance.NewUserTransportEnabled)
				return;

			List<IMyPlayer> players = new List<IMyPlayer>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Players.GetPlayers(players, null);
			});

			lock (m_newUserList)
			{
				for (int r = m_newUserList.Count - 1; r >= 0; r--)
				{
					ulong steamId = m_newUserList[r];

					IMyPlayer player = players.FirstOrDefault(x => x.SteamUserId == steamId && x.Controller != null && x.Controller.ControlledEntity != null);
					if (player != null)
					{
						m_newUserList.RemoveAt(r);

						// In Game
						IMyEntity playerEntity = player.Controller.ControlledEntity.Entity;
						// Player spawned in space suit, ug.
						if (!(playerEntity.GetTopMostParent() is IMyCubeGrid))
							continue;

						IMyEntity entity = playerEntity.GetTopMostParent();
						MyObjectBuilder_CubeGrid cubeGrid = (MyObjectBuilder_CubeGrid)entity.GetObjectBuilder();
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

						int count = 0;
						while (grid.IsLoading)
						{
							Thread.Sleep(100);
							count++;
							if (count > 10)
								break;
						}

						if (grid.IsLoading)
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

						Wrapper.GameAction(() =>
						{
							// This should boot them out of their ship
//							MyAPIGateway.Entities.RemoveEntity(entity);
							MyAPIGateway.Entities.RemapObjectBuilder(cubeGrid);
						});

						CubeGridEntity gridEntity = new CubeGridEntity(cubeGrid);
						gridEntity.PositionAndOrientation = CubeGrids.CreatePositionAndOrientation(validPosition, asteroidPosition);
						SectorObjectManager.Instance.AddEntity(gridEntity);
						Communication.SendPrivateInformation(steamId, string.Format("You have been moved!  You should be within {0} meters of an asteroid.  Good luck.", PluginSettings.Instance.NewUserTransportDistance));
					}
				}
			}

			base.Handle();
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			if (PlayerMap.Instance.GetPlayerIdsFromSteamId(remoteUserId).Count() > 0)
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
			Logging.WriteLineAndConsole(string.Format("Removing New User Transport Queued: {0}", remoteUserId));
			lock (m_newUserList)
			{
				if (m_newUserList.Exists(x => x == remoteUserId))
				{
					Logging.WriteLineAndConsole(string.Format("New User Transport Removed: {0}", remoteUserId));
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
				if (voxelMap.Materials.Count > 3)
				{
					Logging.WriteLineAndConsole(string.Format("Found asteroid with viable materials: {0} - {1}", voxelMap.Name, voxelMap.Materials.Count()));
					asteroidPosition = (Vector3)voxelMap.Position;
					validPosition = MathUtility.RandomPositionFromPoint(asteroidPosition, PluginSettings.Instance.NewUserTransportDistance);
					break;
				}
			}
		}
	}
}

