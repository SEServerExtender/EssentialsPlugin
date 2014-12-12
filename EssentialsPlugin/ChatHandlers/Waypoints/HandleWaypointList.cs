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

			Communication.SendPrivateInformation(userId, "Personal Waypoints:");

			List<WaypointItem> items = Waypoints.Instance.Get(userId);
			foreach (WaypointItem item in items)
			{
				Communication.SendPrivateInformation(userId, string.Format("Waypoint {0}: \"{1}\"  Location: {2}", item.Name, item.Text, General.Vector3DToString(item.Position)));
			}

			Communication.SendPrivateInformation(userId, string.Format("Total waypoints: {0}", items.Count));

			return true;
		}
	}
}
