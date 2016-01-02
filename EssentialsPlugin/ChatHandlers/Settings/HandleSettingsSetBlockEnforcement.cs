namespace EssentialsPlugin.ChatHandlers.Settings
{
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using Settings;
    using EssentialsPlugin.UtilityClasses;

    public class HandleSettingsSetBlockEnforcement : ChatHandlerBase
    {
        public override string GetHelp()
        {
            return "This command allows you to configure block enforcement.";
        }
        public override string GetCommandText()
        {
            return "/settings setblockenforcement";
        }

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "Block Enforcement";
            DialogItem.content = "This command lets you configure Block Enforcement.||" +
                "Usage: /settings setblockenforcement <mode> <typeID> <maxPerGrid> <reachWarning> <exceedWarning>|" +
                "Mode is 0 for disabled, 1 filters by typeID, and 2 by subtypeID.|" +
                "Warning messages will be displayed when the player reaches and exceeds the max count of any block type. " +
                "Messages are optional, but if used, they must be enclosed in quotation marks.";
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
            int mode = 0;
            string outputName = "";

            string typeID = null;
            string maxPerGrid = null;
            string reachWarning = null;
            string exceedWarning = null;

            int gridMax = 0;


            SettingsBlockEnforcementItem blockItem = new SettingsBlockEnforcementItem();

            if (words.Length > 5)
            {
                Communication.SendPrivateInformation(userId, "Too many arguments detected. Doing nothing to be safe.");
                return true;
            }
            else if (words.Length < 3)
            {
                Communication.SendPrivateInformation(userId, "Too few arguments detected. Doing nothing to be safe.");
                return true;
            }

            typeID = words[1];
            maxPerGrid = words[2];

            if (words.Length > 3)
                reachWarning = words[3];
            if (words.Length > 4)
                exceedWarning = words[4];

            if (int.TryParse(words[0], out mode))
            {
                if (mode == 0)
                    blockItem.Mode = SettingsBlockEnforcementItem.EnforcementMode.Off;
                else if (mode == 1)
                    blockItem.Mode = SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId;
                else if (mode == 2)
                    blockItem.Mode = SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId;
                else
                {
                    Communication.SendPrivateInformation(userId, "Couldn't parse Mode; make sure it's 0, 1, or 2.");
                    return true;
                }
            }
            else
            {
                Communication.SendPrivateInformation(userId, "Couldn't parse Mode; make sure it's 0, 1, or 2.");
                return true;
            }

            if (mode == 1)
            {
                blockItem.BlockTypeId = typeID;
                outputName = blockItem.BlockTypeId;
            }
            else if (mode == 2)
            {
                blockItem.BlockSubtypeId = typeID;
                outputName = blockItem.BlockSubtypeId;
            }

            if (int.TryParse(maxPerGrid, out gridMax))
            {
                blockItem.MaxPerGrid = gridMax;
            }
            else
            {
                Communication.SendPrivateInformation(userId, "Couldn't parse maxPerGrid; make sure it's a number");
                return true;
            }

            if (reachWarning != null)
                blockItem.MaxReachWarning = reachWarning;

            if (exceedWarning != null)
                blockItem.MaxExceedWarning = exceedWarning;

            MTObservableCollection<SettingsBlockEnforcementItem> _BlockEnforcementItems = PluginSettings.Instance.BlockEnforcementItems;
            _BlockEnforcementItems.Add(blockItem);
            PluginSettings.Instance.BlockEnforcementItems = _BlockEnforcementItems;
            Communication.SendPrivateInformation(userId, "Set enforcement item for " + outputName + ".");
            return true;
        }
    }
}