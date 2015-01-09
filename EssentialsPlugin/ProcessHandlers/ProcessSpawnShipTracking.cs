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
	public class ProcessSpawnShipTracking : ProcessHandlerBase
	{
		private List<ulong> m_newUserList;
		private volatile bool m_ready = false;
		private Random m_random;
		private bool m_init;
		private DateTime m_lastUpdate = DateTime.Now;


		public override int GetUpdateResolution()
		{
			return 5000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.NewUserTransportEnabled)
				return;

			if (MyAPIGateway.Players == null)
				return;

			if (PluginSettings.Instance.NewUserTransportStopRunawaySpawnShips)
			{
				if (DateTime.Now - m_lastUpdate > TimeSpan.FromSeconds(5))
				{
					m_lastUpdate = DateTime.Now;
					HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
					try
					{
						MyAPIGateway.Entities.GetEntities(entities);
					}
					catch
					{
						Logging.WriteLineAndConsole(string.Format("StopRunaway(): Entities busy, skipping"));
						return;
					}

					foreach (IMyEntity entity in entities)
					{
						if (!(entity is IMyCubeGrid))
							continue;

						bool found = false;
						foreach (string name in PluginSettings.Instance.NewUserTransportSpawnShipNames)
						{
							if (entity.DisplayName.Contains(name))
							{
								found = true;
								break;
							}
						}

						if (!found)
							continue;

						IMyCubeGrid grid = (IMyCubeGrid)entity;
						if (grid.Physics != null)
						{
							bool foundControlled = false;
							foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
							{
								long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(steamId);
								IMyEntity testEntity = Player.FindControlledEntity(playerId);
								if (testEntity == entity)
								{
									foundControlled = true;
									break; ;
								}
							}

							if (foundControlled)
								continue;

							Wrapper.GameAction(() =>
							{
								try
								{
									double linear = Math.Round(((Vector3)grid.Physics.LinearVelocity).LengthSquared(), 1);
									double angular = Math.Round(((Vector3)grid.Physics.AngularVelocity).LengthSquared(), 1);

									if (linear > 0 || angular > 0)
									{
										grid.Physics.LinearVelocity = Vector3.Zero;
										grid.Physics.AngularVelocity = Vector3.Zero;
										Logging.WriteLineAndConsole(string.Format("Stopping runaway spawnship: {0}", grid.EntityId));
									}
								}
								catch (Exception ex)
								{
									Logging.WriteLineAndConsole(string.Format("Error stopping spawnship: {0}", ex.ToString()));
								}
							});
						}
					}
				}
			}

			base.Handle();
		}
	}
}

