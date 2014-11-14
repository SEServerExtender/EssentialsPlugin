using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssentialsPlugin.Utility;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ProcessHandler
{
	class ProcessInfo : ProcessHandlerBase
	{
		public override int GetUpdateResolution()
		{
			return 5000;
		}

		public override void Handle()
		{
			if (PluginSettings.Instance.InformationEnabled)
			{
				foreach (InformationItem item in PluginSettings.Instance.InformationItems)
				{
					if (!item.Enabled)
						continue;

					if (item.SubText == "")
						continue;

					if (item.IntervalSeconds < 1)
						continue;

					if (DateTime.Now - item.lastUpdate > TimeSpan.FromSeconds(item.IntervalSeconds))
					{
						item.lastUpdate = DateTime.Now;

						if (item.SubText.ToLower().Contains("%name%"))
						{
							foreach(ulong userId in PlayerManager.Instance.ConnectedPlayers)
							{
								string userName = PlayerMap.Instance.GetPlayerNameFromSteamId(userId);
								Communication.SendPrivateInformation(userId, item.SubText.Replace("%name%", userName));
							}							
						}
						else
						{
							Communication.SendPublicInformation(item.SubText);
						}
					}
				}
			}

			base.Handle();
		}
	}
}
