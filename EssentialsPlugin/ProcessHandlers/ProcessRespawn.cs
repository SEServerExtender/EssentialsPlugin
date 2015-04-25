namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Server;

	public class ProcessRespawn : ProcessHandlerBase
	{
		Dictionary<long, DateTime> m_deathTracker = new Dictionary<long, DateTime>( );
		HashSet<long> m_respawnShown = new HashSet<long>();

		public ProcessRespawn()
		{
		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.ServerRespawnMenuOverride)
				return;

			List<IMyPlayer> players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);

			foreach (IMyPlayer player in players)
			{
				if (player.Controller != null && player.Controller.ControlledEntity != null && player.Controller.ControlledEntity.Entity != null)
				{
					IMyEntity entity = player.Controller.ControlledEntity.Entity;
					if (entity is IMyCharacter)
					{
						MyObjectBuilder_Character character = (MyObjectBuilder_Character)entity.GetObjectBuilder();

						if (character.Health < 1 && !m_deathTracker.ContainsKey(entity.EntityId))
						{
							m_deathTracker.Add(entity.EntityId, DateTime.Now);
							Essentials.Log.Info( "Found a dead character" );
						}
						else if (character.Health < 1 && m_deathTracker.ContainsKey(entity.EntityId) && !m_respawnShown.Contains(entity.EntityId) && (DateTime.Now - m_deathTracker[entity.EntityId]).TotalSeconds > 3)
						{
							m_respawnShown.Add(entity.EntityId);
							Wrapper.GameAction(() =>
							{
								ServerNetworkManager.ShowRespawnMenu(player.SteamUserId);
							});
						}
					}
				}
			}

			base.Handle();
		}
	}
}

