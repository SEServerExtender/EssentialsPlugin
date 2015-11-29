namespace EssentialsPlugin.ChatHandlers.Settings
{
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using System.Linq;

    public class HandleSettingsGetBlockEnforcement : ChatHandlerBase
    {
        public override string GetHelp()
        {
            return "This command retreives Block Enforcement items. Use with no arguments to get a list of enforcement items.";
        }
        public override string GetCommandText()
        {
            return "/settings getblockenforcement";
        }

        public override string GetHelpDialog()
        {
            string longMessage =
                ("/dialog \"Block Enforcement\" \"Get Help\" \"\" " +
                "\"This command retreives Block Enforcement items.||" +
                "Usage: /settings setblockenforcement <index>|" +
                "Running the command without the index argument returns a simplified list of items, " +
                "run the command again with the number of the item you want more details for.\"" +
                "\"close\" ");
            return longMessage;
        }

        public override bool IsAdminCommand()
        {
            return false;
        }

        public override bool AllowedInConsole()
        {
            return false;
        }

        public override bool HandleCommand(ulong userId, string[] words = null)
        {
            int index = 0;
            string outList = "";

            if (PluginSettings.Instance.BlockEnforcementItems.Count == 0)
            {
                Communication.SendPrivateInformation(userId, "No block enforcement items defined.");
                return true;
            }           

            if (!words.Any())
            {


                foreach (SettingsBlockEnforcementItem blockItem in PluginSettings.Instance.BlockEnforcementItems)
                {
                    //Communication.SendPrivateInformation(userId, string.Format("item: {0} {1} {2} {3} {4}", blockItem.BlockSubtypeId, blockItem.BlockTypeId, blockItem.MaxPerGrid, blockItem.MaxReachWarning, blockItem.MaxExceedWarning, blockItem.Mode));

                    outList += PluginSettings.Instance.BlockEnforcementItems.IndexOf(blockItem).ToString() + ": ";
                    if (!string.IsNullOrEmpty(blockItem.BlockTypeId))
                    {
                        outList += "BlockType: " + blockItem.BlockTypeId + "|";                        
                    }
                    else if (!string.IsNullOrEmpty(blockItem.BlockSubtypeId))
                    {
                        outList += "BlockSubtype: " + blockItem.BlockSubtypeId + "|";
                    }
                    else
                    {
                        outList += "No block type defined.|";
                    }

                    if (blockItem.Mode == SettingsBlockEnforcementItem.EnforcementMode.Off)
                    {
                        outList += "Item is disabled.";
                    }
                    else if (blockItem.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId)
                    {
                        outList += "Item sorts TypeID.";
                    }
                    else if (blockItem.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId)
                    {
                        outList += "Item sorts SubtypeID.";
                    }
                }
            }

            else
            {

                if (words.Length > 1)
                {
                    Communication.SendPrivateInformation(userId, "Too many arguments detected. Doing nothing to be safe.");
                    return true;
                }

                if (!int.TryParse(words[0], out index))
                {
                    Communication.SendPrivateInformation(userId, "Couldn't parse index; make sure it's a number");
                    return true;
                }

                //if (int.TryParse(words[0], out index))
                //    Communication.SendPrivateInformation(userId, "requested index: " + index.ToString());


                if (index > PluginSettings.Instance.BlockEnforcementItems.Count)
                {
                    Communication.SendPrivateInformation(userId, string.Format("Invalid index number, there are {0} block enforcement items.", PluginSettings.Instance.BlockEnforcementItems.Count));
                    return true;
                }

                SettingsBlockEnforcementItem blockItem = PluginSettings.Instance.BlockEnforcementItems[index];
                if (!string.IsNullOrEmpty(blockItem.BlockTypeId))
                {
                    outList += "Block Type: " + blockItem.BlockTypeId;
                }
                else if (!string.IsNullOrEmpty(blockItem.BlockSubtypeId))
                {
                    outList += "Block Subtype: " + blockItem.BlockSubtypeId;
                }
                else
                {
                    outList += "No block type defined";
                }
                if (!string.IsNullOrEmpty(blockItem.MaxPerGrid.ToString()))
                    outList += "|Max per grid: " + blockItem.MaxPerGrid.ToString();
                else
                    outList += "|Max per grid not defined.";


                if (!string.IsNullOrEmpty(blockItem.MaxReachWarning))
                {
                    outList += "|Reach Warning: " + blockItem.MaxReachWarning;
                }
                if (!string.IsNullOrEmpty(blockItem.MaxExceedWarning))
                {
                    outList += "|Exceed Warning: " + blockItem.MaxExceedWarning;
                }
                
                if (blockItem.Mode == SettingsBlockEnforcementItem.EnforcementMode.Off)
                {
                    outList += "|Item is disabled.";
                }
                else if (blockItem.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId)
                {
                    outList += "|Item sorts TypeID.";
                }
                else if (blockItem.Mode == SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId)
                {
                    outList += "|Item sorts SubtypeID.";
                }
            }
            Communication.SendClientMessage(userId, string.Format("/dialog \"Block Enforcement\" \"Get Help\" \"\" \"{0}\" \"close\"", outList));
            return true;
        }
    }
}