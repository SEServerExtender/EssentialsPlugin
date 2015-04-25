namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using EssentialsPlugin.ChatHandlers;
	using EssentialsPlugin.EntityManagers;
	using EssentialsPlugin.Utility;

	public class ProcessConceal : ProcessHandlerBase
	{
		private static DateTime m_lastConcealCheck;
		private static DateTime m_lastRevealCheck;

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

			base.Handle();
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			if (!PluginSettings.Instance.DynamicConcealEnabled)
				return;

			if (HandleUtilityGridsRefresh.RefreshTrack.Contains(remoteUserId))
				HandleUtilityGridsRefresh.RefreshTrack.Remove(remoteUserId);

			base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerWorldSent(ulong remoteUserId)
		{
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

