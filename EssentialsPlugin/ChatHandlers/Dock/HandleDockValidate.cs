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
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ChatHandlers
{
	public class HandleDockValidate : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command validates a docking zone.  Usage: /dock validate <dockingZoneName>";
		}

		public override string GetCommandText()
		{
			return "/dock validate";
		}

		public override bool IsAdminCommand()
		{
			return false;
		}

		public override bool AllowedInConsole()
		{
			return false;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			if (!PluginSettings.Instance.DockingEnabled)
				return false;

			if (words.Length < 1)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			// Grab shared beacon name and playerId
			String pylonName = String.Join(" ", words);
			if (PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).Count < 1)
			{
				Communication.SendPrivateInformation(userId, "Failed to retrieve your playerId.  Command failed.");
				return true;
			}

			long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).First();

			// Find shared beacon area
			Dictionary<String, List<IMyCubeBlock>> testList;
			List<IMyCubeBlock> beaconList;
			DockingZone.FindByName(pylonName, out testList, out beaconList, playerId);
			if (beaconList.Count == 4)
			{
				// Check ownership
				foreach (IMyCubeBlock entityBlock in beaconList)
				{
					if (!Entity.CheckOwnership(entityBlock, playerId))
					{
						Communication.SendPrivateInformation(userId, string.Format("You do not have permission to use '{0}'.  You must either own all the beacons or they must be shared with faction.", pylonName));
						return true;
					}
				}

				// Check for bounding box intsection of other docking zones
				int intersectElement = 0;
				if (Entity.CheckForIntersection(testList, beaconList, out intersectElement))
				{
					Communication.SendPrivateInformation(userId, string.Format("The docking zone '{0}' intersects with docking zone '{1}'.  Make sure you place your docking zones so they don't overlap.", pylonName, testList.ElementAt(intersectElement).Key));
					return true;
				}

				Communication.SendPrivateInformation(userId, string.Format("The docking zone '{0}' appears to be a valid docking zone.", pylonName));
			}
			else if (beaconList.Count > 4)
			{
				Communication.SendPrivateInformation(userId, string.Format("Too many beacons with the name or another zone with the name '{0}'.  Place only 4 beacons to create a zone or try a different zone name.", pylonName));
			}
			else
			{
				Communication.SendPrivateInformation(userId, string.Format("Can not locate docking zone '{0}'.  There must be 4 beacons with the name '{0}' to create a docking zone.  Beacons must be fully built!", pylonName));
			}
			
			return true;
		}
	}
}
