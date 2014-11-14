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

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
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

			Communication.SendPrivateInformation(userId, string.Format("Moving {0} to within {1}m of {2}.  This may take about 20 seconds.", sourceName, distance, targetName));

			Vector3D position;
			CharacterEntity charEntity = SectorObjectManager.Instance.GetTypedInternalData<CharacterEntity>().FirstOrDefault(x => x.DisplayName.ToLower() == targetName.ToLower() && x.Health > 0);
			if(charEntity == null)
			{
				CubeGridEntity gridEntity = SectorObjectManager.Instance.GetTypedInternalData<CubeGridEntity>().FirstOrDefault(x => (x.DisplayName.ToLower().Contains(targetName.ToLower()) || x.Name.ToLower().Contains(targetName.ToLower())) && !x.IsDisposed);
				if (gridEntity == null)
				{
					Communication.SendPrivateInformation(userId, string.Format("Can not find user or grid with the name: {0}", targetName));
					return true;
				}
				position = (Vector3)gridEntity.Position;
			}
			else
				position = (Vector3)charEntity.Position;

			Vector3D startPosition = MathUtility.RandomPositionFromPoint((Vector3)position, distance);
			if(!Player.Move(sourceName, startPosition))
			{
				Communication.SendPrivateInformation(userId, string.Format("Can not move user: {0}", sourceName));
			}

			Communication.SendPrivateInformation(userId, string.Format("Moved {0} to within {1}m of {2}", sourceName, (int)Math.Round(Vector3D.Distance(startPosition, position)), targetName));
			return true;
		}
	}
}
