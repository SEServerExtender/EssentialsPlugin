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

			string playerName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
			long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(userId);
			IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
			if (faction == null)
			{
				Communication.SendPrivateInformation(userId, string.Format("Unable to find your faction information.  You must be in a faction to use this."));
				return true;
			}

			List<WaypointItem> items = Waypoints.Instance.Get((ulong)faction.FactionId);
			WaypointItem item = items.FirstOrDefault(x => x.Name.ToLower() == splits[0].ToLower());
			if (item == null)
			{
				Communication.SendPrivateInformation(userId, string.Format("You do not have a faction waypoint with the name: {0}", splits[0]));
				return true;
			}

			if (item.Leader && !faction.IsLeader(playerId))
			{
				Communication.SendPrivateInformation(userId, string.Format("You must be a faction leader to remove the waypoint: {0}", splits[0]));
				return true;
			}

			Waypoints.Instance.Remove((ulong)faction.FactionId, splits[0]);

			string remove = "";
			foreach (string split in splits)
			{
				if (remove == "")
					remove += split.ToLower();
				else
					remove += " " + split;
			}

			foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
			{
				if (Player.CheckPlayerSameFaction(userId, steamId))
				{
					Communication.SendClientMessage(steamId, string.Format("/waypoint remove {0}", remove));
				}
			}

			Communication.SendFactionClientMessage(userId, string.Format("/message Server {0} has removed the waypoint: {1}", playerName, remove));
			return true;
		}
	}
}
