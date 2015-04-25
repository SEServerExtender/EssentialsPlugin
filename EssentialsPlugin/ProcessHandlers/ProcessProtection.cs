namespace EssentialsPlugin.ProcessHandlers
{
	using System.Collections.Generic;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;

	public class ProcessProtection : ProcessHandlerBase
	{
		public ProcessProtection( )
		{
		}

		public override int GetUpdateResolution( )
		{
			return 1000;
		}

		public override void Handle( )
		{
			if ( !PluginSettings.Instance.ProtectedEnabled )
				return;

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
			Wrapper.GameAction( ( ) =>
			{
				MyAPIGateway.Entities.GetEntities( entities );
			} );

			foreach ( IMyEntity entity in entities )
			{
				if ( !( entity is IMyCubeGrid ) )
					continue;

				foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
				{
					if ( !item.Enabled )
						continue;

					if ( entity.EntityId == item.EntityId )
					{
						ProtectedEntity( entity );
					}
				}
			}

			base.Handle( );
		}

		private static void ProtectedEntity( IMyEntity entity )
		{
			if ( !entity.InScene )
				return;
			Essentials.Log.Info( "Protecting: {0}", entity.EntityId );
			IMyCubeGrid cubeGridEntity = entity as IMyCubeGrid;
			if ( cubeGridEntity == null )
				return;

			List<IMySlimBlock> blocks = new List<IMySlimBlock>( );
			cubeGridEntity.GetBlocks( blocks, block => true );
			bool found = false;
			foreach ( IMySlimBlock block in blocks )
			{
				MyObjectBuilder_CubeBlock objectBuilderCubeBlock = block.GetObjectBuilder( );
				if ( objectBuilderCubeBlock.BuildPercent < 1f )
				{
					found = true;
					objectBuilderCubeBlock.BuildPercent = 1f;
				}
				if ( objectBuilderCubeBlock.IntegrityPercent < 1f )
				{
					found = true;
					objectBuilderCubeBlock.IntegrityPercent = 1f;
				}
				if ( objectBuilderCubeBlock.DeformationRatio > 0f )
				{
					found = true;
					objectBuilderCubeBlock.DeformationRatio = 0;
				}

			}

			if ( found )
				Essentials.Log.Info( "Repaired Grid: {0}", entity.EntityId );
		}
	}
}

