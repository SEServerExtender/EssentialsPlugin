using System;
using System.Linq;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public abstract class ChatHandlerBase
	{
		public ChatHandlerBase()
		{
			//Logging.WriteLineAndConsole(string.Format("Added chat handler: {0}", GetCommandText()));
		}

		public virtual bool CanHandle(ulong steamId, string[] words, ref int commandCount)
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
				commandCount = GetCommandText().Split( ' ' ).Length;
				if (words.Length > commandCount - 1)
					return string.Join(" ", words).ToLower().StartsWith(GetCommandText());
			}
			else
			{
				foreach (string command in GetMultipleCommandText())
				{
					commandCount = command.Split( ' ' ).Length;
					if (words.Length > commandCount - 1)
					{
						if ( string.Join(" ", words).ToLower().StartsWith(command) )
							return true;
					}
				}

				return false;
			}

			return false;
		}

		public abstract string GetHelp();

		public virtual string GetCommandText()
		{
			return string.Empty;
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

		public virtual bool HandleCommand(ulong userId, string command)
		{
			return false;
		}
	}
}
