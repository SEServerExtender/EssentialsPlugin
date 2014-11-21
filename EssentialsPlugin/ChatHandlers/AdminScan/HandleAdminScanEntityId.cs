using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
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

			if(words.Count() == 0)
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
