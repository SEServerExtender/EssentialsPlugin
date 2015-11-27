namespace EssentialsPlugin.ChatHandlers.Settings
{
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using EssentialsPlugin;
    using System.ComponentModel;
    using System;

    public class HandleSettingsSetMOTD : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "This command allows you to set the message of the day. Usage: /settings SetMOTD <enabled> <message> <title> <header> <button>";
        }
        public override string GetCommandText( )
        {
            return "/settings setmotd";
        }

        public override bool IsAdminCommand( )
        {
            return true;
        }

        public override bool AllowedInConsole( )
        {
            return true;
        }
        
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine("PropertyChanged()");
            PluginSettings psettings = new PluginSettings();
            //Save();
            psettings.Save();
        }
        
        public override bool HandleCommand( ulong userId, string[] words )
        {           
            string enabled = words[0].ToLower( );
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

            if ( words.Length > 5 )
            {
                //check if we have too many words, in case user doesn't wrap a multi-word phrase in quotes
                Communication.SendPrivateInformation( userId, "Too many arguments detected. Did you forget to put quotations around something?" );
                return true;
            }

            _greetingItem.Enabled = string.Equals( enabled, "true" );
            //_greetingItem.PropertyChanged += OnPropertyChanged;
            //enabled is the only required argument

            if ( message == null )
                return true;
            //evaluate each argument; if it's null, exit so we don't overwrite existing data
            Log.Debug(message);
            _greetingItem.Contents = message;
           // _greetingItem.PropertyChanged += OnPropertyChanged;

            if ( title == null )
                return true;
            Log.Debug(title);
            _greetingItem.Title = title;
           // _greetingItem.PropertyChanged += OnPropertyChanged;

            

            if ( header == null )
                return true;
            Log.Debug(header);
            _greetingItem.Header = header;
           // _greetingItem.PropertyChanged += OnPropertyChanged;

            if ( button == null )
                return true;
            Log.Debug(button);
            _greetingItem.ButtonText = button;
           // _greetingItem.PropertyChanged += OnPropertyChanged;

            return true;
        }
    }
}