namespace EssentialsPlugin.ProcessHandlers
{
    using System;
    using System.Collections.Generic;
    using Settings;
    using Utility;

    internal class ProcessTimedCommands : ProcessHandlerBase
    {
        public override int GetUpdateResolution( )
        {
            return 10000;
        }

        public override void Handle( )
        {
            if ( !PluginSettings.Instance.TimedCommandsEnabled )
                return;

            foreach ( TimedCommandItem item in PluginSettings.Instance.TimedCommandsItems )
            {
                if ( !item.Enabled )
                    continue;

                if ( item.CommandRunTime.Hour == DateTime.Now.Hour )
                {
                    if (item.CommandRunTime.Minute == DateTime.Now.Minute)
                    {
                        if (!item.HasRun)
                        {
                            item.HasRun = true;
                            Essentials.Instance.HandleChatMessage( 0, item.CommandText );
                        }
                    }

                    if (item.WarningEnabled && !string.IsNullOrEmpty( item.WarningText ))
                    {
                        if (item.CommandRunTime.Minute - item.WarningTime == DateTime.Now.Minute)
                        {
                            if (!item.HasWarned)
                            {
                                item.HasWarned = true;
                                Communication.SendPublicInformation( item.WarningText );
                            }
                        }
                    }

                    if (item.CommandRunTime.Minute + 1 == DateTime.Now.Minute )
                    {
                        item.HasRun = false;
                        item.HasWarned = false;
                    }
                }
            }
            base.Handle( );
        }
    }
}
