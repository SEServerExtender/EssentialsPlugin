using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

namespace EssentialsPlugin.Utility
{
	public class TimedEntityCleanupItem
	{
		public long entityId;
		public DateTime addedTime;
		public Int32 secondsAfterAdding;
	}

	public class TimedEntityCleanup
	{
		private static TimedEntityCleanup m_instance;
		private List<TimedEntityCleanupItem> m_items;

		public static TimedEntityCleanup Instance
		{
			get 			
			{
				if (m_instance == null)
					m_instance = new TimedEntityCleanup();

				return m_instance; 
			}
		}

		public TimedEntityCleanup()
		{
			m_instance = this;
			m_items = new List<TimedEntityCleanupItem>();
		}

		public void Add(long entityId)
		{
			TimedEntityCleanupItem item = new TimedEntityCleanupItem();
			item.addedTime = DateTime.Now;
			item.secondsAfterAdding = 5;
			item.entityId = entityId;

			lock (m_items)
				m_items.Add(item);
		}

		public void Process()
		{
			lock (m_items)
			{
				if (m_items.Count() < 1)
					return;

				for (int r = m_items.Count() - 1; r >= 0; r--)
				{
					TimedEntityCleanupItem item = m_items[r];

					if (DateTime.Now - item.addedTime > TimeSpan.FromSeconds(item.secondsAfterAdding))
					{
						try
						{
							IMyEntity entity = null;
							Wrapper.GameAction(() =>
							{
								MyAPIGateway.Entities.TryGetEntityById(item.entityId, out entity);
							});

							if(entity != null)
							{
								MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder((IMyCubeGrid)entity);
								if (gridBuilder == null)
									continue;

								CubeGridEntity entityToDispose = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
								entityToDispose.Dispose();
							}
							else
								Logging.WriteLineAndConsole("Entity is null");
						}
						catch (Exception ex)
						{
							Logging.WriteLineAndConsole(string.Format("Dispose Error: {0}", ex.ToString()));
						}

						m_items.RemoveAt(r);
					}
				}
			}
		}
	}
}
