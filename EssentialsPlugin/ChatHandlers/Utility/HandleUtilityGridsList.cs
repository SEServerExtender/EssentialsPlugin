using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;
using VRage.Common.Utils;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleUtilityGridsList : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This will list all your ships by name on the server.  Usage: /utility grids list";
		}
		public override string GetCommandText()
		{
			return "/utility grids list";
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
			int page = 1;
			if(words.Length > 0)
			{
				int.TryParse(words[0], out page);
				if (page < 1)
					page = 1;
			}

			if(PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).Count < 1 && userId != 0)
			{
				Communication.SendPrivateInformation(userId, "Error occurred while processing this command. (1)");
				return true;
			}

			long playerId = 0;
			if (PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).Count > 0)
				playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).First();

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
			});

			string result = "";
			int count = 0;
			foreach (IMyEntity entity in entities)
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;

				if (grid.BigOwners.Contains(playerId) || userId == 0)
				{
					if(result != "")
						result += "\r\n";

					result += string.Format("Ship '{0}' ({1})", grid.DisplayName, grid.EntityId);
					count++;
				}
			}
				
			string[] resultLines = result.Split(new string[] {"\r\n"}, StringSplitOptions.None);

			int pages = ((resultLines.Length - 1) / 7) + 1;

			if (page > pages)
				page = pages;

			string output = "";
			for (int r = ((page - 1) * 7); r < resultLines.Length && r < ((page) * 7); r++ )
			{
				string line = resultLines[r];
				if (output != "")
					output += "\r\n";

				output += line;
			}
			Communication.SendPrivateInformation(userId, output);

			Communication.SendPrivateInformation(userId, string.Format("Displaying page {0} of {1} - {2} grids", page, pages, count));
			return true;
		}
	}
}
