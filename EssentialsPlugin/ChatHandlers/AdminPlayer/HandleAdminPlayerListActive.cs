using System;
using System.Collections.Generic;
using System.Linq;
using EssentialsPlugin.Utility;
using Sandbox.Common.ObjectBuilders;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminPlayerListActive : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "";
		}
		public override string GetCommandText()
		{
			return "/admin player list active";
		}

		public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		// /admin movefrom x y z x y z radius
		public override bool HandleCommand( ulong userId, string command )
		{
			string[ ] words = command.Split( ' ' );
			if ( words.Count( ) != 1 && words.Count( ) != 0 )
				return false;

			if (words.Count() != 1)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			int days = -1;
			if (!int.TryParse(words[0], out days) || days < 0)
			{
				Communication.SendPrivateInformation(userId, string.Format("Invalid argument.  Days argument must be an integer that is 0 or greater."));
				return true;
			}

			List<PlayerItem> players = Players.Instance.GetPlayers(x => x.LastLogin > DateTime.Now.AddDays(days * -1) && PlayerMap.Instance.GetPlayerIdsFromSteamId(x.SteamId).FirstOrDefault() > 0);
			try
			{
				foreach (PlayerItem player in players.OrderByDescending(x => x.LastLogin).ToList())
				{
					long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId(player.SteamId).FirstOrDefault();
					if (playerId > 0)
					{
						MyObjectBuilder_Checkpoint.PlayerItem item = PlayerMap.Instance.GetPlayerItemFromPlayerId(playerId);
						Communication.SendPrivateInformation(userId, string.Format("{0} ({1}) last logged in {2}", item.Name, item.PlayerId, player.LastLogin.ToString("g")));
					}
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(string.Format("Error: {0}", ex.ToString()));
			}

			Communication.SendPrivateInformation(userId, string.Format("Found {0} players that have logged in within the last {1} days", players.Count, days));
			return true;
		}
	}
}
