namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.IO;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;

	class ProcessBackup : ProcessHandlerBase
	{
		public ProcessBackup()
		{
			
		}
		public override int GetUpdateResolution()
		{
			return 60000;
		}

		public override void Handle()
		{
			try
			{
				if (PluginSettings.Instance.BackupEnabled)
				{
					// Create Backups
					foreach (BackupItem item in PluginSettings.Instance.BackupItems)
					{
						if (!item.Enabled)
							continue;

						// On the minute
						if (item.Minute == DateTime.Now.Minute && (item.Hour == -1 || item.Hour == DateTime.Now.Hour))
								Backup.Create(PluginSettings.Instance.BackupBaseDirectory, PluginSettings.Instance.BackupCreateSubDirectories, PluginSettings.Instance.BackupDateFormat, PluginSettings.Instance.BackupDateFormatSubDirectory, PluginSettings.Instance.BackupAsteroids, PluginSettings.Instance.BackupEssentials);
					}

					// Cleanup Backups
					if (PluginSettings.Instance.BackupCleanup)
						CleanupBackups();
				}
			}
			catch (Exception ex)
			{
				Essentials.Log.Error( ex );
			} 
			
			base.Handle();
		}

		private void CleanupBackups()
		{
			string baseDirectory = PluginSettings.Instance.BackupBaseDirectory;

			string[] subDirectories = Directory.GetDirectories(baseDirectory);
			foreach (string path in subDirectories)
			{
				string[] files = Directory.GetFiles(path);
				foreach (string file in files)
				{
					FileInfo info = new FileInfo(file);
					if(DateTime.Now - info.CreationTime >= TimeSpan.FromDays(PluginSettings.Instance.BackupCleanupTime))
					{
						Essentials.Log.Info( "Removed old backup: {0}", file );
						File.Delete(file);
						Directory.Delete(path);
						break;
					}
				}
			}

			foreach(string file in Directory.GetFiles(baseDirectory))
			{
				FileInfo info = new FileInfo(file);
				if (DateTime.Now - info.CreationTime >= TimeSpan.FromDays(PluginSettings.Instance.BackupCleanupTime))
				{
					Essentials.Log.Info( "Removed old backup: {0}", file );
					File.Delete(file);
					continue;
				}
			}
		}
	}
}
