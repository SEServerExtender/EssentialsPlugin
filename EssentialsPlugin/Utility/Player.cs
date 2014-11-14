using System;
using System.Linq;
using System.Threading;
using System.IO;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.Utility
{
	public static class Player
	{
		public static bool Move(string userName, Vector3D position)
		{
			CharacterEntity charEntity = SectorObjectManager.Instance.GetTypedInternalData<CharacterEntity>().Where(x => x.DisplayName.ToLower() == userName.ToLower() && x.Health > 0).First();
			CubeGridEntity gridEntity = new CubeGridEntity(new FileInfo(Essentials.PluginPath + "MovePlayer.sbc"));
			gridEntity.EntityId = BaseEntity.GenerateEntityId();
			foreach (MyObjectBuilder_CubeBlock block in gridEntity.BaseCubeBlocks)
			{
				// set ownership
				if (block is MyObjectBuilder_Cockpit)
				{
					MyObjectBuilder_Cockpit cockpit = (MyObjectBuilder_Cockpit)block;
					cockpit.Pilot = (MyObjectBuilder_Character)charEntity.Export();
				}
			}

			gridEntity.PositionAndOrientation = new MyPositionAndOrientation(position, Vector3.Forward, Vector3.Up);

			Wrapper.GameAction(() =>
			{
				MyObjectBuilder_EntityBase baseEntity = gridEntity.Export();
				IMyEntity entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(baseEntity);
				Type someManager = SandboxGameAssemblyWrapper.Instance.GetAssemblyType(SectorObjectManager.EntityBaseNetManagerNamespace, SectorObjectManager.EntityBaseNetManagerClass);
				Wrapper.InvokeStaticMethod(someManager, SectorObjectManager.EntityBaseNetManagerSendEntity, new object[] { entity.GetObjectBuilder() });
				gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
			});


			int count = 0;
			while (gridEntity.IsLoading)
			{
				Thread.Sleep(100);
				count++;
				if (count > 10)
					break;
			}

			if (gridEntity.IsLoading)
				return false;

			foreach (CubeBlockEntity block in gridEntity.CubeBlocks)
			{
				if (block is CockpitEntity)
				{
					block.IntegrityPercent = 0.1f;
				}
			}

			gridEntity.Dispose();
			return true;
		}
	}
}
