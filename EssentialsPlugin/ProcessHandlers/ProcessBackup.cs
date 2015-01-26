using System;
using System.IO;
using EssentialsPlugin.Utility;
using EssentialsPlugin.Settings;

namespace EssentialsPlugin.ProcessHandler
{
	class ProcessBackup : ProcessHandlerBase
	{
		private DateTime m_start = DateTime.Now;

		public ProcessBackup()
		{
			m_start = DateTime.Now;
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
						if (item.Hour == -1)
						{
							if (item.Minute == DateTime.Now.Minute)
								Backup.Create(PluginSettings.Instance.BackupBaseDirectory, PluginSettings.Instance.BackupCreateSubDirectories, PluginSettings.Instance.BackupAsteroids, PluginSettings.Instance.BackupEssentials);
						}
						else
						{
							if (item.Hour == DateTime.Now.Hour && item.Minute == DateTime.Now.Minute)
								Backup.Create(PluginSettings.Instance.BackupBaseDirectory, PluginSettings.Instance.BackupCreateSubDirectories, PluginSettings.Instance.BackupAsteroids, PluginSettings.Instance.BackupEssentials);
						}
					}

					// Cleanup Backups
					if (PluginSettings.Instance.BackupCleanup)
						CleanupBackups();
				}
			}
			catch (Exception ex)
			{
				Logging.WriteLineAndConsole(string.Format("ProcessBackup.Handle(): {0}", ex.ToString()));
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
						Logging.WriteLineAndConsole(string.Format("Removed old backup: {0}", file));
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
					Logging.WriteLineAndConsole(string.Format("Removed old backup: {0}", file));
					File.Delete(file);
					continue;
				}
			}
		}
	}
}
