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
	public class ProcessProtection : ProcessHandlerBase
	{
		public ProcessProtection()
		{
		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.ProtectedEnabled)
				return;

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities);
			});

			foreach(IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				foreach(ProtectedItem item in PluginSettings.Instance.ProtectedItems)
				{
					if (!item.Enabled)
						continue;

					if (entity.EntityId == item.EntityId)
					{
						ProtectedEntity(entity, item);
					}
				}
			}

			base.Handle();
		}

		private void ProtectedEntity(IMyEntity entity, ProtectedItem item)
		{
			//Logging.WriteLineAndConsole(string.Format("Protecting: {0}", entity.EntityId));
			CubeGridEntity gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);

			int count = 0;
			while (gridEntity.IsLoading)
			{
				if (count >= 20)
					return;

				Thread.Sleep(100);
				count++;
			}

			bool found = false;
			foreach(CubeBlockEntity block in gridEntity.CubeBlocks)
			{
				if (block.IntegrityPercent != item.IntegrityIncrease || block.BuildPercent != item.IntegrityIncrease || block.BoneDamage > 0f)
				{
					found = true;
					block.FixBones(0, 100);
					block.IntegrityPercent = item.IntegrityIncrease;
					block.BuildPercent = item.IntegrityIncrease;
				}
			}

			if(found)
     			Logging.WriteLineAndConsole(string.Format("Repaired Grid: {0}", gridEntity.EntityId));
		}
	}
}

