namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using VRage.ModAPI;
	using VRageMath;

	public class HandleAdminMovePlayerTo : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to move a player to a location near another player.  Usage: /admin move player to [SOURCE_USERNAME] [TARGET_USERNAME|TARGET_SHIPNAME] (DISTANCE)";
		}
		public override string GetCommandText()
		{
			return "/admin move player to";
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

		// /admin movefrom x y z x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			if (words.Count() < 2)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}
			
			string sourceName = words[0];
			float distance = 50f;

			// TODO Allow quotes so we can do distance?
			bool parse = false;
			if (words.Count() > 2)
			{
				parse = float.TryParse(words[words.Count() - 1], out distance);
			}

			string targetName;
			if(parse)
				targetName = string.Join(" ", words.Skip(1).Take(words.Count() - 2).ToArray());
			else
				targetName = string.Join(" ", words.Skip(1).ToArray());

			IMyEntity entity = Player.FindControlledEntity(targetName);
			if (entity == null)
			{
				entity = CubeGrids.Find(targetName);
				if(entity == null)
				{
					Communication.SendPrivateInformation(userId, string.Format("Can not find user or grid with the name: {0}", targetName));
					return true;
				}
			}

			Vector3D position = entity.GetPosition();

			Communication.SendPrivateInformation(userId, string.Format("Trying to move {0} to within {1}m of {2}.  This may take about 20 seconds.", sourceName, distance, targetName));
			Vector3D startPosition = MathUtility.RandomPositionFromPoint((Vector3)position, distance);
			if(!Player.Move(sourceName, startPosition))
			{
				Communication.SendPrivateInformation(userId, string.Format("Can not move user: {0} (Is user in a cockpit or not in game?)", sourceName));
				return true;
			}

			Communication.SendPrivateInformation(userId, string.Format("Moved {0} to within {1}m of {2}", sourceName, (int)Math.Round(Vector3D.Distance(startPosition, position)), targetName));
			return true;
		}
	}
}
