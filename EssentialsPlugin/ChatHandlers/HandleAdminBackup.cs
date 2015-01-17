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
using Sandbox.Common;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminBackup : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This will force an immediate save game backup using backup settings. Usage: /admin backup";
		}

		public override string GetCommandText()
		{
			return "/admin backup";
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
			Communication.SendPrivateInformation(userId, string.Format("Creating a save game backup ..."));
			Backup.Create(PluginSettings.Instance.BackupBaseDirectory, PluginSettings.Instance.BackupCreateSubDirectories, PluginSettings.Instance.BackupAsteroids, PluginSettings.Instance.BackupEssentials);
			Communication.SendPrivateInformation(userId, string.Format("Save game backup created"));
			return true;
		}
	}
}
