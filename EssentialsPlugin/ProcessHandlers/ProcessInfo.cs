using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssentialsPlugin.Utility;

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
						Communication.SendPublicInformation(item.SubText);
					}
				}
			}

			base.Handle();
		}
	}
}
