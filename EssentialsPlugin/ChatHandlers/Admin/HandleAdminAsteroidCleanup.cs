namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System;
    using System.Collections.Generic;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.Entities.Character;
    using Sandbox.ModAPI;
    using Utility;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.Network;
    using VRageMath;

    public class HandleAdminAsteroidCleanup : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "Removes asteroids without a player or ship within 1km. Usage: /admin asteroid cleanup";
        }

        public override string GetCommandText( )
        {
            return "/admin asteroid cleanup";
        }

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem dialogItem = new Communication.ServerDialogItem
                                                        {
                                                            title = "Help",
                                                            header = "",
                                                            content = GetHelp( ),
                                                            buttonText = "close"
                                                        };
            return dialogItem;
        }

        public override bool IsAdminCommand( )
        {
            return true;
        }

        public override bool AllowedInConsole( )
        {
            return true;
        }

        private static HashSet<Vector3D> entityPositions = new HashSet<Vector3D>( );
        private static Dictionary<Vector3D, IMyVoxelMap> asteroidPositions = new Dictionary<Vector3D, IMyVoxelMap>( );
        private static HashSet<IMyVoxelMap> toRemove = new HashSet<IMyVoxelMap>( );

        public override bool HandleCommand( ulong userId, string[] words )
        {
            Essentials.Log.Info( "Asteroid cleanup" );
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
            Wrapper.GameAction( ( ) =>
                                {
                                    MyAPIGateway.Entities.GetEntities( entities );
                                    foreach ( IMyEntity entity in entities )
                                    {
                                        if ( entity == null )
                                            continue;

                                        if ( entity is IMyVoxelMap )
                                            asteroidPositions.Add( entity.PositionComp.GetPosition( ), (IMyVoxelMap)entity );
                                        else
                                            entityPositions.Add( entity.PositionComp.GetPosition( ) );
                                    }
                                } );
            //TODO: Use a thread pool to speed this up?
            DateTime profile = DateTime.Now;
            Communication.SendPrivateInformation( userId, $"Found {asteroidPositions.Count} asteroids." );
            foreach ( var asteroid in asteroidPositions )
            {
                bool found = false;
                BoundingSphereD bound = new BoundingSphereD( asteroid.Key, 1000 );
                foreach ( Vector3D checkPosition in entityPositions )
                {
                    if ( bound.Contains( checkPosition ) == ContainmentType.Contains )
                    {
                        found = true;
                        break;
                    }
                }

                if ( !found )
                    toRemove.Add( asteroid.Value );
            }
            Communication.SendPrivateInformation( userId, $"Found {toRemove.Count} asteroids to remove." );
            int count = 0;
            foreach ( IMyVoxelMap asteroid in toRemove )
            {
                if ( asteroid == null || asteroid.Closed )
                    continue;

                count++;

                Wrapper.GameAction( ( ) => asteroid.Close( ) );
            }
            Communication.SendPrivateInformation( userId, $"Removed {count} asteroids." );
            Essentials.Log.Info( "Asteroid cleanup elapsed time: " + (DateTime.Now - profile) );
            return true;
        }
    }
}

