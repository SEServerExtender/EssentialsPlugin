namespace EssentialsPlugin.ChatHandlers.Waypoints
{
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;

	public class HandleWaypointFactionRemove : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Removes a faction waypoint.  If a leader created the waypoint, only a leader can remove it.  Usage: /waypoint factionremove \"name\"";
		}

		public override string GetCommandText()
		{
			return "/waypoint factionremove";
		}

		public override string[] GetMultipleCommandText()
		{
			return new string[] { "/waypoint factionremove", "/wp factionremove", "/waypoint fr", "/wp fr" };
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\"Sorry, there's nothing here yet :(\" \"close\" ";
            return longMessage;
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

			string playerName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
			long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(userId);
			IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
			if (faction == null)
			{
				Communication.SendPrivateInformation(userId, string.Format("Unable to find your faction information.  You must be in a faction to use this."));
				return true;
			}

			List<WaypointItem> items = Waypoints.Instance.Get((ulong)faction.FactionId);
			WaypointItem item = items.FirstOrDefault(x => x.Name.ToLower() == words[0].ToLower());
			if (item == null)
			{
				Communication.SendPrivateInformation(userId, string.Format("You do not have a faction waypoint with the name: {0}", words[0]));
				return true;
			}

			if (item.Leader && !faction.IsLeader(playerId))
			{
				Communication.SendPrivateInformation(userId, string.Format("You must be a faction leader to remove the waypoint: {0}", words[0]));
				return true;
			}

			Waypoints.Instance.Remove((ulong)faction.FactionId, words[0]);
			foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
			{
				if (Player.CheckPlayerSameFaction(userId, steamId))
				{
					Communication.SendClientMessage(steamId, string.Format("/waypoint remove '{0}'", words[0]));
				}
			}

			Communication.SendFactionClientMessage(userId, string.Format("/message Server {0} has removed the waypoint: '{1}'", playerName, words[0]));
			return true;
		}
	}
}
