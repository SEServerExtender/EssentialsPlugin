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

			if (splits.Length != 1)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;			
			}

			string name = splits[0];

			List<WaypointItem> items = Waypoints.Instance.Get(userId);
			WaypointItem item = items.FirstOrDefault(x => x.Name.ToLower() == splits[0]);
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
				item = items.FirstOrDefault(x => x.Name.ToLower() == splits[0]);

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
