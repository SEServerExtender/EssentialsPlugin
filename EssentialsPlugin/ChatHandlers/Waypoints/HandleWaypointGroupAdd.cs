namespace EssentialsPlugin.ChatHandlers.Waypoints
{
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;

	public class HandleWaypointGroupAdd : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Adds a waypoint to a waypoint group.  Waypoint groups can be toggled on and off together with the toggle command.  Usage: /waypoint groupadd [group name] [existing waypoint name].  Example: /waypoint groupadd Targets Target1";
		}

		public override string GetCommandText()
		{
			return "/waypoint groupadd";
		}

		public override string[] GetMultipleCommandText()
		{
			return new string[] { "/waypoint groupadd", "/wp groupadd", "/waypoint ga", "/wp ga" };
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

			if (splits.Length != 2)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			string name = splits[0];
			string group = splits[1];

			List<WaypointItem> items = Waypoints.Instance.Get(userId);
			WaypointItem item = items.FirstOrDefault(x => x.Name.ToLower() == splits[0]);
			if (item != null)
			{
				if (Waypoints.Instance.GroupAdd(userId, splits[0], splits[1]))
					Communication.SendPrivateInformation(userId, string.Format("Waypoint '{0}' added to the group '{1}'", name, group));
				else
					Communication.SendPrivateInformation(userId, string.Format("Failed to add waypoint '{0}' to the group '{1}'", name, group));

				return true;
			}

			string playerName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
			long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(userId);
			IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
			if (faction != null)
			{
				items = Waypoints.Instance.Get((ulong)faction.FactionId);
				item = items.FirstOrDefault(x => x.Name.ToLower() == splits[0]);

				if (item != null)
				{
					if (item.Leader && !faction.IsLeader(playerId))
					{
						Communication.SendPrivateInformation(userId, "Only a faction leader can modify this item");
						return true;
					}

					if (Waypoints.Instance.GroupAdd((ulong)faction.FactionId, splits[0], splits[1]))
						Communication.SendFactionClientMessage(userId, string.Format("/message Server {0} added the waypoint '{1}' to the group '{2}'", playerName, name, group));
					else
						Communication.SendPrivateInformation(userId, string.Format("Failed to add faction waypoint '{0}' to the group '{1}'", name, group));

					return true;
				}
			}

			Communication.SendPrivateInformation(userId, string.Format("You do not have a waypoint with the name '{0}'", name));
			return true;
		}
	}
}
