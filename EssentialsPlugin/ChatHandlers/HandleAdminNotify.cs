using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

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
	public class HandleAdminNotify : ChatHandlerBase
	{
		private Random m_random = new Random();
		public override string GetHelp()
		{
			return "This will broadcast a notification to all users.  Usage: /admin notify <message>";
		}

		public override string GetCommandText()
		{
			return "/admin notify";
		}

		public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		// admin deletearea x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			string message = string.Join(" ", words);
			foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
			{
				Communication.SendFactionClientMessage(steamId, string.Format("/notification {0}", message));
			}

			return true;
		}
	}
}
