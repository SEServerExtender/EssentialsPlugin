namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using EssentialsPlugin.ChatHandlers;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using VRageMath;

	public class ProcessCommunication : ProcessHandlerBase
	{
		#region Private Fields
		private HashSet<long> m_processedRelays;
		private bool m_init;
		#endregion

		public ProcessCommunication()
		{
			m_processedRelays = new HashSet<long>();
		}

		public override int GetUpdateResolution()
		{
			return 400;
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

			bool result = false;
			try
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid && x.DisplayName.StartsWith("CommRelay") && !x.DisplayName.StartsWith("CommRelayGlobal") && !x.DisplayName.StartsWith("CommRelay0") && !x.DisplayName.StartsWith("CommRelayOutput") && !x.DisplayName.StartsWith("CommRelayBroadcast"));
				result = true;
			}
			catch
			{
				Logging.WriteLineAndConsole(string.Format("ProcessCommunication: Problem getting entities, skipping update"));
			}

			if (!result)
			{
				base.Handle();
				return;
			}

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
			TimedEntityCleanup.Instance.Process();

			base.Handle();
		}

		private void ExtractCommandFromEntity(IMyEntity entity)
		{
			IMyCubeGrid grid = (IMyCubeGrid)entity;
			MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);
			if (gridBuilder == null)
				return;
			
			string command = "";
			foreach(MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks)
			{
				MyObjectBuilder_Beacon beacon = block as MyObjectBuilder_Beacon;
				if(beacon != null)
				{
					command = beacon.CustomName;
					break;
				}
			}

			string player = entity.DisplayName.Replace("CommRelay", "");
			long playerId = long.Parse(player);
			ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerId(playerId);

			Wrapper.GameAction(() =>
			{
				entity.DisplayName = "";
				BaseEntityNetworkManager.BroadcastRemoveEntity(entity, false);
			});

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
			entity.PositionAndOrientation = new MyPositionAndOrientation(new Vector3D(MathUtility.GenerateRandomCoord(50000), MathUtility.GenerateRandomCoord(50000), MathUtility.GenerateRandomCoord(50000)), Vector3.Forward, Vector3.Up);

			List<string> commands = new List<string>();
			// Give a list of commands
			foreach(ChatHandlerBase chatBase in Essentials.ChatHandlers)
			{
				if (chatBase.GetMultipleCommandText().Length > 0)
				{
					foreach (string cmd in chatBase.GetMultipleCommandText())
					{
						string[] command = cmd.Split(new char[] { ' ' }, 2);
						if (!commands.Contains(command[0]))
							commands.Add(command[0]);
					}
				}
				else
				{
					string[] command = chatBase.GetCommandText().Split(new char[] { ' ' }, 2);
					if (!commands.Contains(command[0]))
						commands.Add(command[0]);
				}
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
				MyObjectBuilder_Beacon beacon = block as MyObjectBuilder_Beacon;
				if (beacon != null)
				{
					beacon.CustomName = finalText;
				}
			}

			SectorObjectManager.Instance.AddEntity(entity);
		}
	}
}

