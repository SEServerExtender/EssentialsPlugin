namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using EssentialsPlugin.Settings;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using SEModAPIInternal.API.Common;
    using Utility;
    using VRage.Game;
    using VRage.Game.Entity;

    public class HandleRevoke : ChatHandlerBase
	{

    public override string GetHelp()
		{
			return "Revokes ownership of blocks handled by player block enforcement. Use /revoke list to get a list of available block types.";
		}

		public override string GetCommandText()
		{
			return "/revoke";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            var dialogItem = new Communication.ServerDialogItem
                             {
                                 title = "Help",
                                 header = "/revoke",
                                 buttonText = "close",
                                 content = "Revokes ownership of blocks handled by player block enforcement. |" +
                                           "Use /revoke list to get a list of block types you can revoke ownership of, " +
                                           "then use /revoke (blocktype) to revoke ownership of ALL blocks of that type. |" +
                                           "Blocks you revoke ownership of will be reduced to 1% build state and the components " +
                                           "will be left in place."
                             };
            return dialogItem;
        }

        public override bool IsAdminCommand()
		{
			return false;
		}

		public override bool AllowedInConsole()
		{
			return false;
		}

        public override bool HandleCommand( ulong userId, string[ ] words )
        {
            if (words.Length < 1)
            {
                Communication.SendPrivateInformation( userId, GetHelp() );
                return true;
            }

            if (!PluginSettings.Instance.PlayerBlockEnforcementEnabled)
            {
                //nothing to do here
                Communication.SendPrivateInformation( userId, "Player block enforcement is not enabled on this server" );
                return true;
            }

            if (words[0].ToLower() == "list")
            {
                StringBuilder message = new StringBuilder();
                message.Append("Available block types:|");
                foreach (var enf in PluginSettings.Instance.PlayerBlockEnforcementItems)
                {
                    switch (enf.Mode)
                    {
                        case SettingsBlockEnforcementItem.EnforcementMode.Off:
                            continue;

                        case SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId:
                        {
                            message.Append( $"{enf.BlockSubtypeId}|" );
                            continue;
                        }

                            case SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId:
                        {
                            message.Append( $"{enf.BlockTypeId}|" );
                            continue;
                        }
                    }
                }
                message.Append( "|Use /revoke (blocktype) to revoke ownership." );
                Communication.DisplayDialog( userId, "Help", "/revoke", message.ToString() );
                return true;
            }

            foreach (var enf in PluginSettings.Instance.PlayerBlockEnforcementItems)
            {
                switch (enf.Mode)
                {
                    case SettingsBlockEnforcementItem.EnforcementMode.Off:
                        continue;

                    case SettingsBlockEnforcementItem.EnforcementMode.BlockSubtypeId:
                    {
                        if (!enf.BlockSubtypeId.Contains( words[0], StringComparison.CurrentCultureIgnoreCase ))
                            continue;
                        RevokeOwnership( userId, enf.BlockSubtypeId, true );
                        return true;
                    }

                    case SettingsBlockEnforcementItem.EnforcementMode.BlockTypeId:
                    {
                        if (!enf.BlockTypeId.Contains( words[0], StringComparison.CurrentCultureIgnoreCase ))
                            continue;
                        RevokeOwnership( userId, enf.BlockTypeId, false );
                        return true;
                    }
                }
            }
            return true;
        }

        private void RevokeOwnership( ulong steamId, string blocktype, bool isSubtype )
        {
            long playerId = PlayerMap.Instance.GetFastPlayerIdFromSteamId( steamId );

            int counter = 0;

            HashSet<MyEntity> entities = new HashSet<MyEntity>();
            Wrapper.GameAction( () => entities = MyEntities.GetEntities() );

            foreach (var entity in entities)
            {
                var grid = entity as MyCubeGrid;
                if (grid == null)
                    continue;

                if (!grid.SmallOwners.Contains( playerId ))
                    continue;

                foreach (var block in grid.GetFatBlocks())
                {
                    if (block.OwnerId != playerId)
                        continue;

                    if (isSubtype && block.BlockDefinition.Id.SubtypeId.ToString().Contains( blocktype, StringComparison.CurrentCultureIgnoreCase ))
                    {
                        //set owner to nobody in block enforcement
                        lock(PlayerBlockEnforcement.BlockOwners)
                            if (PlayerBlockEnforcement.BlockOwners.ContainsKey( (MyTerminalBlock) block ))
                                PlayerBlockEnforcement.BlockOwners[(MyTerminalBlock) block] = 0;
                        
                        Wrapper.GameAction(() =>
                        {
                            //change owner to nobody and reduce build to 1%
                            block.ChangeOwner(0, MyOwnershipShareModeEnum.None);
                            block.SlimBlock.DecreaseMountLevelToDesiredRatio( 0.1f, null );
                        } );
                        counter++;
                    }

                    if (!isSubtype && block.BlockDefinition.Id.TypeId.ToString().Contains(blocktype, StringComparison.CurrentCultureIgnoreCase))
                    {
                        lock(PlayerBlockEnforcement.BlockOwners)
                            if (PlayerBlockEnforcement.BlockOwners.ContainsKey( (MyTerminalBlock) block ))
                                PlayerBlockEnforcement.BlockOwners[(MyTerminalBlock) block] = 0;

                        Wrapper.GameAction(() =>
                        {
                            block.ChangeOwner(0, MyOwnershipShareModeEnum.None);
                            block.SlimBlock.DecreaseMountLevelToDesiredRatio(0.1f, null);
                        });
                        counter++;
                    }
                }
            }

            Communication.SendPrivateInformation( steamId, $"Revoked ownership of {counter} {blocktype} blocks" );
        }
	}

}

