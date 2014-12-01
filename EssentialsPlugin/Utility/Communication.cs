using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;
using VRageMath;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Common;
using SEModAPIExtensions.API;

using EssentialsPlugin.Utility;
using EssentialsPlugin.ChatHandlers;

namespace EssentialsPlugin.Utility
{
	public class Communication
	{
		public static void SendPublicInformation(String infoText)
		{
			if (infoText == "")
				return;

			ChatManager.Instance.SendPublicChatMessage(infoText);
		}

		public static void SendPrivateInformation(ulong playerId, String infoText)
		{
			if (infoText == "")
				return;

			ChatManager.Instance.SendPrivateChatMessage(playerId, infoText);
		}

		public static void SendClientMessage(ulong steamId, string message)
		{
			if (PlayerMap.Instance.GetPlayerIdsFromSteamId(steamId).Count < 1)
			{
				Logging.WriteLineAndConsole(string.Format("Unable to locate playerId for user with steamId: {0}", steamId));
				return;
			}

			CubeGridEntity entity = new CubeGridEntity(new FileInfo(Essentials.PluginPath + "CommRelay.sbc"));
			long entityId = BaseEntity.GenerateEntityId();
			entity.EntityId = entityId;
			entity.DisplayName = string.Format("CommRelayOutput{0}", PlayerMap.Instance.GetPlayerIdsFromSteamId(steamId).First());
			entity.PositionAndOrientation = new MyPositionAndOrientation(MathUtility.GenerateRandomEdgeVector(), Vector3.Forward, Vector3.Up);

			foreach (MyObjectBuilder_CubeBlock block in entity.BaseCubeBlocks)
			{
				if (block is MyObjectBuilder_Beacon)
				{
					MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)block;
					beacon.CustomName = message;
				}
			}

			SectorObjectManager.Instance.AddEntity(entity);
			Cleanup.Instance.Add(entityId);
		}

		public static void SendFactionClientMessage(ulong playerSteamId, String message)
		{
			foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
			{
				if (Player.CheckPlayerSameFaction(playerSteamId, steamId))
				{
					Communication.SendClientMessage(steamId, message);
				}
			}
		}

		public static void Notification(ulong steamId, MyFontEnum color, int timeInSeconds, string message)
		{
			SendClientMessage(steamId, string.Format("/notification {0} {1} {2}", color, timeInSeconds, message));
		}
	}
}
