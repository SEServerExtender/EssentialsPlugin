namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Sandbox.Game.World;
	using Sandbox.ModAPI;
	using Settings;
	using Utility;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;
	using VRageMath;

    class ProcessSpeed : ProcessHandlerBase
	{
        public static Dictionary<long, Tuple<float,DateTime>> SpeedPlayers = new Dictionary<long, Tuple<float, DateTime>>();
		public override int GetUpdateResolution()
		{
			return 100;
		}

		public override void Handle()
		{
		    HashSet<long> toRemove = new HashSet<long>();
		    lock ( SpeedPlayers )
		    {
		        foreach ( var item in SpeedPlayers )
		        {
		            List<IMyPlayer> players = new List<IMyPlayer>( );
		            MyAPIGateway.Players.GetPlayers( players, x => x.IdentityId == item.Key );
		            IMyPlayer player = players.FirstOrDefault( );
		            float playerSpeed = item.Value.Item1;
		            DateTime playerTime = item.Value.Item2;

		            if ( player == null || DateTime.Now - playerTime > TimeSpan.FromSeconds( 1 ) )
		            {
		                toRemove.Add( item.Key );
		                continue;
		            }

		            IMyEntity controlledEntity = player.Controller?.ControlledEntity?.Entity?.GetTopMostParent( );

		            if ( controlledEntity?.Physics == null )
		            {
		                continue;
		            }

		            Wrapper.GameAction( ( ) =>
		                                {
		                                    if ( controlledEntity.Physics.LinearVelocity.Length( ) > playerSpeed )
		                                    {
		                                        controlledEntity.Physics.SetSpeeds( Vector3.ClampToSphere( controlledEntity.Physics.LinearVelocity, playerSpeed ), controlledEntity.Physics.AngularVelocity);
		                                    }
		                                } );
		        }

		        foreach ( long key in toRemove )
		            SpeedPlayers.Remove( key );
		    }

		    base.Handle();
		}
	}
}
