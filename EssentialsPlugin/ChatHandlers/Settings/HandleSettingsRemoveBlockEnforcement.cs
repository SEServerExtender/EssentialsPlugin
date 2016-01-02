namespace EssentialsPlugin.ChatHandlers.Settings
{
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using Settings;
    using EssentialsPlugin.UtilityClasses;

    public class HandleSettingsRemoveBlockEnforcement : ChatHandlerBase
    {
        public override string GetHelp()
        {
            return "This command allows you to remove a block enforcement item.";
        }
        public override string GetCommandText()
        {
            return "/settings removeblockenforcement";
        }

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "Block Enforcement";
            DialogItem.content = "This command lets you remove a Block Enforcement item.||" +
                "Usage: /settings removeblockenforcement <index>|" +
                "This command will remove the block enforcement item at the given index.|" +
                "Running the command without the index argument will return a simplified list of enforcement items. " +
                "Use the '/settings getblockenforcement' command for more information.";
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
            int index = 0;


            SettingsBlockEnforcementItem blockItem = new SettingsBlockEnforcementItem();

            if (words.Length > 1)
            {
                Communication.SendPrivateInformation(userId, "Too many arguments detected. Doing nothing to be safe.");
                return true;
            }
            else if (words.Length == 0)
            {
                HandleSettingsGetBlockEnforcement getEnf = new HandleSettingsGetBlockEnforcement();
                getEnf.HandleCommand(userId, null);
            }


            if (!int.TryParse(words[0], out index))
            {
                Communication.SendPrivateInformation(userId, "Couldn't parse index; make sure it's a number.");
                return true;
            }
            else
            {
                MTObservableCollection<SettingsBlockEnforcementItem> _BlockEnforcementItems = PluginSettings.Instance.BlockEnforcementItems;
                _BlockEnforcementItems.RemoveAt(index);
                PluginSettings.Instance.BlockEnforcementItems = _BlockEnforcementItems;
                Communication.SendPrivateInformation(userId, "Removed enforcement item " + index.ToString() + ".");
                return true;
            }
        }
    }
}