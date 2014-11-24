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
	public class HandleUtilityExportServer : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to export a ship in the game local to the server.  Usage: /utilty export <ship name>";
		}
		public override string GetCommandText()
		{
			return "/utility export server";
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
			if(words.Count() == 0)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			if(PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).Count < 1 && userId != 0)
			{
				Communication.SendPrivateInformation(userId, "Error occurred while processing this command. (1)");
				return true;
			}

			Regex rgx = new Regex("[^a-zA-Z0-9]");
			string userName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
			if (userId == 0)
				userName = "Server";

			if(userName == "")
				return true;
			
			string cleanUserName = rgx.Replace(userName, "").ToLower();

			string modPath = MyFileSystem.ModsPath;
			if (!Directory.Exists(modPath))
			{
				Communication.SendPrivateInformation(userId, "Error occurred while processing this command. (2)");
				return true;
			}

			string exportPath = Path.Combine(modPath, "Exports");
			if (!Directory.Exists(exportPath))
				Directory.CreateDirectory(exportPath);

			string userExportPath = Path.Combine(exportPath, cleanUserName);
			if(!Directory.Exists(userExportPath))
				Directory.CreateDirectory(userExportPath);

			string shipName = string.Join(" ", words);
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
			});

			long playerId = 0;
			if(PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).Count > 0 && userId != 0)
				playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).First();

			bool found = false;
			foreach (IMyEntity entity in entities)
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;

				if (grid.DisplayName.ToLower().Contains(shipName.ToLower()))
				{
					Console.WriteLine(string.Format("Here: '{0}'", grid.DisplayName));
					if (grid.BigOwners.Contains(playerId) || PlayerManager.Instance.IsUserAdmin(userId) || userId == 0)
					{
						string cleanShipName = rgx.Replace(shipName, "");
						CubeGridEntity gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
						gridEntity.Export(new FileInfo(string.Format("{0}\\{1}.sbc", userExportPath, cleanShipName)));
						Communication.SendPrivateInformation(userId, string.Format("Exported the ship '{0}' to a file on the server.", shipName));
						found = true;
						break;
					}
				}
			}

			if (!found)
				Communication.SendPrivateInformation(userId, string.Format("Unable to find a ship by the name '{0}' that exists or belongs to you!", shipName));
				

			return true;
		}
	}
}
