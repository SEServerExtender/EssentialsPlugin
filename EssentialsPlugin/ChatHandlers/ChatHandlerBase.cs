using System;
using System.Linq;
using SEModAPIInternal.API.Common;
using EssentialsPlugin.Utility;

namespace EssentialsPlugin.ChatHandlers
{
	public abstract class ChatHandlerBase
	{
		public ChatHandlerBase()
		{
			//Logging.WriteLineAndConsole(string.Format("Added chat handler: {0}", GetCommandText()));
		}

		public virtual Boolean CanHandle(ulong steamId, String[] words, ref int commandCount)
		{
			if (IsAdminCommand())
			{
				if (!PlayerManager.Instance.IsUserAdmin(steamId) && steamId != 0)
					return false;
			}

			commandCount = GetCommandText().Split(new char[] { ' ' }).Count();
			if (words.Length > commandCount - 1)
				return String.Join(" ", words).ToLower().StartsWith(GetCommandText());

			return false;
		}

		public abstract string GetHelp();

		public virtual String GetCommandText()
		{
			return "";
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
