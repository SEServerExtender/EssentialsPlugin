namespace EssentialsPlugin.ChatHandlers.Settings
{
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using UtilityClasses;

    public class HandleSettingsSetBlockEnforcement : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "This command allows you to configure block enforcement.";
        }
        public override string GetCommandText( )
        {
        }
        /*
        public override string GetHelpDialog( )
        {
            //Probably easier to define a help dialog than to attempt to format huge GetHelp strings
        }
        */
        public override bool IsAdminCommand( )
        {
            return true;
        }

        public override bool AllowedInConsole( )
        {
            return true;
        }


        public override bool HandleCommand( ulong userId, string[] words )
        {
            //string mode = words[0];
            int mode = 0;
            string typeID = words[1];
            string subTypeID = words[2];
            string maxPerGrid = words[3];
            string reachWarning = words[4];
            string exceedWarning = words[5];

            //MTObservableCollection<SettingsBlockEnforcementItem> _blockEnforcementItem = new SettingsBlockEnforcementItem();
            //_blockEnforcementItems.Add(new SettingsBlockEnforcementItem _newEnforcementItem);
            //god I hope this is right
            //SettingsBlockEnforcementItem _newEnforcement = new SettingsBlockEnforcementItem();
            if (words.Length > 6)
            {
                Communication.SendPrivateInformation(userId, "Too many arguments detected. Doing nothing to be safe.");
                return true;
            }

            if (int.TryParse(words[0], out mode))
                if (mode <= 2 && mode >= 1)
                    


            Communication.SendPrivateInformation(userId, "Nothing here yet!");

            return true;
        }
    }
}
