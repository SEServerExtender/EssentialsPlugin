namespace EssentialsPlugin.ChatHandlers
{
	using System.Linq;
	using EssentialsPlugin.Utility;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;

	public class HandleAdminScanEntityId : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to scan a grid by EntityId and get it's display name and Extender name.  Usage: /admin scan entityid <entityId>";
		}
		public override string GetCommandText()
		{
			return "/admin scan entityid";
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

		// admin nobeacon scan
		public override bool HandleCommand(ulong userId, string[] words)
		{
			if (words.Count() > 1)
				return false;

			if(!words.Any())
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			long entityId = 0;
			if(!long.TryParse(words[0], out entityId))
			{
				Communication.SendPrivateInformation(userId, string.Format("The value '{0}' is not a valid entityId", words[0]));
				return true;
			}

			CubeGridEntity entity = (CubeGridEntity)GameEntityManager.GetEntity(entityId);
			Communication.SendPrivateInformation(userId, string.Format("Entity {0} DisplayName: {1} FullName: {2}", entityId, entity.DisplayName, entity.Name));

			return true;
		}
	}
}
