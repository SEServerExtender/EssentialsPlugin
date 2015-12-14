namespace EssentialsPlugin.ChatHandlers
{
	using System.Text;
	using EssentialsPlugin.Utility;
	using VRage.Utils;

	public class HandleAdminStatistics : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Get performance statistics of the server.  Usage: /admin statistics";
		}

		public override string GetCommandText()
		{
			return "/admin statistics";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\""+GetHelp()+"\" \"close\" ";
            return longMessage;
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
			StringBuilder sb = new StringBuilder();
			Stats.Generic.WriteTo(sb);
			Communication.SendPrivateInformation(userId, sb.ToString());
			return true;
		}
	}
}

