namespace EssentialsPlugin.ChatHandlers.Waypoints
{
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;

	public class HandleWaypointGroupRemove : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Removes a waypoint to a waypoint group.  Usage: /waypoint groupremove [existing waypoint name].  Example: /waypoint groupremove Target1";
		}

		public override string GetCommandText()
		{
			return "/waypoint groupremove";
		}

		public override string[] GetMultipleCommandText()
		{
			return new string[] { "/waypoint groupremove", "/wp groupremove", "/waypoint gr", "/wp gr" };
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

			if (words.Length != 1)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;			
			}

			string name = words[0];

			List<WaypointItem> items = Waypoints.Instance.Get(userId);
			WaypointItem item = items.FirstOrDefault(x => x.Name.ToLower() == words[0]);
			if (item != null)
			{
				if (Waypoints.Instance.GroupRemove(userId, name))
					Communication.SendPrivateInformation(userId, string.Format("Waypoint '{0}' removed from group", name));
				else
					Communication.SendPrivateInformation(userId, string.Format("Failed to remove waypoint '{0}' from group", name));

				return true;
			}

			string playerName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
			long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(userId);
			IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
			if (faction != null)
			{
				items = Waypoints.Instance.Get((ulong)faction.FactionId);
				item = items.FirstOrDefault(x => x.Name.ToLower() == words[0]);

				if (item != null)
				{
					if (item.Leader && !faction.IsLeader(playerId))
					{
						Communication.SendPrivateInformation(userId, "Only a faction leader can modify this item");
						return true;
					}

					if (Waypoints.Instance.GroupRemove((ulong)faction.FactionId, name))
						Communication.SendFactionClientMessage(userId, string.Format("/message Server {0} removed the waypoint '{1}' from it's group", playerName, name));
					else
						Communication.SendPrivateInformation(userId, string.Format("Failed to remove faction waypoint '{0}' from it's group", name));

					return true;
				}
			}

			return true;
		}
	}
}
