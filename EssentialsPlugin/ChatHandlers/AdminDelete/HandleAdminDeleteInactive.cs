namespace EssentialsPlugin.ChatHandlers.AdminDelete
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using VRage.ModAPI;

	public class HandleAdminDeleteInactive : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to delete all grids owned by players that are inactive.  Usage: /admin delete inactive [days]";
		}

		public override string GetCommandText()
		{
			return "/admin delete inactive";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\"Sorry, there's nothing here yet :(\" \"close\" ";
            return longMessage;
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
			if (words.Count() > 3)
				return false;

			if (!words.Any())
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			int days = -1;
			if (!int.TryParse(words[0], out days) || days < 0)
			{
				Communication.SendPrivateInformation(userId, string.Format("Invalid argument.  Days argument must be an integer that is 0 or greater."));
				return true;
			}

			// Just assume that anything after the days is going to "ignorenologin"
			bool removeNoLoginInformation = true;
			bool removeOwnerless = true;

			if (words.Count() > 1)
			{
				foreach (string word in words)
				{
					if (word.ToLower() == "ignorenologin")
						removeNoLoginInformation = false;
					if (word.ToLower() == "ignoreownerless")
						removeOwnerless = false;
				}
			}

			Communication.SendPrivateInformation(userId, string.Format("Scanning for grids with owners that haven't logged in {0} days.  (Must Have Login Info={1})", days, removeNoLoginInformation));

			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
			});

			HashSet<IMyEntity> entitiesFound = new HashSet<IMyEntity>();
			foreach (IMyEntity entity in entities)
			{
				if (!(entity is IMyCubeGrid))
					continue;

                if (entity.DisplayName.Contains("CommRelay"))
                    continue;

				IMyCubeGrid grid = (IMyCubeGrid)entity;
				CubeGridEntity gridEntity = (CubeGridEntity)GameEntityManager.GetEntity(grid.EntityId);
				MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(grid);
				if (gridBuilder == null)
					continue;

				// This entity is protected by whitelist
				if (PluginSettings.Instance.LoginEntityWhitelist.Length > 0 && PluginSettings.Instance.LoginEntityWhitelist.Contains(grid.EntityId.ToString()))
					continue;

				if (CubeGrids.GetAllOwners(gridBuilder).Count < 1 && removeOwnerless)
				{
					Communication.SendPrivateInformation(userId, string.Format("Found entity '{0}' ({1}) not owned by anyone.", gridEntity.Name, entity.EntityId));
					entitiesFound.Add(entity);
					continue;
				}

				foreach (long player in CubeGrids.GetBigOwners(gridBuilder))
				{
					// This playerId is protected by whitelist
					if (PluginSettings.Instance.LoginPlayerIdWhitelist.Length > 0 && PluginSettings.Instance.LoginPlayerIdWhitelist.Contains(player.ToString()))
						continue;

					MyObjectBuilder_Checkpoint.PlayerItem checkItem = PlayerMap.Instance.GetPlayerItemFromPlayerId(player);
					if (checkItem.IsDead || checkItem.Name == "")
					{
						Communication.SendPrivateInformation(userId, string.Format("Found entity '{0}' ({1}) owned by dead player - ID: {2}", gridEntity.Name, entity.EntityId, player));
						entitiesFound.Add(entity);
						continue;
					}

					PlayerItem item = Players.Instance.GetPlayerById(player);
					if (item == null)
					{
						if (removeNoLoginInformation)
						{
							Communication.SendPrivateInformation(userId, string.Format("Found entity '{0}' ({1}) owned by a player with no login info: {2}", gridEntity.Name, entity.EntityId, checkItem.Name));
							entitiesFound.Add(entity);
						}
					}
					else if (item.LastLogin < DateTime.Now.AddDays(days * -1))
					{
						Communication.SendPrivateInformation(userId, string.Format("Found entity '{0}' ({1}) owned by inactive player: {2}", gridEntity.Name, entity.EntityId, PlayerMap.Instance.GetPlayerItemFromPlayerId(player).Name));
						entitiesFound.Add(entity);
					}
				}
			}

			Communication.SendPrivateInformation(userId, string.Format("Found {0} grids owned by inactive users", entitiesFound.Count));

			foreach(IMyEntity entity in entitiesFound)
			{
				if (!(entity is IMyCubeGrid))
					continue;

				CubeGridEntity gridEntity = new CubeGridEntity((MyObjectBuilder_CubeGrid)entity.GetObjectBuilder(), entity);
				gridEntity.Dispose();
			}
			Communication.SendPrivateInformation(userId, string.Format("Removed {0} grids owned by inactive users", entitiesFound.Count()));
			return true;
		}
	}
}
