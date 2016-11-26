namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System.Linq;
    using ParallelTasks;
    using Sandbox.Definitions;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Engine.Voxels;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Character;
    using Sandbox.Game.Replication;
    using Utility;
    public class HandleAdminResetPlanet : ChatHandlerBase
	{

    public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin reset planet";
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

                if(!(ent is MyPlanet))
                    continue;

                Communication.SendVoxelReset( ent.EntityId );

                //Wrapper.BeginGameAction( ( ) =>
                //                         {
                                             Parallel.Start(()=>voxel.Storage.Reset( VRage.Voxels.MyStorageDataTypeFlags.All ));
                                             //MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( ent ) );
                //                         }, null, null );
            }
            return true;
        }

	}

}

