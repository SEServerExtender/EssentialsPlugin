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
            MyPlayerCollection playerCollection = MyAPIGateway.Players as MyPlayerCollection;

	        if ( playerCollection == null )
	            return;

	        int count = 0;
	        foreach ( var identity in playerCollection.GetAllIdentities( ) )
	        {
	            if ( identity.IsDead )
	            {
                    Essentials.Log.Info( $"Removed dead player: {identity.DisplayName}" );
	                count++;
	                playerCollection.RemoveIdentity( identity.IdentityId );
	            }
	        }

	        if ( count != 0 )
	        {
	            Essentials.Log.Info( $"Deleted {count} dead identities." );
	        }
            
			base.Handle();
		}
	}
}
