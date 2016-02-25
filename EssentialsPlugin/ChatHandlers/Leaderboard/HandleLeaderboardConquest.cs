namespace EssentialsPlugin.ChatHandlers
{
	using System.Linq;
	using EssentialsPlugin.GameModes;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using SEModAPIInternal.API.Common;
	using VRage.Game;

    public class HandleLeaderboardConquest : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This displays the leaderboard for the conquest game mode.  Usage: /leaderboard conquest";
		}

		public override string GetCommandText()
		{
			return "/leaderboard conquest";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "";
            DialogItem.content = GetHelp( );
            DialogItem.buttonText = "close";
            return DialogItem;
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
			var board = Conquest.Instance.Leaderboard;

			string leaderResult = "";
			/*
			foreach (var p in board)
			{
				leaderResult += string.Format("Here: {0} - {1}", p.Key, p.Value);
			}
			leaderResult += "\r\n";
			*/
			var leaders = Conquest.Instance.Leaderboard.GroupBy(x => x.Value).Select(group => new { group.Key, Total = group.Count() }).OrderByDescending(x => x.Total);
			int position = 1;
			foreach (var p in leaders)
			{
				if (leaderResult != "")
					leaderResult += "\r\n";				

				MyObjectBuilder_Checkpoint.PlayerItem item = PlayerMap.Instance.GetPlayerItemFromPlayerId(p.Key);
				leaderResult += string.Format("#{0}: {1} with {2} asteroids", position, item.Name, p.Total);
				position++;
			}

			leaderResult += "\r\n\r\n";

			long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId(userId);
			int playerCount = 0;
			var playerItem = leaders.FirstOrDefault(x => x.Key == playerId);
			if(playerItem != null)
			{
				playerCount = playerItem.Total;
			}

			leaderResult += string.Format("You currently have {0} owned asteroids.", playerCount);

			//Communication.SendPrivateInformation(userId, leaderResult);
			Communication.DisplayDialog(userId, "Conquest Leaderboard", "Current Leaders", leaderResult);

			return true;
		}
	}
}

