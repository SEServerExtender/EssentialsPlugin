namespace EssentialsPlugin.ChatHandlers
{
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using VRageMath;

	public class HandlePos : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "Shows your current position.  Usage: /pos";
		}

		public override string GetCommandText()
		{
			return "/pos";
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
			return false;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			Vector3D position = Vector3D.Zero;
			Wrapper.GameAction(() => 
			{
				List<IMyPlayer> players = new List<IMyPlayer>();
				MyAPIGateway.Players.GetPlayers(players, x => x.SteamUserId == userId);

				if (players.Count > 0)
				{
					IMyPlayer player = players.First();
					position = player.GetPosition();
				}
			});

			Communication.SendPrivateInformation(userId, string.Format("Position - X:{0:F2} Y:{1:F2} Z:{2:F2}", position.X, position.Y, position.Z));
			return true;
		}
	}
}
