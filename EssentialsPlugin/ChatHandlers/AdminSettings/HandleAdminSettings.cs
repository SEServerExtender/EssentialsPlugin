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
            string results = PluginSettings.Instance.GetOrSetSettings( "" ).Replace( "\r\n", "| " );
            string longMessage =
                "/dialog \"Help\" \"Admin Settings\" \"\"" +
                "\" Allows you to configure settings in Essentials.|" +
                "Type /admin settings to get a list of available settings.|" +
                "Using the command without a set argument will return the current setting for that item.||" +
                "Available commands:| " + results +
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
            if ( words.Length == 0 )
            {
                string longMessage =
                    "/dialog \"Help\" \"Admin Settings\" \"\"" +
                    "\"" + results.Replace( "\r\n", "| " ) + "\" \"close\" ";
                Communication.SendClientMessage( userId, longMessage );
            }
            else
            Communication.SendPrivateInformation(userId, results);
            return true;
		}
	}
}
