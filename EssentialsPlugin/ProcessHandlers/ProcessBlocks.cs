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

using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Entity;

using Sandbox.Common.ObjectBuilders;

using EssentialsPlugin.Settings;
using EssentialsPlugin.EntityManagers;

using Sandbox.Definitions;

namespace EssentialsPlugin.ProcessHandler
{
	public class ProcessBlocks : ProcessHandlerBase
	{
		private static DateTime _lastEnableCheck;
		private static DateTime _lastDisableCheck;

		public ProcessBlocks()
		{

		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (PluginSettings.Instance.DynamicBlockManagementEnabled)
			{
				if (DateTime.Now - _lastEnableCheck > TimeSpan.FromSeconds(6))
				{
					BlockManagement.Instance.CheckAndEnableBlocks();
					_lastEnableCheck = DateTime.Now;
				}

				if (DateTime.Now - _lastDisableCheck > TimeSpan.FromSeconds(30))
				{
					BlockManagement.Instance.CheckAndDisableBlocks();
					_lastDisableCheck = DateTime.Now;
				}
			}

			base.Handle();
		}

		public override void OnSectorSaved()
		{
			if (!PluginSettings.Instance.DynamicBlockManagementEnabled)
				return;

			BlockManagement.Instance.Save();
			base.OnSectorSaved();
		}
	}
}

