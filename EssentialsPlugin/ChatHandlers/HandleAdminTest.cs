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
	public class HandleAdminTest : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin test";
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
			string user = words[0];
			List<IMyPlayer> players = new List<IMyPlayer>();

			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Players.GetPlayers(players, x => x.DisplayName.ToLower() == user.ToLower());
				if (players.Count > 0)
				{
					IMyPlayer player = players.First();
					player.Controller.ControlledEntity.Entity.SetPosition(new Vector3D(1000, 1000, 5000));
					Communication.SendPrivateInformation(userId, "Moved Entity");

					
				}
			});

			//Communication.SendPrivateInformation(userId, string.Format("Total ships removed: {0}", count));

			MyAPIGateway.Utilities.ConfigDedicated.Load();
			Communication.SendPrivateInformation(userId, string.Format("Here: {0} - {1}", MyAPIGateway.Utilities.ConfigDedicated.Administrators.Count, MyAPIGateway.Utilities.ConfigDedicated.Administrators.First()));

			return true;
		}
	}
}
