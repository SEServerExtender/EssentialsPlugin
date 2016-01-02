namespace EssentialsPlugin.ChatHandlers
{
    using System;
    using EssentialsPlugin.Utility;

    public class HandleAdminBackup : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This will force an immediate save game backup using backup settings. Usage: /admin backup";
		}

		public override string GetCommandText()
		{
			return "/admin backup";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "";
            DialogItem.content = GetHelp( );
            DialogItem.buttonText = "close";
            return DialogItem;
        }

        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			Communication.SendPrivateInformation(userId, string.Format("Creating a save game backup ..."));
			Backup.Create(PluginSettings.Instance.BackupBaseDirectory, PluginSettings.Instance.BackupCreateSubDirectories, PluginSettings.Instance.BackupDateFormat, PluginSettings.Instance.BackupDateFormatSubDirectory, PluginSettings.Instance.BackupAsteroids, PluginSettings.Instance.BackupEssentials);
			Communication.SendPrivateInformation(userId, string.Format("Save game backup created"));
			return true;
		}
	}
}
