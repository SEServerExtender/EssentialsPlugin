using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminDeleteStationsArea : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to delete all stations from an area defined by x, y, z, and radius.  Usage: /admin delete ships area [X] [Y] [Z] [RADIUS]";
		}

		public override string GetCommandText()
		{
			return "/admin delete stations area";
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
			if(words.Count() != 4 && words.Count() != 0)
				return false;

			if (words.Count() == 0)
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

			CubeGrids.RemoveGridsInSphere(userId, startPosition, radius, RemoveGridTypes.Stations);
			return true;
		}
	}
}
