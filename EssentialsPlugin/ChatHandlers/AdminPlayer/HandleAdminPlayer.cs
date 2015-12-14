namespace EssentialsPlugin.ChatHandlers
{
	using System;
	using System.Collections.Generic;
	using EssentialsPlugin.Utility;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using VRage.ModAPI;

	public class HandleAdminPlayer : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "";
		}
		public override string GetCommandText()
		{
			return "/admin player";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\""+GetHelp()+"\" \"close\" ";
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

		// /admin movefrom x y z x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
			});

			HashSet<long> playerOwners = new HashSet<long>();
			foreach (IMyEntity entity in entities)
			{
				IMyCubeGrid grid = (IMyCubeGrid)entity;
				MyObjectBuilder_CubeGrid gridBlock = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder();

				foreach(MyObjectBuilder_CubeBlock block in gridBlock.CubeBlocks)
				{
					if(block.Owner == 0)
						continue;

					if (!playerOwners.Contains(block.Owner))
						playerOwners.Add(block.Owner);
				}
			}

			Communication.SendPrivateInformation(userId, string.Format("Total block owners: {0}", playerOwners.Count));

			int count = 0;
			foreach(long owner in playerOwners)
			{
				ulong steamId = PlayerMap.Instance.GetPlayerItemFromPlayerId(owner).SteamId;
				if (steamId == 0)
					count++;
			}

			Communication.SendPrivateInformation(userId, string.Format("Total owners without a steam Id: {0}", count));
			HashSet<long> badPlayers = new HashSet<long>();
			HashSet<long> noLogin = new HashSet<long>();

			foreach(long owner in playerOwners)
			{
				MyObjectBuilder_Checkpoint.PlayerItem item = PlayerMap.Instance.GetPlayerItemFromPlayerId(owner);
				if(item.SteamId == 0)
					continue;

				if (!Players.Instance.PlayerLogins.ContainsKey(item.SteamId))
				{
					Communication.SendPrivateInformation(userId, string.Format("No login information: {0}", item.Name));
					noLogin.Add(owner);
					continue;
				}

				PlayerItem playerItem = Players.Instance.PlayerLogins[item.SteamId];
				if (DateTime.Now - playerItem.LastLogin > TimeSpan.FromDays(20))
				{
					Communication.SendPrivateInformation(userId, string.Format("Player hasn't logged in 20 days: {0}", item.Name));
					badPlayers.Add(owner);
				}
			}

			Communication.SendPrivateInformation(userId, string.Format("Users not logged in the last 20 days: {0}", badPlayers.Count));
			Communication.SendPrivateInformation(userId, string.Format("Users with no login information: {0}", noLogin.Count));

			/*
			count = 0;
			List<CubeGridEntity> grids = SectorObjectManager.Instance.GetTypedInternalData<CubeGridEntity>();
			foreach(CubeGridEntity grid in grids)
			{
				Thread.Sleep(100);
				foreach (CubeBlockEntity block in grid.CubeBlocks)
				{
					MyObjectBuilder_CubeBlock blockBuilder = (MyObjectBuilder_CubeBlock)block.Export();
					if (badPlayers.Contains(blockBuilder.Owner) || noLogin.Contains(blockBuilder.Owner))
					{
						//grid.DeleteCubeBlock(block);
						block.Dispose();
						count++;
					}
				}
			}
			*/

			Wrapper.GameAction(() =>
			{
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);

				foreach(IMyEntity entity in entities)
				{
					IMyCubeGrid grid = (IMyCubeGrid)entity;
					List<IMySlimBlock> blocks = new List<IMySlimBlock>();
					grid.GetBlocks(blocks, x => x.FatBlock != null && x.FatBlock.OwnerId != 0);
					foreach(IMySlimBlock block in blocks)
					{
						IMyCubeBlock cubeBlock = (IMyCubeBlock)block.FatBlock;
						if (badPlayers.Contains(cubeBlock.OwnerId) || noLogin.Contains(cubeBlock.OwnerId))
						{
							grid.RazeBlock(cubeBlock.Min);
							count++;
						}
					}					
				}
			});

			Communication.SendPrivateInformation(userId, string.Format("Blocks disposed: {0}", count));

			return true;
		}
	}
}
