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
			bool dialog = false;
			if(words.Length > 0)
			{
				if (words[0] == "dialog")
					dialog = true;

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
			List<IMyPlayer> players = new List<IMyPlayer>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
				MyAPIGateway.Players.GetPlayers(players);
			});

			IMyPlayer player = players.FirstOrDefault(x => x.SteamUserId == userId);			

			string result = "";
			int count = 0;
			foreach (IMyEntity entity in entities)
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;

				MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);
				if (grid == null)
					continue;

				if (CubeGrids.GetAllOwners(gridBuilder).Contains(playerId))
				{
					if(result != "")
						result += "\r\n";

					if(CubeGrids.IsFullOwner(gridBuilder, playerId, player) && !dialog)
						result += string.Format("Grid '{0}' at {2}", grid.DisplayName, grid.EntityId, ShowCoordinates(grid.GetPosition()));
					else if (CubeGrids.IsFullOwner(gridBuilder, playerId, player) && dialog)
						result += string.Format("{0} - {1} - {2}bl - {3}", grid.DisplayName, ShowCoordinates(grid.GetPosition()), gridBuilder.CubeBlocks.Count, gridBuilder.GridSizeEnum);
					else
						result += string.Format("Grid '{0}'", grid.DisplayName, grid.EntityId);

					count++;
				}
			}

			if (dialog)
			{
				Communication.SendClientMessage(userId, string.Format("/dialog \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"", "User Grid List", "Ships / Stations you ", "own:", result.Replace("\r\n", "|"), "OK"));
				return true;
			}

			string[] resultLines = result.Split(new string[] {"\r\n"}, StringSplitOptions.None);

			int pages = ((resultLines.Length - 1) / 6) + 1;

			if (page > pages)
				page = pages;

			string output = "";
			for (int r = ((page - 1) * 6); r < resultLines.Length && r < ((page) * 6); r++ )
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

		private string ShowCoordinates(Vector3D pos)
		{
			if (PluginSettings.Instance.ServerUtilityGridsShowCoords)
				return General.Vector3DToString(pos);
			else
				return "(Unknown Postion)";
		}
	}
}
