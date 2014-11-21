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
	public class CleanupItem
	{
		public long entityId;
		public DateTime addedTime;
		public Int32 secondsAfterAdding;
	}

	public class Cleanup
	{
		private static Cleanup m_instance;
		private List<CleanupItem> m_items;

		public static Cleanup Instance
		{
			get 			
			{
				if (m_instance == null)
					m_instance = new Cleanup();

				return m_instance; 
			}
		}

		public Cleanup()
		{
			m_instance = this;
			m_items = new List<CleanupItem>();
		}

		/*
		public void Add(CubeGridEntity entity)		
		{
			CleanupItem item = new CleanupItem();
			item.entityToCleanup = entity;
			item.addedTime = DateTime.Now;
			item.secondsAfterAdding = 5;

			lock (m_items)
				m_items.Add(item);
		}
		 */ 

		public void Add(long entityId)
		{
			CleanupItem item = new CleanupItem();
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
					CleanupItem item = m_items[r];

					if (DateTime.Now - item.addedTime > TimeSpan.FromSeconds(item.secondsAfterAdding))
					{
						try
						{
							IMyEntity entity = null;
							Wrapper.GameAction(() =>
							{
								entity = MyAPIGateway.Entities.GetEntityById(item.entityId);
							});

							if(entity != null)
							{
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
