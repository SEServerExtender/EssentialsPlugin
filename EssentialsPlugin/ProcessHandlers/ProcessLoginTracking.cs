namespace EssentialsPlugin.ProcessHandlers
{
	using EssentialsPlugin.ProcessHandler;
	using EssentialsPlugin.Utility;

	public class ProcessLoginTracking : ProcessHandlerBase
	{
		public ProcessLoginTracking()
		{
			if (PluginSettings.Instance.LoginEnabled)
			{
				if(Players.Instance.PlayerLogins.Count == 0)
					Players.ProcessServerLogsForLogins(true);
				else
					Players.ProcessServerLogsForLogins();
			}
		}

		public override int GetUpdateResolution()
		{
			return 10000;
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			Players.Instance.UpdatePlayerTime(remoteUserId);
			base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerLeft(ulong remoteUserId)
		{
			Players.Instance.UpdatePlayerTime(remoteUserId);
			base.OnPlayerLeft(remoteUserId);
		}
	}
}

