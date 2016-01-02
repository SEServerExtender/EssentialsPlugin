namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using System.Linq;
    using NLog;
    using SEModAPIInternal.API.Common;
    using Utility;
    public abstract class ChatHandlerBase
	{
		protected static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
		public ChatHandlerBase( )
		{
			Log.Debug(string.Format("Added chat handler: {0}", GetCommandText()));
		}

		public virtual Boolean CanHandle(ulong steamId, String[] words, ref int commandCount)
		{
			// Administrator Command
			if (IsAdminCommand())
			{


                if (!PlayerManager.Instance.IsUserAdmin(steamId) && steamId != 0)
					return false;

            }

			// Check if this command has multiple commands that do the same thing
			if (GetMultipleCommandText().Length < 1)
			{
				commandCount = GetCommandText().Split(new char[] { ' ' }).Count();
				if (words.Length > commandCount - 1)
					return String.Join(" ", words).ToLower().StartsWith(GetCommandText());
			}
			else
			{
				bool found = false;
				foreach (string command in GetMultipleCommandText())
				{
					commandCount = command.Split(new char[] { ' ' }).Count();
					if (words.Length > commandCount - 1)
					{
						found = String.Join(" ", words).ToLower().StartsWith(command);
						if (found)
							break;
					}
				}

				return found;
			}

			return false;
		}

		public abstract string GetHelp();

        public abstract Communication.ServerDialogItem GetHelpDialog();

        public virtual String GetCommandText()
		{
			return "";
		}

		public virtual string[] GetMultipleCommandText()
		{
			return new string[] { };
		}

		public virtual bool IsAdminCommand()
		{
			return false;
		}

		public virtual bool AllowedInConsole()
		{
			return false;
		}

		public virtual bool IsClientOnly()
		{
			return false;
		}

		public virtual bool HandleCommand(ulong userId, String[] words)
		{
			return false;
		}
	}
}
