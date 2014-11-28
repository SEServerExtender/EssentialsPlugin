using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssentialsPlugin.Utility;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRageMath;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using System.IO;
using SEModAPIInternal.API.Common;
using EssentialsPlugin.ChatHandlers;

namespace EssentialsPlugin.ProcessHandler
{
	public class ProcessCommunication : ProcessHandlerBase
	{
		#region Private Fields
		private HashSet<long> m_processedRelays;
		private Random m_random;
		private bool m_init;
		#endregion

		public ProcessCommunication()
		{
			m_processedRelays = new HashSet<long>();
			m_random = new Random();
		}

		public override int GetUpdateResolution()
		{
			return 100;
		}

		public override void Handle()
		{
			if (!m_init && MyAPIGateway.Entities != null)
			{
				m_init = true;
				CleanupRelays();
				AddGlobalRelay();
			}

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid && x.DisplayName.StartsWith("CommRelay") && !x.DisplayName.StartsWith("CommRelayGlobal") && !x.DisplayName.StartsWith("CommRelay0") && !x.DisplayName.StartsWith("CommRelayOutput"));
			});

			foreach(IMyEntity entity in entities)
			{
				try
				{
					ExtractCommandFromEntity(entity);
				}
				catch(Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("ExtractCommandFromEntity Error: {0}", ex.ToString()));
				}
			}

			/*
			foreach (IMyEntity entity in entities)
			{
				CubeGridEntity cubeEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
				cubeEntity.Dispose();
			}
			*/
			// Cleanup processing
			Cleanup.Instance.Process();

			base.Handle();
		}

		private void ExtractCommandFromEntity(IMyEntity entity)
		{
			IMyCubeGrid grid = (IMyCubeGrid)entity;
			MyObjectBuilder_CubeGrid gridBuilder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder();
			string command = "";
			foreach(MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
			{
				if(block is MyObjectBuilder_Beacon)
				{
					MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)block;
					command = beacon.CustomName;
					CubeGridEntity cubeEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
					cubeEntity.Dispose();

					break;
				}
			}

			string player = entity.DisplayName.Replace("CommRelay", "");
			long playerId = long.Parse(player);
			ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerId(playerId);
			Logging.WriteLineAndConsole(string.Format("COMMAND {1} - {2}: {0}", command, playerId, entity.EntityId));

			if (!m_processedRelays.Contains(entity.EntityId))
			{
				m_processedRelays.Add(entity.EntityId);
				Essentials.Instance.HandleChatMessage(steamId, command);
			}
			else
			{
				Logging.WriteLineAndConsole(string.Format("Ignoring repeat beacon: {0}", entity.EntityId));
			}
		}

		// Events and Overrides

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerLeft(ulong remoteUserId)
		{
			base.OnPlayerLeft(remoteUserId);
		}

		// Private functions
		private void CleanupRelays()
		{
			Wrapper.GameAction(() =>
			{
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
				List<IMyEntity> entitiesToRemove = new List<IMyEntity>();

				foreach (IMyEntity entity in entities)
				{
					if (entity.DisplayName.StartsWith("CommRelay"))
					{
						entitiesToRemove.Add(entity);
					}
				}

				for (int r = entitiesToRemove.Count - 1; r >= 0; r--)
				{
					IMyEntity entity = entitiesToRemove[r];
					MyAPIGateway.Entities.RemoveEntity(entity);
				}
			});
		}

		private void AddGlobalRelay()
		{
			CubeGridEntity entity = new CubeGridEntity(new FileInfo(Essentials.PluginPath + "CommRelay.sbc"));
			entity.EntityId = BaseEntity.GenerateEntityId();
			entity.DisplayName = "CommRelayGlobal";

			float halfExtent = MyAPIGateway.Entities.WorldSafeHalfExtent();
			if (halfExtent == 0f)
				halfExtent = 900000f;

			entity.PositionAndOrientation = new MyPositionAndOrientation(MathUtility.GenerateRandomEdgeVector(), Vector3.Forward, Vector3.Up);

			List<string> commands = new List<string>();
			// Give a list of commands
			foreach(ChatHandlerBase chatBase in Essentials.ChatHandlers)
			{
				string[] command = chatBase.GetCommandText().Split(new char[] {' '}, 2);
				if (!commands.Contains(command[0]))
					commands.Add(command[0]);
			}

			string finalText = "";
			foreach(string command in commands)
			{
				if (finalText != "")
					finalText += "\n";

				finalText += command;
			}

			finalText += "\n/help";

			if (PluginSettings.Instance.ServerName != "")
				finalText += "\n" + "servername:" + PluginSettings.Instance.ServerName;

			foreach(MyObjectBuilder_CubeBlock block in entity.BaseCubeBlocks)
			{
				if (block is MyObjectBuilder_Beacon)
				{
					MyObjectBuilder_Beacon beacon = (MyObjectBuilder_Beacon)block;
					beacon.CustomName = finalText;
				}
			}

			SectorObjectManager.Instance.AddEntity(entity);
		}
	}
}

