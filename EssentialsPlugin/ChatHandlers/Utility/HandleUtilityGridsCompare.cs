namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using VRage.ModAPI;

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
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
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

			//Communication.WaypointMessage(userId, string.Format("/conceal {0}", string.Join(",", entityList.Select(x => x.ToString()).ToArray())));			
			Console.WriteLine("Here: {0}", string.Join(",", entityList.Select(x => x.ToString()).ToArray()));
			return true;
		}

	}
}
