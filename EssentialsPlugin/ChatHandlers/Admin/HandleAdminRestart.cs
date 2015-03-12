namespace EssentialsPlugin.ChatHandlers.Admin
{
	using System;
	using EssentialsPlugin.ProcessHandlers;
	using EssentialsPlugin.Utility;

	public class HandleAdminRestart : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Force a restart to occur within the next X minutes.  Usage: /admin restart <minutes>";
		}

		public override string GetCommandText()
		{
			return "/admin restart";
		}

		public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand( ulong userId, string command )
		{
			string[ ] words = command.Split( ' ' );
			if ( words.Length != 1 )
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			int minutes = -1;
			if (!int.TryParse(words[0], out minutes))
			{
				Communication.SendPrivateInformation(userId, string.Format("Invalid reboot time.  Must be an integer."));
				return true;
			}

			if(minutes < 1)
			{
				Communication.SendPrivateInformation(userId, string.Format("Invalid reboot time.  Must be greater than 0."));
				return true;
			}

			ProcessRestart.ForcedRestart = DateTime.Now.AddMinutes(minutes);
			Communication.SendPublicInformation(string.Format("[NOTICE]: The administrator is forcing a restart to occur in {0} minute(s).", minutes));

			return true;
		}
	}
}
