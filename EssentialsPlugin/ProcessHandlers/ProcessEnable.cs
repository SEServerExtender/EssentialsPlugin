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

using Sandbox.Definitions;

namespace EssentialsPlugin.ProcessHandler
{
	public class ProcessEnable : ProcessHandlerBase
	{
		private static DateTime m_lastEnableCheck;
		private static DateTime m_lastDisableCheck;

		public ProcessEnable()
		{
			//EntityManagement.RevealAll();
		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.DynamicConcealEnabled)
				return;

			if (PluginSettings.Instance.DynamicTurretManagmentEnabled)
			{
				if (DateTime.Now - m_lastEnableCheck > TimeSpan.FromSeconds(5))
				{
					EntityManagement.CheckAndEnableTurrets();
					m_lastEnableCheck = DateTime.Now;
				}

				if (DateTime.Now - m_lastDisableCheck > TimeSpan.FromSeconds(30))
				{
					EntityManagement.CheckAndDisableTurrets();
					m_lastDisableCheck = DateTime.Now;
				}
			}

			base.Handle();
		}
	}
}

