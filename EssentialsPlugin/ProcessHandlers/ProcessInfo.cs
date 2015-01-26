using System;
using System.Linq;
using SEModAPIInternal.API.Common;
using EssentialsPlugin.Utility;
using EssentialsPlugin.Settings;

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
								string subText = item.SubText.Replace("%name%", userName).Split(new string[] { "\n" }, 2, StringSplitOptions.None)[item.position];

								item.position++;
								if (item.position >= item.SubText.Replace("%name%", userName).Split(new string[] { "\n" }, 2, StringSplitOptions.None).Count())
									item.position = 0;

								Communication.SendPrivateInformation(userId, subText);
							}							
						}
						else
						{
							string subText = item.SubText.Split(new string[] { "\n" }, StringSplitOptions.None)[item.position];
							item.position++;
							if (item.position >= item.SubText.Split(new string[] { "\n" }, StringSplitOptions.None).Count())
								item.position = 0;

							Communication.SendPublicInformation(subText);
						}
					}
				}
			}

			base.Handle();
		}
	}
}
