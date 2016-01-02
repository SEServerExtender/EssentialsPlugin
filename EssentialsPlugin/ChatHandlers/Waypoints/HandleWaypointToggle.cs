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

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "";
            DialogItem.content = GetHelp( );
            DialogItem.buttonText = "close";
            return DialogItem;
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

			if (words.Length > 0 && !Waypoints.Instance.GroupExists(userId, words[0]))
			{
				Communication.SendPrivateInformation(userId, string.Format("Group '{0}' does not exist.  You can only toggle a valid group", words[0]));
				return true;
			}

			if (words.Length < 1)
				Waypoints.Instance.Toggle(userId);
			else
				Waypoints.Instance.Toggle(userId, words[0]);

			Waypoints.SendClientWaypoints(userId);

			if (words.Length < 1)
				Communication.SendPrivateInformation(userId, string.Format("Toggled all waypoints"));
			else
				Communication.SendPrivateInformation(userId, string.Format("Toggled waypoint group '{0}'", words[0]));

			return true;
		}
	}
}
