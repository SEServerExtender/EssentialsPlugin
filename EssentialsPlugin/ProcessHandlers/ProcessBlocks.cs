namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using EssentialsPlugin.EntityManagers;

	public class ProcessBlocks : ProcessHandlerBase
	{
		private static DateTime _lastEnableCheck;
		private static DateTime _lastDisableCheck;

		public ProcessBlocks()
		{

		}

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (PluginSettings.Instance.DynamicBlockManagementEnabled)
			{
				if (DateTime.Now - _lastEnableCheck > TimeSpan.FromSeconds(6))
				{
					BlockManagement.Instance.CheckAndEnableBlocks();
					_lastEnableCheck = DateTime.Now;
				}

				if (DateTime.Now - _lastDisableCheck > TimeSpan.FromSeconds(30))
				{
					BlockManagement.Instance.CheckAndDisableBlocks();
					_lastDisableCheck = DateTime.Now;
				}
			}

			base.Handle();
		}

		public override void OnSectorSaved()
		{
			if (!PluginSettings.Instance.DynamicBlockManagementEnabled)
				return;

			BlockManagement.Instance.Save();
			base.OnSectorSaved();
		}
	}
}

