namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Net;
	using System.Threading;
	using System.Windows.Forms;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox;
	using Sandbox.Game.Multiplayer;
	using Sandbox.Game.World;
	using Sandbox.ModAPI;
	using SEModAPIExtensions.API;
	using SEModAPIInternal.API.Common;
	using SteamSDK;
	using VRage.Game.ModAPI;
	using VRage.Library.Collections;
	using VRage.ModAPI;
	using VRageMath;

	class ProcessPlayerCleanup : ProcessHandlerBase
	{
		public override int GetUpdateResolution()
		{
			return 3600000;
		}
        
	    public override void Handle()
	    {
            Essentials.Log.Info( "Cleaning up identities." );
            MyPlayerCollection playerCollection = MyAPIGateway.Players as MyPlayerCollection;

            if ( playerCollection == null )
                return;

            HashSet<long> owners = new HashSet<long>( );
            HashSet<MyIdentity> toRemove = new HashSet<MyIdentity>( );

            HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
            Wrapper.GameAction( ( ) => MyAPIGateway.Entities.GetEntities( entities, x => x is IMyCubeGrid ) );

            foreach ( IMyEntity entity in entities )
            {
                var grid = entity as IMyCubeGrid;
                if ( grid == null )
                    continue;

                foreach ( long owner in grid.SmallOwners )
                {
                    owners.Add( owner );
                }
            }

            var myIdentities = playerCollection.GetAllIdentities( );

            foreach ( MyIdentity identity in myIdentities )
            {
                if ( !identity.IsDead )
                    continue;

                if ( !owners.Contains( identity.IdentityId ) )
                    toRemove.Add( identity );
            }

            int count = toRemove.Count;
            Wrapper.GameAction( ( ) =>
                                {
                                    foreach ( MyIdentity identity in toRemove )
                                    {
                                        Essentials.Log.Info( $"Removed identity {identity.DisplayName}: {identity.IdentityId}" );
                                        playerCollection.RemoveIdentity( identity.IdentityId );
                                    }
                                } );
            Essentials.Log.Info( $"Removed {count} identities." );
            base.Handle();
		}
	}
}
