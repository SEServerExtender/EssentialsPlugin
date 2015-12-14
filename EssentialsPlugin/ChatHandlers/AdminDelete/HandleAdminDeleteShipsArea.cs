namespace EssentialsPlugin.ChatHandlers.AdminDelete
{
	using System.Linq;
	using EssentialsPlugin.Utility;
	using VRageMath;

	public class HandleAdminDeleteShipsArea : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to delete all ships from an area defined by x, y, z, and radius.  Usage: /admin delete ships area [X] [Y] [Z] [RADIUS]";
		}

		public override string GetCommandText()
		{
			return "/admin delete ships area";
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

		// admin deletearea x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			if (words.Count() != 4 && words.Count() != 0)
				return false;

			if (words.Count() != 4)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			// Test Input
			float test = 0;
			for(int r = 0; r < 4; r++)
			{
				if(!float.TryParse(words[r], out test))
				{
					Communication.SendPrivateInformation(userId, string.Format("The value at position {0} - '{1}' is invalid.  Please try the command again.", r + 1, words[r]));
					return true;
				}
			}

			Vector3D startPosition = new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2]));
			float radius = float.Parse(words[3]);

			CubeGrids.RemoveGridsInSphere(userId, startPosition, radius, RemoveGridTypes.Ships);

			return true;
		}
	}
}
