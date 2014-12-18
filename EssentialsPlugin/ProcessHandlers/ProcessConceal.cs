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
	public class ProcessConceal : ProcessHandlerBase
	{
		private static DateTime m_lastConcealCheck;
		private static DateTime m_lastRevealCheck;

		public ProcessConceal()
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

			if (DateTime.Now - m_lastRevealCheck > TimeSpan.FromSeconds(5))
			{
				//Logging.WriteLineAndConsole("CheckAndRevealEntities");
				EntityManagement.CheckAndRevealEntities();
				m_lastRevealCheck = DateTime.Now;
			}

			if (DateTime.Now - m_lastConcealCheck > TimeSpan.FromSeconds(30))
			{
				//Logging.WriteLineAndConsole("CheckAndConcealEntities");
				EntityManagement.CheckAndConcealEntities();
				m_lastConcealCheck = DateTime.Now;
			}

			base.Handle();
		}
	}
}

