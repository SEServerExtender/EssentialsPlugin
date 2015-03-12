namespace EssentialsPlugin.ChatHandlers.Waypoints
{
	using EssentialsPlugin.Utility;

	public class HandleWaypointToggle : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Toggles waypoints off or on.  Specifying a group name hides only that group.  Specifying a waypoint name only toggles that waypoint.  Usage: /waypoint toggle (optional: group name or waypoint name).  Example: /waypoint toggle Targets";
		}

		public override string GetCommandText()
		{
			return "/waypoint add";
		}

		public override string[] GetMultipleCommandText()
		{
			return new string[] { "/waypoint toggle", "/wp toggle" };
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

		public override bool HandleCommand( ulong userId, string command )
		{
			string[ ] words = command.Split( ' ' );
			if ( !PluginSettings.Instance.WaypointsEnabled )
				return false;
			
			string[] splits = General.SplitString(string.Join(" ", words));

			if (splits.Length > 0 && !Waypoints.Instance.GroupExists(userId, splits[0]))
			{
				Communication.SendPrivateInformation(userId, string.Format("Group '{0}' does not exist.  You can only toggle a valid group", splits[0]));
				return true;
			}

			if (splits.Length < 1)
				Waypoints.Instance.Toggle(userId);
			else
				Waypoints.Instance.Toggle(userId, splits[0]);

			Waypoints.SendClientWaypoints(userId);

			if (splits.Length < 1)
				Communication.SendPrivateInformation(userId, string.Format("Toggled all waypoints"));
			else
				Communication.SendPrivateInformation(userId, string.Format("Toggled waypoint group '{0}'", splits[0]));

			return true;
		}
	}
}
