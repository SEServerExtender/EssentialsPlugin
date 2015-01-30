using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;
using VRage.Common;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminStatistics : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Get performance statistics of the server.  Usage: /admin statistics";
		}

		public override string GetCommandText()
		{
			return "/admin statistics";
		}

		public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			StringBuilder sb = new StringBuilder();
			Sandbox.Stats.Generic.WriteTo(sb);
			Communication.SendPrivateInformation(userId, sb.ToString());
			return true;
		}
	}
}

