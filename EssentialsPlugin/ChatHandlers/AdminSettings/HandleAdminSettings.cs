namespace EssentialsPlugin.ChatHandlers
{
	using EssentialsPlugin.Utility;

	public class HandleAdminSettings : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "";
		}
		public override string GetCommandText()
		{
			return "/admin settings";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"Admin Settings\" \"\"" +
                "\" Allows you to configure settings in Essentials.|" +
                "Type /admin settings to get a list of available settings.|" +
                "Using the command without a set argument will return the current setting for that item." +
                "\" \"close\" ";
            return longMessage;
        }

        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		// admin nobeacon scan
		public override bool HandleCommand(ulong userId, string[] words)
		{
			string results = PluginSettings.Instance.GetOrSetSettings(string.Join(" ", words));
            //Communication.SendPrivateInformation(userId, results);
            string longMessage =
                "/dialog \"Help\" \"Admin Settings\" \"\"" +
                "\"" + results + "\" \"close\" ";
            Communication.SendClientMessage( userId, longMessage );
            return true;
		}
	}
}
