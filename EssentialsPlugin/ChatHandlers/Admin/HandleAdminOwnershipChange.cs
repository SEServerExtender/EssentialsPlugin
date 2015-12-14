namespace EssentialsPlugin.ChatHandlers
{
	using System.Linq;
	using EssentialsPlugin.Utility;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
	using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

	public class HandleAdminOwnershipChange : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to change the ownership of a ship.  Usage: /admin ownership change <entityId> <PlayerName>";
		}
		public override string GetCommandText()
		{
			return "/admin ownership change";
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
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		// /admin ownership change name gridId
		public override bool HandleCommand(ulong userId, string[] words)
		{
			string name = words[0].ToLower();
			long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId(PlayerMap.Instance.GetSteamIdFromPlayerName(name, true)).First();
			string gridId = words[1].ToLower();

			long gridEntityId = 0;
			if (!long.TryParse(gridId, out gridEntityId))
			{
				Communication.SendPrivateInformation(userId, string.Format("Invalid EntityID entered: {0}", gridId));
				return true;
			}

			CubeGridEntity grid = (CubeGridEntity)GameEntityManager.GetEntity(gridEntityId);

			for (int r = 0; r < grid.CubeBlocks.Count; r++)
			{
				CubeBlockEntity block = (CubeBlockEntity)grid.CubeBlocks[r];

				if (block is TerminalBlockEntity)
				{
					if (block.Owner != playerId && block.Owner != 0)
					{
						Communication.SendPrivateInformation(userId, string.Format("Changing ownership of {0} - {1}", block.Name, playerId));
						block.Owner = playerId;
					}
				}
			}

			return true;
		}
	}
}
