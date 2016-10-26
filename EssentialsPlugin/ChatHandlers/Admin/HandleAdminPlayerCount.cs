namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Sandbox.Definitions;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Engine.Voxels;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Blocks;
    using Sandbox.Game.Entities.Character;
    using Sandbox.Game.Entities.Character.Components;
    using Sandbox.Game.Replication;
    using Sandbox.Game.SessionComponents;
    using Sandbox.Game.World;
    using SEModAPIInternal.API.Server;
    using SpaceEngineers.Game.World;
    using Utility;
    using VRage.Game.Entity;
    using VRage.Network;

    public class HandleAdminPlayerCount : ChatHandlerBase
	{

    public override string GetHelp()
		{
			return "Sets the max number of players on the server";
		}

		public override string GetCommandText()
		{
			return "/admin playercount";
		}
        
        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}
        
        public override bool HandleCommand( ulong userId, string[ ] words )
        {
            if (words.Length == 0)
            {
                Communication.SendPrivateInformation( userId, $"Max player count: {MyMultiplayer.Static.MemberLimit}. Current player count: {MyMultiplayer.Static.MemberCount - 1}" );
                return true;
            }

            int count;
            if (!int.TryParse( words[0], out count ))
            {
                Communication.SendPrivateInformation( userId, GetHelp(  ) );
                return true;
            }

            MyMultiplayer.Static.MemberLimit = count;
            Communication.SendPrivateInformation(userId, $"Max player count: {MyMultiplayer.Static.MemberLimit}. Current player count: {MyMultiplayer.Static.MemberCount - 1}");

            return true;
        }

	}

}

