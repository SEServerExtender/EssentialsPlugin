namespace EssentialsPlugin.ChatHandlers.Settings
{
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using EssentialsPlugin;
    using System.ComponentModel;
    using System;
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

        public override string GetHelpDialog()
        {
            string longMessage =
                ("/dialog \"MOTD Help\" \"MOTD Header\" \"\" " +
                "\"This command lets you set the message of the day.||" +
                "Usage: /settings SetMOTD < enabled > < message > < title > < header > < button >|" +
                "'Enabled' is the only required argument, and must be 'true' or 'false'.|" +
                "Arguments with multiple words must be wrapped in quotations.\"" +
                "\"close\" ");
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

        public override bool HandleCommand(ulong userId, string[] words)
        {
            string enabled = words[0].ToLower();
            string message = words[1];
            string title = words[2];
            string header = words[3];
            string button = words[4];


            //var _greetingItem = new SettingsGreetingDialogItem( );
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

            _greetingItem.Enabled = string.Equals(enabled, "true");
            //enabled is the only required argument

            //evaluate each argument; if it's null, exit so we don't overwrite existing data
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