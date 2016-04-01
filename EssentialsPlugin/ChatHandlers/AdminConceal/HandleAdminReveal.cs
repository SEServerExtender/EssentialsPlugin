namespace EssentialsPlugin.ChatHandlers.AdminConceal
{
    using System.Collections.Generic;
    using System.Linq;
    using EntityManagers;
    using EssentialsPlugin.Utility;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Replication;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Entity;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;

    public class HandleAdminReveal : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to reveal concealed grids.  Usage: /admin reveal (force) - This command without 'force' only shows you how many would be revealed.";
		}

		public override string GetCommandText()
		{
			return "/admin reveal";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "Admin Reveal";
            DialogItem.content =
                " This command allows you to reveal concealed grids.|" +
                "Usage: /admin reveal (force) - this command without 'force' only show you how many grids would be revealed.||" +
                "This command will run when concealment is disabled, and respects the update time setting.";
            DialogItem.buttonText = "close";
            return DialogItem;
        }        

        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
        {
            bool force = words.FirstOrDefault( x => x.ToLower( ) == "force" ) != null;
            bool now = words.Count( ) > 1 && words[1] == "now";

            if ( force && !now )
                EntityManagement.RevealAll( );

            else
            {
                HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
                Wrapper.GameAction( ( ) => MyAPIGateway.Entities.GetEntities( entities ) );

                List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>( );
                int count = 0;

                if ( !now )
                {
                    Wrapper.GameAction( ( ) =>
                     {
                         foreach ( IMyEntity entity in entities )
                         {
                             if ( entity.InScene )
                                 continue;

                             if ( !(entity is IMyCubeGrid) )
                                 continue;
                             
                         //if ( now )
                         //    EntityManagement.RevealEntity( new KeyValuePair<IMyEntity, string>( entity, "Immediate force reveal" ) );

                         count++;
                         }
                     } );
                }

                if ( now )
                {
                    Wrapper.GameAction( ( ) =>
                    {
                        foreach ( IMyEntity entity in entities )
                        {
                            if ( entity.InScene )
                                continue;

                            if ( !(entity is MyCubeGrid) )
                                continue;
                            
                            count++;

                            MyCubeGrid grid = (MyCubeGrid)entity;
                            MyObjectBuilder_EntityBase builder = grid.GetObjectBuilder( );

                            long ownerId = 0;
                            string ownerName = "";
                            if ( grid.BigOwners.Count > 0 )
                            {
                                ownerId = grid.BigOwners.First( );
                                ownerName = PlayerMap.Instance.GetPlayerItemFromPlayerId( ownerId ).Name;
                            }


                            entity.PersistentFlags = (MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.CastShadows);
                            MyAPIGateway.Entities.RemapObjectBuilder( builder );

                            //Log.Info("Conceal", string.Format("Force Revealing - Id: {0} -> {4} Display: {1} OwnerId: {2} OwnerName: {3}", entity.EntityId, entity.DisplayName.Replace("\r", "").Replace("\n", ""), ownerId, ownerName, builder.EntityId));
                            Log.Info( "Revealing" );
                            IMyEntity newEntity = MyAPIGateway.Entities.CreateFromObjectBuilder( builder );
                            entity.InScene = true;
                            entity.OnAddedToScene( entity );
                            entity.Close( );
                            MyAPIGateway.Entities.AddEntity( newEntity );
                            MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( newEntity ) );
                            entity.Stop( );
                        }
                    } );
                }

                if ( !now )
                   Communication.SendPrivateInformation( userId,
                                                         $"Command would reveal {count} grids.  Type /admin reveal force to reveal them." );

                else
                    Communication.SendPrivateInformation(userId, $"Command revealed {count} grids." );
            }
            return true;
        }
	}
}
