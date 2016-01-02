namespace EssentialsPlugin.ChatHandlers.Settings
{
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using Settings;
    using EssentialsPlugin.UtilityClasses;

    public class HandleSettingsEnableBlockEnforcement : ChatHandlerBase
    {
        public override string GetHelp()
        {
            return "This command enables or disables block enforcement. Usage: /settings enableblockenforcement <true/false>";
        }
        public override string GetCommandText()
        {
            return "/settings enableblockenforcement";
        }

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "Block Enforcement";
            DialogItem.content = "This command lets you enable or disable Block Enforcement.||" +
                "Usage: /settings setblockenforcement <true/false>|" +
                "Running the command with no argument will return the current setting.";
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
            bool enabled = false;

            if (words.Length > 1)
            {
                Communication.SendPrivateInformation(userId, "Too many arguments detected. Doing nothing to be safe.");
                return true;
            }
            else if (words.Length == 0)
            {
                if (PluginSettings.Instance.BlockEnforcementEnabled)
                    Communication.SendPrivateInformation(userId, "Block enforcement is enabled.");
                else
                    Communication.SendPrivateInformation(userId, "Block enforcement is disabled.");
                return true;
            }
            else
            {
                if (bool.TryParse(words[0], out enabled))
                    PluginSettings.Instance.BlockEnforcementEnabled = enabled;
                else
                    Communication.SendPrivateInformation(userId, "Couldn't parse argument. Make sure it's true or false.");

                if (PluginSettings.Instance.BlockEnforcementEnabled)
                    Communication.SendPrivateInformation(userId, "Block enforcement is enabled.");
                else
                    Communication.SendPrivateInformation(userId, "Block enforcement is disabled.");
                return true;
            }
        }
    }
}