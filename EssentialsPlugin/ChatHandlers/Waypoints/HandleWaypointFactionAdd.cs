namespace EssentialsPlugin.ChatHandlers.Waypoints
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using VRageMath;

	public class HandleWaypointFactionAdd : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Creates a faction waypoint.  Only your faction can see it.\r\nUsage: /waypoint factionadd \"waypoint name\" \"waypoint text\" Neutral | Allied | Enemy X Y Z (group name)\r\nExampleA: /waypoint factionadd MyWayPoint MyWaypoint Neutral 1000 1000 1000\r\nExampleB: /waypoint factionadd target\r\nExampleC: /waypoint factionadd Target1 Target1 Enemy 1000 1000 1000 Targets";
		}

		public override string GetCommandText()
		{
			return "/waypoint factionadd";
		}

		public override string[] GetMultipleCommandText()
		{
			return new[] { "/waypoint factionadd", "/wp factionadd", "/waypoint fa", "/wp fa" };
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
				if (!PluginSettings.Instance.WaypointsEnabled)
				return false;
			
			string[] splits = General.SplitString(string.Join(" ", words));

			if (splits.Length != 6 && splits.Length != 7 && splits.Length != 1)
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
			if (PluginSettings.Instance.WaypointsMaxPerFaction > 0 && items.Count >= PluginSettings.Instance.WaypointsMaxPerFaction)
			{
				Communication.SendPrivateInformation(userId, string.Format("Waypoint limit has been reached.  You may only have {0} faction waypoints at a time on this server.  Please remove some waypoints in order to add new ones.", PluginSettings.Instance.WaypointsMaxPerPlayer));
				return true;
			}

			if (splits.Length == 1)
			{
				IMyEntity playerEntity = Player.FindControlledEntity(playerId);
				if(playerEntity == null)
				{
					Communication.SendPrivateInformation(userId, string.Format("Can't find your position"));
					return true;
				}

				Vector3D pos = playerEntity.GetPosition();
				string name = splits[0];

				foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
				{
					if (Player.CheckPlayerSameFaction(userId, steamId))
					{
						Communication.SendClientMessage(steamId, string.Format("/waypoint add \"{0}\" \"{0}\" Neutral {1} {2} {3}", name, Math.Floor(pos.X), Math.Floor(pos.Y), Math.Floor(pos.Z)));
					}
				}

				WaypointItem item = new WaypointItem
				                    {
					                    SteamId = (ulong) faction.FactionId,
					                    Name = name,
					                    Text = name,
					                    Position = pos,
					                    WaypointType = WaypointTypes.Neutral,
					                    Leader = faction.IsLeader( playerId )
				                    };
				Waypoints.Instance.Add(item);

				Communication.SendFactionClientMessage(userId, string.Format("/message Server {2} has added the waypoint: {0} at {1} by {2}", item.Name, General.Vector3DToString(item.Position), playerName));
			}
			else
			{
				for (int r = 3; r < 6; r++)
				{
					double test;
					if (!double.TryParse(splits[r], out test))
					{
						Communication.SendPrivateInformation(userId, string.Format("Invalid position information: {0} is invalid", splits[r]));
						return true;
					}
				}

				string add = string.Join( " ", splits.Select( s => s.ToLowerInvariant( ) ) );

				foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
				{
					if (Player.CheckPlayerSameFaction(userId, steamId))
					{
						Communication.SendClientMessage(steamId, string.Format("/waypoint add {0}", add));
					}
				}

				string group = "";
				if (splits.Length == 7)
					group = splits[7];

				WaypointItem item = new WaypointItem
				                    {
					                    SteamId = (ulong) faction.FactionId,
					                    Name = splits[ 0 ],
					                    Text = splits[ 1 ]
				                    };
				WaypointTypes type;
				Enum.TryParse(splits[2], true, out type);
				item.WaypointType = type;
				item.Position = new Vector3D(double.Parse(splits[3]), double.Parse(splits[4]), double.Parse(splits[5]));
				item.Group = group;
				item.Leader = faction.IsLeader(playerId);
				Waypoints.Instance.Add(item);

				Communication.SendFactionClientMessage(userId, string.Format("/message Server {2} has added the waypoint: {0} at {1} by {2}", item.Name, General.Vector3DToString(item.Position), playerName));
			}
			return true;
		}
	}
}
