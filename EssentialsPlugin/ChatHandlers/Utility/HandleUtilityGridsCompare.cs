using System;
using System.Collections.Generic;
using System.Linq;
using EssentialsPlugin.Utility;

using Sandbox.ModAPI;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleUtilityGridsCompare : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This will refresh your ships.  Usage: /utility grids refresh";
		}
		public override string GetCommandText()
		{
			return "/utility grids compare";
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
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>( );
			MyAPIGateway.Entities.GetEntities(entities);
			List<long> entityList = new List<long>();

			foreach(IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				if (!entity.InScene)
					continue;

				entityList.Add(entity.EntityId);
			}

			Communication.SendClientMessage(userId, string.Format("/conceal {0}", string.Join(",", entityList.Select(x => x.ToString()).ToArray())));			
			Console.WriteLine("Here: {0}", string.Join(",", entityList.Select(x => x.ToString()).ToArray()));
			return true;
		}

	}
}
