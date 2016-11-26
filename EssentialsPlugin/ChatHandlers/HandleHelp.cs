namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SEModAPIInternal.API.Common;
    using Utility;

    public class HandleHelp : ChatHandlerBase
    {
        public override string GetHelp( )
        {
            return "If you're here, I can't help you.";
        }

        public override string GetCommandText( )
        {
            return "/help";
        }

        public override bool AllowedInConsole( )
        {
            return true;
        }
        
        public override bool HandleCommand( ulong userId, string[] words )
        {
            if (userId == 0)
                HandleCommand( userId, words );
            else if (( words.Length > 0 ) && ( words[0] == "chat" ))
                HandleHelpCommand( userId, words.Skip( 1 ).ToArray( ) );
            else
                HandleHelpDialog( userId, words );

            return true;
        }

        /// <summary>
        ///     This function displays available help for all the functionality of this plugin
        /// </summary>
        /// <param name="remoteUserId"></param>
        /// <param name="commandParts"></param>
        private void HandleHelpCommand( ulong remoteUserId, IReadOnlyCollection<string> commandParts )
        {
            if (commandParts.Count == 0)
            {
                List<string> commands = new List<string>( );
                foreach (ChatHandlerBase handler in Essentials.ChatHandlers)
                    if (handler.GetMultipleCommandText( ).Length < 1)
                    {
                        string commandBase = handler.GetCommandText( ).Split( new[] {" "}, StringSplitOptions.RemoveEmptyEntries ).First( );
                        if (!commands.Contains( commandBase ) && !handler.IsClientOnly( ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || ( remoteUserId == 0 ) ) ) ))
                            commands.Add( commandBase );
                    }
                    else
                    {
                        foreach (string cmd in handler.GetMultipleCommandText( ))
                        {
                            string commandBase = cmd.Split( new[] {" "}, StringSplitOptions.RemoveEmptyEntries ).First( );
                            if (!commands.Contains( commandBase ) && !handler.IsClientOnly( ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || ( remoteUserId == 0 ) ) ) ))
                                commands.Add( commandBase );
                        }
                    }

                string commandList = string.Join( ", ", commands );
                string info = $"Dedicated Server Essentials v{Essentials.Instance.Version}. Available Commands: {commandList}";
                Communication.SendPrivateInformation( remoteUserId, info );
            }
            else
            {
                string helpTarget = string.Join( " ", commandParts );
                bool found = false;
                foreach (ChatHandlerBase handler in Essentials.ChatHandlers)
                    if (handler.GetMultipleCommandText( ).Length < 1)
                    {
                        if (string.Equals( handler.GetCommandText( ), helpTarget, StringComparison.CurrentCultureIgnoreCase ))
                        {
                            Communication.SendPrivateInformation( remoteUserId, handler.GetHelp( ) );
                            found = true;
                        }
                    }
                    else
                    {
                        foreach (string cmd in handler.GetMultipleCommandText( ))
                            if (string.Equals( cmd, helpTarget, StringComparison.CurrentCultureIgnoreCase ))
                            {
                                Communication.SendPrivateInformation( remoteUserId, handler.GetHelp( ) );
                                found = true;
                            }
                    }

                if (!found)
                {
                    List<string> helpTopics = new List<string>( );

                    foreach (ChatHandlerBase handler in Essentials.ChatHandlers)
                    {
                        // Again, cleanup to one function
                        string[] multipleCommandText = handler.GetMultipleCommandText( );
                        if (multipleCommandText.Length == 0)
                        {
                            if (handler.GetCommandText( ).ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || ( remoteUserId == 0 ) ) ) ))
                                helpTopics.Add( handler.GetCommandText( ).ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
                        }
                        else
                        {
                            foreach (string cmd in multipleCommandText)
                                if (cmd.ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || ( remoteUserId == 0 ) ) ) ))
                                    helpTopics.Add( cmd.ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
                        }
                    }

                    if (helpTopics.Any( ))
                    {
                        Communication.SendPrivateInformation( remoteUserId, $"Help topics for command '{helpTarget.ToLower( )}': {string.Join( ",", helpTopics.ToArray( ) )}" );
                        found = true;
                    }
                }

                if (!found)
                    Communication.SendPrivateInformation( remoteUserId, "Unknown command" );
            }
        }

        /// <summary>
        ///     This function displays available help for all the functionality of this plugin in a dialog window
        /// </summary>
        /// <param name="remoteUserId"></param>
        /// <param name="commandParts"></param>
        private void HandleHelpDialog( ulong remoteUserId, IReadOnlyCollection<string> commandParts )
        {
            if (commandParts.Count == 0)
            {
                List<string> commands = new List<string>( );
                foreach (ChatHandlerBase handler in Essentials.ChatHandlers)
                    if (handler.GetMultipleCommandText( ).Length < 1)
                    {
                        string commandBase = handler.GetCommandText( ).Split( new[] {" "}, StringSplitOptions.RemoveEmptyEntries ).First( );
                        if (!commands.Contains( commandBase ) && !handler.IsClientOnly( ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || ( remoteUserId == 0 ) ) ) ))
                            commands.Add( commandBase );
                    }
                    else
                    {
                        foreach (string cmd in handler.GetMultipleCommandText( ))
                        {
                            string commandBase = cmd.Split( new[] {" "}, StringSplitOptions.RemoveEmptyEntries ).First( );
                            if (!commands.Contains( commandBase ) && !handler.IsClientOnly( ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || ( remoteUserId == 0 ) ) ) ))
                                commands.Add( commandBase );
                        }
                    }

                string commandList = string.Join( ", ", commands );
                //take our list of commands, put line breaks between all the entries and stuff it into a dialog winow

                Communication.DisplayDialog( remoteUserId, "Help", "Available commands", commandList + "{0}||Type '/help dialog <command>' for more info.", "close" );
            }
            else
            {
                string helpTarget = string.Join( " ", commandParts);
                bool found = false;
                foreach (ChatHandlerBase handler in Essentials.ChatHandlers)
                    if (handler.GetMultipleCommandText( ).Length < 1)
                    {
                        if (string.Equals( handler.GetCommandText( ), helpTarget, StringComparison.CurrentCultureIgnoreCase ))
                        {
                            Communication.DisplayDialog( remoteUserId, handler.GetHelpDialog( ) );
                            found = true;
                        }
                    }
                    else
                    {
                        foreach (string cmd in handler.GetMultipleCommandText( ))
                            if (string.Equals( cmd, helpTarget, StringComparison.CurrentCultureIgnoreCase ))
                            {
                                Communication.DisplayDialog( remoteUserId, handler.GetHelpDialog( ) );
                                found = true;
                            }
                    }

                if (!found)
                {
                    List<string> helpTopics = new List<string>( );

                    foreach (ChatHandlerBase handler in Essentials.ChatHandlers)
                    {
                        // Again, cleanup to one function
                        string[] multipleCommandText = handler.GetMultipleCommandText( );
                        if (multipleCommandText.Length == 0)
                        {
                            if (handler.GetCommandText( ).ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || ( remoteUserId == 0 ) ) ) ))
                                helpTopics.Add( handler.GetCommandText( ).ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
                        }
                        else
                        {
                            foreach (string cmd in multipleCommandText)
                                if (cmd.ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ( !handler.IsAdminCommand( ) || ( handler.IsAdminCommand( ) && ( PlayerManager.Instance.IsUserAdmin( remoteUserId ) || ( remoteUserId == 0 ) ) ) ))
                                    helpTopics.Add( cmd.ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
                        }
                    }

                    if (helpTopics.Any( ))
                    {
                        Communication.DisplayDialog( remoteUserId, "Help", helpTarget.ToLower( ), string.Format( "Help topics for command '{0}': {1}", helpTarget.ToLower( ), string.Join( ",", helpTopics.ToArray( ) ) ) );
                        found = true;
                    }
                }

                if (!found)
                    Communication.SendPrivateInformation( remoteUserId, "Unknown command" );
            }
        }
    }
}
