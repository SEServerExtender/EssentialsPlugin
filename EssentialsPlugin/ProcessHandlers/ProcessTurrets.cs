namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using EssentialsPlugin.EntityManagers;
	using EssentialsPlugin.Utility;

	public class ProcessTurrets : ProcessHandlerBase
	{
		private static DateTime m_lastEnableCheck;
		private static DateTime m_lastDisableCheck;

		public ProcessTurrets()
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
					TurretManagement.CheckAndEnableTurrets();
					m_lastEnableCheck = DateTime.Now;
				}

				if (DateTime.Now - m_lastDisableCheck > TimeSpan.FromSeconds(45))
				{
					TurretManagement.CheckAndDisableTurrets();
					m_lastDisableCheck = DateTime.Now;
				}
			}

			base.Handle();
		}
	}
}

