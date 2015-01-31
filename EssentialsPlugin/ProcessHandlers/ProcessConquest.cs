namespace EssentialsPlugin.ProcessHandlers
{
	using EssentialsPlugin.GameModes;

	class ProcessConquest : ProcessHandlerBase
	{
		public override int GetUpdateResolution()
		{
			return 30000;
		}

		public override void Handle()
		{
			if (!PluginSettings.Instance.GameModeConquestEnabled)
				return;

			Conquest.Process();
			base.Handle();
		}
	}
}
