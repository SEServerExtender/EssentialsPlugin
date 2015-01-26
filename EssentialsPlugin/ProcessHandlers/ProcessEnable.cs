namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using EssentialsPlugin.Utility;

	public class ProcessEnable : ProcessHandlerBase
	{
		private static DateTime m_lastEnableCheck;
		private static DateTime m_lastDisableCheck;

		public ProcessEnable()
		{
			//EntityManagement.RevealAll();
		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (PluginSettings.Instance.DynamicTurretManagmentEnabled)
			{
				if (DateTime.Now - m_lastEnableCheck > TimeSpan.FromSeconds(6))
				{
					EntityManagement.CheckAndEnableTurrets();
					m_lastEnableCheck = DateTime.Now;
				}

				if (DateTime.Now - m_lastDisableCheck > TimeSpan.FromSeconds(45))
				{
					EntityManagement.CheckAndDisableTurrets();
					m_lastDisableCheck = DateTime.Now;
				}
			}

			base.Handle();
		}
	}
}

