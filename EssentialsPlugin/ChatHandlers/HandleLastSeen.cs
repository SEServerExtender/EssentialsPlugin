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
	public class HandleLastSeen : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Shows the last time a user has been seen.  Usage: /lastseen \"<playername>\"";
		}

		public override string GetCommandText()
		{
			return "/lastseen";
		}

		public override bool IsAdminCommand()
		{
			return false;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			string[] splits = General.SplitString(string.Join(" ", words));
			if (splits.Count() != 1)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			string userName = splits[0];
			ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerName(userName, true);
			if (steamId == 0)
			{
				Communication.SendPrivateInformation(userId, string.Format("Unable to find player '{0}", userName));
				return true;
			}

			long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(steamId);			
			PlayerItem item = Players.Instance.GetPlayerById(playerId);
			if (item != null)
			{
				Communication.SendPrivateInformation(userId, string.Format("Player '{0}' last seen: {1}", PlayerMap.Instance.GetPlayerItemFromPlayerId(playerId).Name, item.LastLogin.ToString("g")));
			}
			else
			{
				Communication.SendPrivateInformation(userId, string.Format("No login information for user '{0}'", userName));
			}

			return true;
		}
	}
}
