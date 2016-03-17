namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Sandbox.ModAPI;
	using Settings;
	using Utility;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;
	using VRageMath;

    class ProcessSpeed : ProcessHandlerBase
	{
        public static HashSet<Tuple<IMyPlayer,double,int>> SpeedPlayers = new HashSet<Tuple<IMyPlayer, double, int>>();
		public override int GetUpdateResolution()
		{
			return 100;
		}

		public override void Handle()
		{
		    foreach ( var item in SpeedPlayers )
		    {
		        IMyPlayer player = item.Item1;
		        double playerSpeed = item.Item2;
		        int playerTime = item.Item3;

		        IMyEntity controlledEntity = player?.Controller?.ControlledEntity?.Entity?.GetTopMostParent( );

		        if ( controlledEntity?.Physics == null )
		            continue;

		        if ( controlledEntity.Physics.LinearVelocity.Length( ) < playerSpeed )
		        {
		            controlledEntity.Physics.LinearVelocity = Vector3D.ClampToSphere(
		                    controlledEntity.Physics.LinearVelocity, playerSpeed );
		        }
                if ( controlledEntity.Physics.AngularVelocity.Length( ) < playerSpeed )
                {
                    controlledEntity.Physics.AngularVelocity = Vector3D.ClampToSphere(
                            controlledEntity.Physics.AngularVelocity, playerSpeed );
                }
            }
			base.Handle();
		}
	}
}
