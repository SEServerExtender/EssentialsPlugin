using System;
using System.Linq;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;
using VRage.Common.Utils;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.Support;

using EssentialsPlugin.UtilityClasses;

namespace EssentialsPlugin.Utility
{
	public static class Player
	{
		public static bool Move(string userName, Vector3D position)
		{
			CharacterEntity charEntity = SectorObjectManager.Instance.GetTypedInternalData<CharacterEntity>().Where(x => x.DisplayName.ToLower() == userName.ToLower() && x.Health > 0).First();
			CubeGridEntity gridEntity = new CubeGridEntity(new FileInfo(Essentials.PluginPath + "MovePlayer.sbc"));
			gridEntity.EntityId = BaseEntity.GenerateEntityId();
			foreach (MyObjectBuilder_CubeBlock block in gridEntity.BaseCubeBlocks)
			{
				// set ownership
				if (block is MyObjectBuilder_Cockpit)
				{
					MyObjectBuilder_Cockpit cockpit = (MyObjectBuilder_Cockpit)block;
					cockpit.Pilot = (MyObjectBuilder_Character)charEntity.Export();
				}
			}

			gridEntity.PositionAndOrientation = new MyPositionAndOrientation(position, Vector3.Forward, Vector3.Up);

			Wrapper.GameAction(() =>
			{
				MyObjectBuilder_EntityBase baseEntity = gridEntity.Export();
				IMyEntity entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(baseEntity);
				Type someManager = SandboxGameAssemblyWrapper.Instance.GetAssemblyType(SectorObjectManager.EntityBaseNetManagerNamespace, SectorObjectManager.EntityBaseNetManagerClass);
				Wrapper.InvokeStaticMethod(someManager, SectorObjectManager.EntityBaseNetManagerSendEntity, new object[] { entity.GetObjectBuilder() });
				gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
			});


			int count = 0;
			while (gridEntity.IsLoading)
			{
				Thread.Sleep(100);
				count++;
				if (count > 40)
					break;
			}

			if (gridEntity.IsLoading)
			{
				Logging.WriteLineAndConsole(string.Format("Failed to load cockpit entity: {0}", gridEntity.EntityId));
				return false;
			}

			foreach (CubeBlockEntity block in gridEntity.CubeBlocks)
			{
				if (block is CockpitEntity)
				{
					block.IntegrityPercent = 0.1f;
				}
			}

			gridEntity.Dispose();
			return true;
		}

		public static Boolean CheckPlayerSameFaction(long playerId, long compareId)
		{
			if (compareId == playerId)
				return true;

			MyObjectBuilder_FactionCollection m_factionCollection = (MyObjectBuilder_FactionCollection)InvokeEntityMethod(FactionsManager.Instance.BackingObject, FactionsManager.FactionManagerGetFactionCollectionMethod);

			if (m_factionCollection == null)
			{
				Console.WriteLine("No faction collection");
				return false;
			}

			if (m_factionCollection.Factions == null)
			{
				Console.WriteLine("No factions");
				return false;
			}

			MyObjectBuilder_Faction faction = m_factionCollection.Factions.FirstOrDefault(f => f.Members.FirstOrDefault(m => m.PlayerId == playerId).PlayerId != 0);
			if (faction != null)
			{
				if (faction.Members.FirstOrDefault(m => m.PlayerId == compareId).PlayerId != 0)
				{
					return true;
				}
			}

			return false;
		}

		public static Boolean CheckPlayerSameFaction(ulong steamId, ulong steamCompareId)
		{
			long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId(steamId).FirstOrDefault();
			long compareId = PlayerMap.Instance.GetPlayerIdsFromSteamId(steamCompareId).FirstOrDefault();
			return CheckPlayerSameFaction(playerId, compareId);
		}

		internal static Object InvokeEntityMethod(Object gameEntity, string methodName)
		{
			return InvokeEntityMethod(gameEntity, methodName, new object[] { });
		}

		internal static Object InvokeEntityMethod(Object gameEntity, string methodName, Object[] parameters)
		{
			return InvokeEntityMethod(gameEntity, methodName, parameters, null);
		}

		internal static Object InvokeEntityMethod(Object gameEntity, string methodName, Object[] parameters, Type[] argTypes)
		{
			try
			{
				MethodInfo method = GetEntityMethod(gameEntity, methodName, argTypes);
				if (method == null)
					throw new Exception("Method is empty");
				Object result = method.Invoke(gameEntity, parameters);

				return result;
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLine("Failed to invoke entity method '" + methodName + "' on type '" + gameEntity.GetType().FullName + "': " + ex.Message);

				if (SandboxGameAssemblyWrapper.IsDebugging)
					LogManager.ErrorLog.WriteLine(Environment.StackTrace);

				LogManager.ErrorLog.WriteLine(ex);
				return null;
			}
		}

		internal static MethodInfo GetEntityMethod(Object gameEntity, string methodName)
		{
			try
			{
				if (gameEntity == null)
					throw new Exception("Game entity was null");
				if (methodName == null || methodName.Length == 0)
					throw new Exception("Method name was empty");
				MethodInfo method = gameEntity.GetType().GetMethod(methodName);
				if (method == null)
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = gameEntity.GetType();
					while (type != typeof(Object))
					{
						method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
						if (method != null)
							break;

						type = type.BaseType;
					}
				}
				if (method == null)
					throw new Exception("Method not found");
				return method;
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLine("Failed to get entity method '" + methodName + "': " + ex.Message);
				if (SandboxGameAssemblyWrapper.IsDebugging)
					LogManager.ErrorLog.WriteLine(Environment.StackTrace);
				LogManager.ErrorLog.WriteLine(ex);
				return null;
			}
		}

		internal static MethodInfo GetEntityMethod(Object gameEntity, string methodName, Type[] argTypes)
		{
			try
			{
				if (argTypes == null || argTypes.Length == 0)
					return GetEntityMethod(gameEntity, methodName);

				if (gameEntity == null)
					throw new Exception("Game entity was null");
				if (methodName == null || methodName.Length == 0)
					throw new Exception("Method name was empty");
				MethodInfo method = gameEntity.GetType().GetMethod(methodName, argTypes);
				if (method == null)
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = gameEntity.GetType();
					while (type != typeof(Object))
					{
						method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null);
						if (method != null)
							break;

						type = type.BaseType;
					}
				}
				if (method == null)
					throw new Exception("Method not found");
				return method;
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLine("Failed to get entity method '" + methodName + "': " + ex.Message);
				if (SandboxGameAssemblyWrapper.IsDebugging)
					LogManager.ErrorLog.WriteLine(Environment.StackTrace);
				LogManager.ErrorLog.WriteLine(ex);
				return null;
			}
		}
	}

	public class PlayerItem : IEquatable<PlayerItem>
	{
		private ulong steamId;
		public ulong SteamId
		{
			get { return steamId; }
			set { steamId = value; }
		}

		private DateTime lastLogin;
		public DateTime LastLogin
		{
			get { return lastLogin; }
			set { lastLogin = value; }
		}

		public PlayerItem()
		{
			steamId = 0;
			lastLogin = DateTime.Now;
		}


		#region IEquatable<PlayerItem> Members

		public bool Equals(PlayerItem other)
		{
			return steamId.Equals(other.steamId);
		}

		public override int GetHashCode()
		{
			return steamId.GetHashCode();
		}
		#endregion
	}

	public class Players
	{
		private static volatile bool m_checking = false;
		private static Players m_instance;
		public static Players Instance
		{
			get 
			{
				if (m_instance == null)
				{
					Load();
				}

				return Players.m_instance; 
			}
		}

		private SerializableDictionary<ulong, PlayerItem> playerLogins;
		public SerializableDictionary<ulong, PlayerItem> PlayerLogins
		{
			get { return playerLogins; }
			set { playerLogins = value; }
		}

		public Players()
		{
			playerLogins = new SerializableDictionary<ulong, PlayerItem>();
		}

		private static void Load()
		{
			try
			{
				String fileName = Essentials.PluginPath + "Essential-PlayerLogins.xml";
				if (File.Exists(fileName))
				{
					using (StreamReader reader = new StreamReader(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(Players));
						m_instance = (Players)x.Deserialize(reader);
						reader.Close();
					}
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("LoginTracking Load Error: {0}", ex.ToString()));
			}
			finally
			{
				m_instance = new Players();
			}
		}

		private void Save()
		{
			try
			{
				lock (this)
				{
					String fileName = Essentials.PluginPath + "Essential-PlayerLogins.xml";
					using (StreamWriter writer = new StreamWriter(fileName))
					{
						XmlSerializer x = new XmlSerializer(typeof(Players));
						x.Serialize(writer, m_instance);
						writer.Close();
					}
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("Error saving player logins: {0}", ex.ToString()));
			}
		}

		public void UpdatePlayers(List<PlayerItem> playerItems)
		{
			lock(Instance)
			{
				foreach(PlayerItem item in playerItems)
				{
					if (playerLogins.ContainsKey(item.SteamId))
					{
						if (item.LastLogin > playerLogins[item.SteamId].LastLogin)
							UpdatePlayer(item);
					}
					else
						AddPlayer(item);
				}

				Save();
			}
		}

		public void UpdatePlayerTime(ulong steamId)
		{
			PlayerItem item;
			lock (m_instance)
			{
				if (!playerLogins.ContainsKey(steamId))
				{
					PlayerItem newItem = new PlayerItem();
					newItem.SteamId = steamId;
					newItem.LastLogin = DateTime.Now;
					playerLogins.Add(steamId, newItem);
					return;
				}

				item = playerLogins[steamId];
			}

			item.LastLogin = DateTime.Now;
			Save();
		}

		public void AddPlayer(PlayerItem item, bool save = false)
		{
			lock (Instance)
			{
				playerLogins.Add(item.SteamId, item);

			}
			if (save)
				Save();
		}

		public void RemovePlayer(ulong steamId)
		{
			lock (Instance)
			{
				playerLogins.Remove(steamId);
			}

			Save();
		}

		public void UpdatePlayer(PlayerItem item, bool save=false)
		{
			lock(Instance)
			{
				if (!playerLogins.ContainsKey(item.SteamId))
					return;

				PlayerItem existingItem = playerLogins[item.SteamId];
				existingItem.LastLogin = item.LastLogin;
			}

			if (save)
				Save();
		}

		public List<PlayerItem> GetPlayers(Func<PlayerItem, bool> func = null)
		{
			List<PlayerItem> result = null;

			lock(Instance)
			{
				if (func == null)
					result = playerLogins.Select(x => x.Value).ToList();
				else
					result = playerLogins.Select(x => x.Value).Where(func).ToList();
			}

			return result;
		}

		public PlayerItem GetPlayerById(long playerId)
		{
			ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerId(playerId);
			if(steamId > 0)
			{
				if (playerLogins.ContainsKey(steamId))
					return playerLogins[steamId];
			}

			return null;
		}

		public static void ProcessServerLogsForLogins()
		{
			if (m_checking)
				return;

			m_checking = true;
			try
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(state =>
				{
					string logPath = MyFileSystem.UserDataPath;
					string[] files = Directory.GetFiles(logPath, "SpaceEngineersDedicated_*.log");
					List<PlayerItem> playerItems = new List<PlayerItem>();

					Logging.WriteLineAndConsole(string.Format("Checking {0} log files for logins", files.Length));
					DateTime start = DateTime.Now;
					Parallel.ForEach(files, currentFile =>
					{
						string fileText;
						try
						{
							fileText = File.ReadAllText(currentFile);
						}
						catch
						{
							return;
						}

						List<PlayerItem> results = GetSteamIdsFromFile(fileText);
						lock (playerItems)
							playerItems.AddRange(results);

					});

					Players.Instance.UpdatePlayers(playerItems);
					Logging.WriteLineAndConsole(string.Format("Completed checking logs in {0}s: {2} ({1}) steamIds", (DateTime.Now - start).TotalSeconds, playerItems.Count, Players.Instance.PlayerLogins.Count));
				}));
			}
			finally
			{
				m_checking = false;
			}
		}

		private static List<PlayerItem> GetSteamIdsFromFile(string fileText)
		{
			List<PlayerItem> playerItems = new List<PlayerItem>();
			Regex regexObj = new Regex(@"^(.*) - Thread:.*, owner: (\d*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
			Match matchResults = regexObj.Match(fileText);
			while (matchResults.Success)
			{
				PlayerItem item = new PlayerItem();
				item.LastLogin = DateTime.Parse(matchResults.Groups[1].Value);
				item.SteamId = ulong.Parse(matchResults.Groups[2].Value);
				playerItems.Add(item);

				matchResults = matchResults.NextMatch();
			}

			return playerItems;
		}
	}
}
