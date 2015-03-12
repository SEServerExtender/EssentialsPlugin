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
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleAdminPlayerCleanup : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "";
		}
		public override string GetCommandText()
		{
			return "/admin player cleanup";
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
		public override bool HandleCommand( ulong userId, string command )
		{
			string[ ] words = command.Split( ' ' );
			Logging.WriteLineAndConsole( "Starting scan ..." );

			List<IMyIdentity> players = new List<IMyIdentity>();
			MyAPIGateway.Players.GetAllIdentites(players);
			HashSet<long> owners = GetAllOwners();

			Logging.WriteLineAndConsole(string.Format("Total Players: {0}  Total Owners: {1}", players.Count, owners.Count));
			Communication.SendPrivateInformation(userId, string.Format("Total Players: {0}  Total Owners: {1}", players.Count, owners.Count));

			Wrapper.GameAction(() =>
			{
				foreach (IMyIdentity identity in players)
				{
					if (!owners.Contains(identity.PlayerId))
						PlayerMap.Instance.RemovePlayer(identity.PlayerId);
				}

				Communication.SendPrivateInformation(userId, "Clearing Camera Data");
				PlayerMap.Instance.ClearCameraData();
				Communication.SendPrivateInformation(userId, "Camera Data Cleared");

				Communication.SendPrivateInformation(userId, "Clearing Faction Data");
				FactionCleanup();
				Communication.SendPrivateInformation(userId, "Faction Data Cleared");
			});

			return true;
		}

		private HashSet<long> GetAllOwners()
		{
			HashSet<long> owners = new HashSet<long>();
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities);

			foreach (IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);
				if (gridBuilder == null)
					continue;

				foreach (MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
				{
					if (block.Owner != 0 && !owners.Contains(block.Owner))
						owners.Add(block.Owner);
				}
			}
			return owners;
		}

		private void FactionCleanup()
		{
			HashSet<long> owners = GetAllOwners();
			MyObjectBuilder_FactionCollection factionCollection = MyAPIGateway.Session.GetWorld().Checkpoint.Factions;
			List<MyObjectBuilder_Faction> removeList = new List<MyObjectBuilder_Faction>();

			foreach (MyObjectBuilder_Faction faction in factionCollection.Factions)
			{
				bool hasMembers = false;				
				foreach(MyObjectBuilder_FactionMember member in faction.Members)
				{
					if (owners.Contains(member.PlayerId))
					{
						hasMembers = true;
						//break;
					}
					else
					{
						Logging.WriteLineAndConsole(string.Format("Removing member with no player info: {0}", member.PlayerId));
						MyAPIGateway.Session.Factions.KickMember(faction.FactionId,member.PlayerId );
					}
				}

				if(!hasMembers)
				{
					removeList.Add(faction);
				}
			}

			foreach(MyObjectBuilder_Faction faction in removeList)
			{
				MyAPIGateway.Session.Factions.RemoveFaction(faction.FactionId);
			}

			Logging.WriteLineAndConsole(string.Format("Removed {0} factions", removeList.Count));
		}
	}
}
