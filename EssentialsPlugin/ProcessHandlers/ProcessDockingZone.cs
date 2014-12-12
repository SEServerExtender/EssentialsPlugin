using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EssentialsPlugin.Utility;
using Sandbox.ModAPI;
using SEModAPIInternal.API.Common;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using VRage.Common.Utils;
using System.Text.RegularExpressions;
using System.Threading;

using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Entity;

using EssentialsPlugin.Settings;

using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;

using VRageMath;

namespace EssentialsPlugin.ProcessHandler
{
	public class ProcessDockingZone : ProcessHandlerBase
	{
		private DateTime m_lastZoneUpdate;
		private List<IMyCubeGrid> m_zoneCache;
		private List<long> m_playersInside;

		public ProcessDockingZone()
		{
			m_lastZoneUpdate = DateTime.Now.AddMinutes(-1);
			m_playersInside = new List<long>();
			m_zoneCache = new List<IMyCubeGrid>();
		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.DockingEnabled)
				return;

			if (DateTime.Now - m_lastZoneUpdate > TimeSpan.FromSeconds(30))
			{
				m_lastZoneUpdate = DateTime.Now;
				PopulateZoneCache();
			}

			List<IMyPlayer> players = new List<IMyPlayer>();
			//Wrapper.GameAction(() =>
			//{
				MyAPIGateway.Players.GetPlayers(players);
			//});

			foreach (IMyPlayer player in players)
			{
				CheckPlayerInDockingZone(player);
			}

			base.Handle();
		}

		private void CheckPlayerInDockingZone(IMyPlayer player)
		{

			if(player.Controller == null || player.Controller.ControlledEntity == null || player.Controller.ControlledEntity.Entity == null)
				return;

			IMyEntity entity = player.Controller.ControlledEntity.Entity;
			long playerId = player.PlayerID;
			IMyEntity parent = entity.GetTopMostParent();

			// Not a ship?  let's not process
			if (!(parent is IMyCubeGrid))
			{
				return;
			}

			// Get All ships with 500m
			BoundingSphereD sphere = new BoundingSphereD(parent.GetPosition(), 500);
			List<IMyEntity> nearByEntities = null;

			// Live dangerously (no wrapper for speed!)
			try
			{
				nearByEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
			}
			catch(Exception ex)			
			{
				Logging.WriteLineAndConsole(string.Format("CheckPlayerInDockingZone(): {0}", ex.ToString()));
				return;
			}
			
			if (nearByEntities == null)
				return;

			List<IMyEntity> nearByGrids = nearByEntities.FindAll(x => x is IMyCubeGrid);

			// See if player's ship is inside a docking zone
			foreach (IMyEntity nearByEntity in nearByGrids)
			{
				// Sanity Check
				if (!(nearByEntity is IMyCubeGrid))
					return;

				IMyCubeGrid cubeGrid = (IMyCubeGrid)nearByEntity;
				// Does this grid contain a docking zone?
				if (m_zoneCache.Contains(cubeGrid))
				{
					Dictionary<String, List<IMyCubeBlock>> zoneList = DockingZone.GetZonesInGrid(cubeGrid);
					if (zoneList == null)
						continue;

					// Get zones
					foreach (KeyValuePair<String, List<IMyCubeBlock>> p in zoneList)
					{
						// Check if we're inside
						if (DockingZone.IsGridInside((IMyCubeGrid)parent, p.Value))
						{
							if (!m_playersInside.Contains(playerId))
							{
								m_playersInside.Add(playerId);
								ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerId(playerId);
								Communication.Notification(steamId, MyFontEnum.Green, 7, string.Format("You are inside a valid docking zone: {0}", p.Key));
							}

							return;
						}
					}
				}
			}

			// We've left
			if (m_playersInside.Contains(playerId))
			{
				m_playersInside.Remove(playerId);
				ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerId(playerId);
				Communication.Notification(steamId, MyFontEnum.Red, 7, "You have left a docking zone");
			}
		}

		private void PopulateZoneCache()
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
			});

			foreach (IMyEntity entity in entities)
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;
				if (DockingZone.DoesGridContainZone(grid))
				{
					m_zoneCache.Add(grid);
				}
			}
		}
	}
}

