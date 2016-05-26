namespace EssentialsPlugin.ProcessHandlers
{
    using System;
    using Entities.Blocks;
    using EssentialsPlugin;
    using EssentialsPlugin.ChatHandlers;
    using EssentialsPlugin.EntityManagers;
    using Sandbox.Game.Entities.Blocks;

    public class ProcessConceal : ProcessHandlerBase
	{
		private static DateTime m_lastConcealCheck;
		private static DateTime m_lastRevealCheck;
        private static DateTime m_lastConcealProcess;
        
        public static DateTime LastRevealCheck
		{
			get { return m_lastRevealCheck; }
			set { m_lastRevealCheck = value; }
		}
        
		public override int GetUpdateResolution()
        {
                return 5000;
        }

		public override void Handle()
		{
            if (!PluginSettings.Instance.DynamicConcealEnabled )
				return;

			if (DateTime.Now - m_lastConcealCheck > TimeSpan.FromSeconds(30))
			{
				//Essentials.Log.Info("CheckAndConcealEntities");
				EntityManagement.CheckAndConcealEntities();
				m_lastConcealCheck = DateTime.Now;
			}

			if (DateTime.Now - m_lastRevealCheck > TimeSpan.FromSeconds(5))
			{
				//Essentials.Log.Info("CheckAndRevealEntities");
				EntityManagement.CheckAndRevealEntities();
				m_lastRevealCheck = DateTime.Now;
			}

			base.Handle();
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			if (!PluginSettings.Instance.DynamicConcealEnabled)
				return;
            
            EntityManagement.SetOnline( remoteUserId, true );

            if ( !PluginSettings.Instance.DynamicConcealEnabled )
                return;

            if ( DateTime.Now - m_lastRevealCheck > TimeSpan.FromSeconds( 2 ) )
            {
                EntityManagement.CheckAndRevealEntities( );
                m_lastRevealCheck = DateTime.Now;
            }
            
            Essentials.Log.Info( "Check Reveal due to: {0}", remoteUserId );

            base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerWorldSent(ulong remoteUserId)
		{
            //OnPlayerWorldSent doesn't seem to exist anymore. Doesn't hurt to leave this, though.
			EntityManagement.SetOnline(remoteUserId, true);

			if (!PluginSettings.Instance.DynamicConcealEnabled)
				return;

            if ( DateTime.Now - m_lastRevealCheck > TimeSpan.FromSeconds( 2 ) )
            {
                EntityManagement.CheckAndRevealEntities( );
                m_lastRevealCheck = DateTime.Now;
            }

            Essentials.Log.Info( "Check Reveal due to: {0}", remoteUserId );

			base.OnPlayerWorldSent(remoteUserId);
		}

		public override void OnPlayerLeft(ulong remoteUserId)
		{
			EntityManagement.SetOnline(remoteUserId, false);
			base.OnPlayerLeft(remoteUserId);
		}

	}
}

