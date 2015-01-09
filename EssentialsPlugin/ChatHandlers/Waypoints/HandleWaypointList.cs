using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using EssentialsPlugin.Utility;
using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;


namespace EssentialsPlugin.ChatHandlers
{
	public class HandleWaypointList : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Lists all personal waypoints.  Usage: /waypoint list";
		}

		public override string GetCommandText()
		{
			return "/waypoint list";
		}

		public override string[] GetMultipleCommandText()
		{
			return new string[] { "/waypoint list", "/wp list" };
		}

		public override bool IsAdminCommand()
		{
			return false;
		}

		public override bool AllowedInConsole()
		{
			return false;
		}

		public override bool IsClientOnly()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			if (!PluginSettings.Instance.WaypointsEnabled)
				return false;

			Communication.SendPrivateInformation(userId, "Personal / Faction Waypoints:");
			int personalCount = 0;
			int factionCount = 0;

			List<WaypointItem> items = Waypoints.Instance.Get(userId);
			string waypoints = "";
			foreach (WaypointItem item in items.OrderBy(x => x.Group))
			{
				if(waypoints != "")
					waypoints += "\r\n";

				if(item.Group != null && item.Group != "")
					waypoints += string.Format("Group {3} - {0}: '{1}' : ({2})", item.Name, item.Text, General.Vector3DToString(item.Position), item.Group);
				else
					waypoints += string.Format("{0}: '{1}' : ({2})", item.Name, item.Text, General.Vector3DToString(item.Position));
			}
			personalCount = items.Count;

			long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(userId);
			IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
			if (faction != null)
			{
				items = Waypoints.Instance.Get((ulong)faction.FactionId);
				if (waypoints != "" && items.Count > 0)
					waypoints += "\r\n";
				
				foreach (WaypointItem item in items.OrderBy(x => x.Group))
				{
					if (waypoints != "")
						waypoints += "\r\n";

					if (item.Group != null && item.Group != "")
						waypoints += string.Format("F: Group {3} - {0}: '{1}' : ({2})", item.Name, item.Text, General.Vector3DToString(item.Position), item.Group);
					else
						waypoints += string.Format("F: {0}: '{1}' : ({2})", item.Name, item.Text, General.Vector3DToString(item.Position));
				}

				factionCount = items.Count;
			}

			if(waypoints != "")
				waypoints += "\r\n";

			Communication.DisplayDialog(userId, "Waypoints", string.Format("Your defined waypoints: {0} personal, {1} faction", personalCount, factionCount), waypoints);

			waypoints += string.Format("Your defined waypoints: {0} personal, {1} faction", personalCount, factionCount);
			Communication.SendPrivateInformation(userId, waypoints);


			return true;
		}
	}
}
