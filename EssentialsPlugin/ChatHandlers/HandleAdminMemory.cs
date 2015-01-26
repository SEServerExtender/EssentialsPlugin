using System;
using EssentialsPlugin.Utility;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminMemory : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Forces garbage collection.  Probably won't do much, but may help a bit.  Usage: /admin memory";
		}

		public override string GetCommandText()
		{
			return "/admin memory";
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
			GC.Collect();
			Communication.SendPrivateInformation(userId, string.Format("Essential Memory Usage: {0}", GC.GetTotalMemory(false)));

			Wrapper.GameAction(() =>
			{
				GC.Collect();
				Communication.SendPrivateInformation(userId, string.Format("In game: memory Usage: {0}", GC.GetTotalMemory(false)));
			});
			
			return true;
		}
	}
}
