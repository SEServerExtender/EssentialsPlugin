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
	public class HandleWaypointAdd : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Creates a personal waypoint.  Only you can see it.  Usage: /waypoint add \"waypoint name\" \"waypoint text\" Neutral | Allied | Enemy X Y Z.  Example: /waypoint add MyWayPoint MyWaypoint Neutral 1000 1000 1000";
		}

		public override string GetCommandText()
		{
			return "/waypoint add";
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
			
			string[] splits = General.SplitString(string.Join(" ", words));

			if (splits.Count() != 6)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			List<WaypointItem> items = Waypoints.Instance.Get(userId);
			if (PluginSettings.Instance.WaypointsMaxPerPlayer > 0 && items.Count >= PluginSettings.Instance.WaypointsMaxPerPlayer)
			{
				Communication.SendPrivateInformation(userId, string.Format("Waypoint limit has been reached.  You may only have {0} waypoints at a time on this server.  Please remove some waypoints in order to add new ones.", PluginSettings.Instance.WaypointsMaxPerPlayer));
				return true;
			}

			for (int r = 3; r < 6; r++)
			{
				double test = 0d;
				if(!double.TryParse(splits[r], out test))
				{
					Communication.SendPrivateInformation(userId, string.Format("Invalid position information: {0} is invalid", splits[r]));
					return true;
				}
			}

			string add = "";
			foreach (string split in splits)
			{
				if (add == "")
					add += split.ToLower();
				else
					add += " " + split;
			}

			Communication.SendClientMessage(userId, string.Format("/waypoint add {0}", add));

			WaypointItem item = new WaypointItem();
			item.SteamId = userId;
			item.Name = splits[0];
			item.Text = splits[1];
			WaypointTypes type = WaypointTypes.Neutral;
			Enum.TryParse<WaypointTypes>(splits[2], true, out type);
			item.WaypointType = type;
			item.Position = new Vector3D(double.Parse(splits[3]), double.Parse(splits[4]), double.Parse(splits[5]));
			Waypoints.Instance.Add(item);

			Communication.SendPrivateInformation(userId, string.Format("Waypoint added: {0} at ({1})", item.Name, General.Vector3DToString(item.Position)));

			return true;
		}
	}
}
