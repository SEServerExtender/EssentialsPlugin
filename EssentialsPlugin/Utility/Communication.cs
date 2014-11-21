using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
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

			/*
			String[] lines;
			if (wrap)
			{
				Int32 maxLineLength = 32;
				Int32 charCount = 0;
				lines = infoText.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / maxLineLength).Select(g => string.Join(" ", g)).ToArray();
			}
			else
				lines = new string[] { infoText };

			foreach (String line in lines)
			{
				if (infoText != "")
					ChatManager.Instance.SendPublicChatMessage(line);
			}
			 */

			ChatManager.Instance.SendPublicChatMessage(infoText);
		}

		public static void SendPrivateInformation(ulong playerId, String infoText)
		{
			if (infoText == "")
				return;

			/*
			String[] lines;

			if (wrap)
			{
				Int32 maxLineLength = 32;
				Int32 charCount = 0;
				lines = infoText.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / maxLineLength).Select(g => string.Join(" ", g)).ToArray();
			}
			else
				lines = new string[] { infoText };

			foreach (String line in lines)
			{
				if (infoText != "")
					ChatManager.Instance.SendPrivateChatMessage(playerId, line);
			}
			 */

			ChatManager.Instance.SendPrivateChatMessage(playerId, infoText);
		}

		public static void SendClientMessage(ulong steamId, string message)
		{
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
	}
}
