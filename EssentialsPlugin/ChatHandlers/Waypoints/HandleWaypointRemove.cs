using System.Collections.Generic;
using System.Linq;
using EssentialsPlugin.Utility;


namespace EssentialsPlugin.ChatHandlers
{
	public class HandleWaypointRemove : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Removes a personal waypoint.  Usage: /waypoint remove \"waypoint name\"";
		}

		public override string GetCommandText()
		{
			return "/waypoint remove";
		}

		public override string[] GetMultipleCommandText()
		{
			return new string[] { "/waypoint remove", "/wp remove" };
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

			if (splits.Count() != 1)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			List<WaypointItem> items = Waypoints.Instance.Get(userId);
			if (items.FirstOrDefault(x => x.Name.ToLower() == splits[0].ToLower()) == null)
			{
				Communication.SendPrivateInformation(userId, string.Format("You do not have a waypoint with the name: {0}", splits[0]));
				return true;
			}

			Waypoints.Instance.Remove(userId, splits[0]);

			string remove = "";
			foreach (string split in splits)
			{
				if (remove == "")
					remove += split.ToLower();
				else
					remove += " " + split;
			}

			Communication.SendClientMessage(userId, string.Format("/waypoint remove {0}", remove));
			Communication.SendPrivateInformation(userId, string.Format("Removed waypoint: {0}", splits[0]));
			return true;
		}
	}
}
