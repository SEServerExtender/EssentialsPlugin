namespace EssentialsPlugin.ChatHandlers.Waypoints
{
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;

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

			if (words.Count() != 1)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			List<WaypointItem> items = Waypoints.Instance.Get(userId);
			if (items.FirstOrDefault(x => x.Name.ToLower() == words[0].ToLower()) == null)
			{
				Communication.SendPrivateInformation(userId, string.Format("You do not have a waypoint with the name: {0}", words[0]));
				return true;
			}

			Waypoints.Instance.Remove(userId, words[0]);
			//Communication.SendClientMessage(userId, string.Format("/waypoint remove '{0}'", words[0]));
			//Communication.SendPrivateInformation(userId, string.Format("Removed waypoint: '{0}'", words[0]));
			return true;
		}
	}
}
