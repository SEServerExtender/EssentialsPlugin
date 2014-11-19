using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminTest : ChatHandlerBase
	{
		private Random m_random = new Random();
		public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin test";
		}

		public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		// admin deletearea x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			CubeGridEntity entity = new CubeGridEntity(new FileInfo(Essentials.PluginPath + "CommRelayTest.sbc"));
			entity.EntityId = BaseEntity.GenerateEntityId();
			//entity.PersistentFlags = MyPersistentEntityFlags2.CastShadows;

			float halfExtent = MyAPIGateway.Entities.WorldSafeHalfExtent();
			if (halfExtent == 0f)
				halfExtent = 900000f;

			entity.PositionAndOrientation = new MyPositionAndOrientation(new Vector3(GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent)), Vector3.Forward, Vector3.Up);
			//entity.PositionAndOrientation = new MyPositionAndOrientation(new Vector3(100, 100, 100), Vector3.Forward, Vector3.Up);
			SectorObjectManager.Instance.AddEntity(entity);

			return true;
		}

		private float GenerateRandomCoord(float halfExtent)
		{
			float result = (m_random.Next(200) + halfExtent) * (m_random.Next(2) == 0 ? -1 : 1);
			return result;
		}

	}
}
