using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEModAPIInternal.API.Common;

using EssentialsPlugin.Utility;
using EssentialsPlugin.Settings;
using EssentialsPlugin.GameModes;

namespace EssentialsPlugin.ProcessHandler
{
	class ProcessConquest : ProcessHandlerBase
	{
		public override int GetUpdateResolution()
		{
			return 30000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.GameModeConquestEnabled)
				return;

			Conquest.Process();
			base.Handle();
		}
	}
}
