namespace EssentialsPlugin.ChatHandlers.Settings
{
    using EssentialsPlugin;
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;

    public class HandleSettingsSetMOTD : ChatHandlerBase
    {
        public override string GetHelp()
        {
            return "This command allows you to set the message of the day. Usage: /settings SetMOTD <enabled> <message> <title> <header> <button>";
        }
        public override string GetCommandText()
        {
            return "/settings setmotd";
        }

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "MOTD";
            DialogItem.content = "This command lets you set the message of the day.||" +
                "Usage: /settings setmotd < enabled > < message > < title > < header > < button >|" +
                "'Enabled' is the only required argument, and must be 'true' or 'false'.|" +
                "Arguments with multiple words must be wrapped in quotations, and line breaks can be added with the pipe character.||" +
                "This window would be made with: /settings setmotd enabled \"lots of text here\" Help MOTD close";
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

        public override bool HandleCommand(ulong userId, string[] words)
        {
            string enabled = null;
            string message = null;
            string title = null;
            string header = null;
            string button = null;

            SettingsGreetingDialogItem _greetingItem = PluginSettings.Instance.GreetingItem;
            if (words.Length < 1)
            {
                Communication.SendPrivateInformation(userId, GetHelp());
                return true;
            }

            if (words.Length > 5)
            {
                //check if we have too many words, in case user doesn't wrap a multi-word phrase in quotes
                Communication.SendPrivateInformation(userId, "Too many arguments detected. Did you forget to put quotations around something?");
                return true;
            }
            
            //let's make sure we only read elements that exist
            if ( words.Length >= 1 )
                enabled = words[0].ToLower( );
            if ( words.Length >= 2 )
                message = words[1];
            if ( words.Length >= 3 )
                title = words[2];
            if ( words.Length >= 4 )
                header = words[3];
            if ( words.Length >= 5 )
                button = words[4];

            //enabled is the only required argument
            _greetingItem.Enabled = string.Equals(enabled, "true");

            //evaluate each argument; if it's null, don't change existing data
            if (message != null)
                _greetingItem.Contents = message;

            if (title != null)
                _greetingItem.Title = title;

            if (header != null)
                _greetingItem.Header = header;

            if (button != null)
                _greetingItem.ButtonText = button;

            Communication.SendPrivateInformation(userId, "MOTD set.");
            return true;
        }
    }
}