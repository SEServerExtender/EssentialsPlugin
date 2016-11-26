namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using ParallelTasks;
    using Sandbox.Definitions;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Engine.Voxels;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Character;
    using Sandbox.Game.Replication;
    using Utility;
    using VRage.Game.Entity;
    using VRageMath;

    public class HandleAdminResetAsteroid : ChatHandlerBase
	{

    public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin reset asteroid";
		}
        
        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}
        
        public override bool HandleCommand( ulong userId, string[ ] words )
        {
            var ents = MyEntities.GetEntities( ).ToArray( );
            foreach (var ent in ents)
            {
                var voxel = ent as MyVoxelBase;
                if(voxel == null)
                    continue;

                if(ent is MyPlanet)
                    continue;

                if (string.IsNullOrEmpty(voxel.StorageName)||!(voxel.StorageName.StartsWith( "Asteroid_" ) || voxel.StorageName.StartsWith( "ProcAsteroid-" )))
                    continue;

                var box = voxel.PositionComp.WorldAABB;

                if (ents.Any(x => !(x is MyVoxelBase) && box.Contains( x.PositionComp.WorldAABB ) != ContainmentType.Disjoint))
                    continue;

                Communication.SendVoxelReset( ent.EntityId );

                //Wrapper.BeginGameAction( ( ) =>
                //                         {
                                             Parallel.Start(()=>voxel.Storage.Reset( VRage.Voxels.MyStorageDataTypeFlags.All ));
                                             //MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( ent ) );
                //                         }, null, null );

                Thread.Sleep( 100 );
            }
            return true;
        }

	}

}

