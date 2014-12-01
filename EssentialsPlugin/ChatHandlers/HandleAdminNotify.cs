using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminNotify : ChatHandlerBase
	{
		private Random m_random = new Random();
		public override string GetHelp()
		{
			return "This will broadcast a notification to all users.  Usage: /admin notify <color> <time> <message>";
		}

		public override string GetCommandText()
		{
			return "/admin notify";
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
			if (words.Length < 3)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			string colour = words[0];
			MyFontEnum font = MyFontEnum.White;
			if (!Enum.TryParse<MyFontEnum>(colour, out font))
			{
				Communication.SendPrivateInformation(userId, string.Format("Invalid colour value entered.  {0} is nto a valid value.  Please enter one of the following: {1}", colour, GetFontList()));
				return true;
			}

			int timeInSeconds = 2;
			if (!int.TryParse(words[1], out timeInSeconds) || timeInSeconds < 1)
			{
				Communication.SendPrivateInformation(userId, string.Format("Invalid time value entered.  {0} is not a valid value.  Please enter a value above 0"));
				return true;
			}

			string message = string.Join(" ", words.Skip(2).ToArray());

			foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
			{
				Communication.SendClientMessage(steamId, string.Format("/notification {0} {1} {2}", font, timeInSeconds, message));
			}

			return true;
		}

		private string GetFontList()
		{
			string result = "";
			foreach(string name in Enum.GetNames(typeof(MyFontEnum)))
			{
				if(result != "")
					result += ", ";

				result += name;
			}

			return result;
		}
	}
}
