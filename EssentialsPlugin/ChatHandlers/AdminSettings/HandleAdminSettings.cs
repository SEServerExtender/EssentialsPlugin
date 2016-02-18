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

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            string results = PluginSettings.Instance.GetOrSetSettings( "" ).Replace( "\r\n", "| " );
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "Admin Settings";
            DialogItem.content = " Allows you to configure settings in Essentials.|" +
                "Type /admin settings to get a list of available settings and their current values.|" +
                "Using the command without a set argument will return the current setting for that item.||" +
                "Current settings:| " + results;
            DialogItem.buttonText = "close";
            return DialogItem;
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
                //string longMessage =
                //    "/dialog \"Help\" \"Admin Settings\" \"\"" +
                //    "\"" + results.Replace( "\r\n", "| " ) + "\" \"close\" ";
                //Communication.WaypointMessage( userId, longMessage );

                Communication.DisplayDialog( userId, "Admin Settings", "Current Settings", results.Replace( "\r\n", "|" ), "close" );
            }
            else
            Communication.SendPrivateInformation(userId, results);
            return true;
		}
	}
}
