namespace EssentialsPlugin.ChatHandlers.Admin
{
	using System.Text;
	using EssentialsPlugin.Utility;

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
			StringBuilder sb = new StringBuilder( );
			VRage.Utils.Stats.Generic.WriteTo(sb);
			Communication.SendPrivateInformation(userId, sb.ToString());
			return true;
		}
	}
}

