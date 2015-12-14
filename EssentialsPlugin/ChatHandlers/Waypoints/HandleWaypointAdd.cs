namespace EssentialsPlugin.ChatHandlers.Waypoints
{
	using System;
	using System.Collections.Generic;
	using EssentialsPlugin.Utility;
	using SEModAPIInternal.API.Common;
	using VRage.ModAPI;
	using VRageMath;

	public class HandleWaypointAdd : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Creates a personal waypoint.  Only you can see it.  Usage: /waypoint add \"waypoint name\" \"waypoint text\" Neutral | Allied | Enemy X Y Z.  Example: /waypoint add MyWayPoint MyWaypoint Neutral 1000 1000 1000";
		}

		public override string GetCommandText()
		{
			return "/waypoint add";
		}

		public override string[] GetMultipleCommandText()
		{
			return new string[] { "/waypoint add", "/wp add" };
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\""+GetHelp()+"\" \"close\" ";
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

			if (words.Length != 1 && words.Length != 5 && words.Length != 6 && words.Length != 7)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			List<WaypointItem> items = Waypoints.Instance.Get(userId);
			if (PluginSettings.Instance.WaypointsMaxPerPlayer > 0 && items.Count >= PluginSettings.Instance.WaypointsMaxPerPlayer)
			{
				Communication.SendPrivateInformation(userId, string.Format("Waypoint limit has been reached.  You may only have {0} waypoints at a time on this server.  Please remove some waypoints in order to add new ones.", PluginSettings.Instance.WaypointsMaxPerPlayer));
				return true;
			}

			if (words.Length == 1)
			{
				long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(userId);
				IMyEntity playerEntity = Player.FindControlledEntity(playerId);
				if(playerEntity == null)
				{
					Communication.SendPrivateInformation(userId, string.Format("Can't find your position"));
					return true;
				}

				Vector3D pos = playerEntity.GetPosition();
				string name = words[0];

				Communication.SendClientMessage(userId, string.Format("/waypoint add '{0}' '{0}' Neutral {1} {2} {3}", name, Math.Floor(pos.X), Math.Floor(pos.Y), Math.Floor(pos.Z)));

				WaypointItem item = new WaypointItem();
				item.SteamId = userId;
				item.Name = name;
				item.Text = name;
				item.Position = pos;
				item.WaypointType = WaypointTypes.Neutral;
				Waypoints.Instance.Add(item);

				Communication.SendPrivateInformation(userId, string.Format("Waypoint added: '{0}' at {1}", item.Name, General.Vector3DToString(item.Position)));
			}
			else
			{
				int len = 5;
				if (words.Length > 5)
					len = 6;

				for (int r = len - 3; r < len; r++)
				{
					double test = 0d;
					if (!double.TryParse(words[r], out test))
					{
						Communication.SendPrivateInformation(userId, string.Format("Invalid position information: {0} is invalid", words[r]));
						return true;
					}
				}

				string add = "";
				foreach (string word in words)
				{
					if (add == "")
						add += word.ToLower();
					else
						add += " " + word;
				}

				Communication.SendClientMessage(userId, string.Format("/waypoint add {0}", add));

				string group = "";
				if (words.Length == 7)
					group = words[7];

				WaypointItem item = new WaypointItem();
				item.SteamId = userId;
				item.Name = words[0];

				int diff = words.Length > 5 ? 1 : 0;
				item.Text = words[diff];
				WaypointTypes type = WaypointTypes.Neutral;
				Enum.TryParse<WaypointTypes>(words[diff + 1], true, out type);
				item.WaypointType = type;
				item.Position = new Vector3D(double.Parse(words[diff + 2]), double.Parse(words[diff + 3]), double.Parse(words[diff + 4]));
				item.Group = group;
				Waypoints.Instance.Add(item);

				Communication.SendPrivateInformation(userId, string.Format("Waypoint added: '{0}' at {1}", item.Name, General.Vector3DToString(item.Position)));
			}
			return true;
		}
	}
}
