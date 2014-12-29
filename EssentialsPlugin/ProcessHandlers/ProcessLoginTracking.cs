using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EssentialsPlugin.Utility;
using Sandbox.ModAPI;
using SEModAPIInternal.API.Common;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using VRage.Common.Utils;
using System.Text.RegularExpressions;
using System.Threading;

namespace EssentialsPlugin.ProcessHandler
{
	public class ProcessLoginTracking : ProcessHandlerBase
	{
		public ProcessLoginTracking()
		{
			if (PluginSettings.Instance.LoginEnabled)
			{
				if(Players.Instance.PlayerLogins.Count == 0)
					Players.ProcessServerLogsForLogins(true);
				else
					Players.ProcessServerLogsForLogins();
			}
		}

		public override int GetUpdateResolution()
		{
			return 10000;
		}

		public override void Handle()
		{
			base.Handle();
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			Players.Instance.UpdatePlayerTime(remoteUserId);
			base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerLeft(ulong remoteUserId)
		{
			Players.Instance.UpdatePlayerTime(remoteUserId);
			base.OnPlayerLeft(remoteUserId);
		}
	}
}

