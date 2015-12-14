namespace EssentialsPlugin.ProcessHandlers
{
    using System;
    using EssentialsPlugin.ChatHandlers;
    using EssentialsPlugin.EntityManagers;
    using SEModAPIInternal.API.Common;
    using EssentialsPlugin;

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

		public ProcessConceal()
		{
			//EntityManagement.RevealAll();
		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.DynamicConcealEnabled)
				return;

			if (DateTime.Now - m_lastConcealCheck > TimeSpan.FromSeconds(30))
			{
				//Log.Info("CheckAndConcealEntities");
				EntityManagement.CheckAndConcealEntities();
				m_lastConcealCheck = DateTime.Now;
			}

			if (DateTime.Now - m_lastRevealCheck > TimeSpan.FromSeconds(5))
			{
				//Log.Info("CheckAndRevealEntities");
				EntityManagement.CheckAndRevealEntities();
				m_lastRevealCheck = DateTime.Now;
			}

            if ( DateTime.Now - m_lastConcealProcess > TimeSpan.FromMilliseconds( 500 ) )
            {
                //Essentials.Log.Info( "Process concalment" );
                EntityManagement.ProcessConcealment( );
                m_lastConcealProcess = DateTime.Now;
            }

			base.Handle();
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			if (!PluginSettings.Instance.DynamicConcealEnabled)
				return;

			if (HandleUtilityGridsRefresh.RefreshTrack.Contains(remoteUserId))
				HandleUtilityGridsRefresh.RefreshTrack.Remove(remoteUserId);

            EntityManagement.SetOnline( remoteUserId, true );

            if ( !PluginSettings.Instance.DynamicConcealEnabled )
                return;

            EntityManagement.CheckAndRevealEntities( );
            m_lastRevealCheck = DateTime.Now;
            
            Essentials.Log.Info( "Check Reveal due to: {0}", remoteUserId );

            base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerWorldSent(ulong remoteUserId)
		{
            //OnPlayerWorldSent doesn't seem to exist anymore. Doesn't hurt to leave this, though.
			EntityManagement.SetOnline(remoteUserId, true);

			if (!PluginSettings.Instance.DynamicConcealEnabled)
				return;

			EntityManagement.CheckAndRevealEntities();
			m_lastRevealCheck = DateTime.Now;

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

