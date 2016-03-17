namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;
	using VRageMath;

	public class ProcessSpawnShipTracking : ProcessHandlerBase
	{
		public override int GetUpdateResolution( )
		{
			return 5000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.NewUserTransportEnabled)
				return;

			if (MyAPIGateway.Players == null)
				return;

			if (PluginSettings.Instance.NewUserTransportStopRunawaySpawnShips)
			{
				if (DateTime.Now - LastUpdate > TimeSpan.FromSeconds(5))
				{
					LastUpdate = DateTime.Now;
					HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
					try
					{
						MyAPIGateway.Entities.GetEntities(entities);
					}
					catch
					{
						Essentials.Log.Info( string.Format( "StopRunaway(): Entities busy, skipping" ) );
						return;
					}

					foreach (IMyEntity entity in entities)
					{
						if (!(entity is IMyCubeGrid))
							continue;

						bool found = PluginSettings.Instance.NewUserTransportSpawnShipNames.Any( name => entity.DisplayName.Contains( name ) );

						if (!found)
							continue;

						IMyCubeGrid grid = (IMyCubeGrid)entity;
						if ( grid.Physics == null )
						{
							continue;
						}
						bool foundControlled = PlayerManager.Instance.ConnectedPlayers.Select( steamId => PlayerMap.Instance.GetFastPlayerIdFromSteamId( steamId ) ).Select( Player.FindControlledEntity ).Any( testEntity => testEntity == entity );

						if (foundControlled)
							continue;

						Wrapper.GameAction(() =>
						                   {
							                   try
							                   {
								                   double linear = Math.Round(grid.Physics.LinearVelocity.LengthSquared(), 1);
								                   double angular = Math.Round(grid.Physics.AngularVelocity.LengthSquared(), 1);

								                   if (linear > 0 || angular > 0)
								                   {
									                   grid.Physics.LinearVelocity = Vector3.Zero;
									                   grid.Physics.AngularVelocity = Vector3.Zero;
													   Essentials.Log.Info( "Stopping runaway spawnship: {0}", grid.EntityId );
								                   }
							                   }
							                   catch (Exception ex)
							                   {
												   Essentials.Log.Error( ex, "Error stopping spawnship: {0}", ex );
							                   }
						                   });
					}
				}
			}
			
			base.Handle();
		}
	}
}

