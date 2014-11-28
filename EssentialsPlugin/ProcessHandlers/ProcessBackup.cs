using System;
using System.IO;
using EssentialsPlugin.Utility;
using SEModAPIExtensions.API;
using System.IO.Compression;
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
								CreateBackup();
						}
						else
						{
							if (item.Hour == DateTime.Now.Hour && item.Minute == DateTime.Now.Minute)
								CreateBackup();
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

		private void CreateBackup()
		{
			Logging.WriteLineAndConsole(string.Format("Creating backup ..."));
			string baseDirectory = PluginSettings.Instance.BackupBaseDirectory;
			if (!Directory.Exists(baseDirectory))
			{
				Directory.CreateDirectory(baseDirectory);
			}

			var savePath = Server.Instance.Config.LoadWorld;

			string finalDirectory = baseDirectory;
			if(PluginSettings.Instance.BackupCreateSubDirectories)
			{
				string subDirectory = string.Format("Backup-{0}", DateTime.Now.ToString("d-M-yyyy-HH-mm"));
				if (!Directory.Exists(baseDirectory + "\\" + subDirectory))
					Directory.CreateDirectory(baseDirectory + "\\" + subDirectory);

				finalDirectory = baseDirectory + "\\" + subDirectory;
			}

			string tempDirectory = baseDirectory + "\\" + "temp";
			if(!Directory.Exists(tempDirectory))
				Directory.CreateDirectory(tempDirectory);


			File.Copy(savePath + "\\" + "SANDBOX_0_0_0_.sbs", tempDirectory + "\\" + "SANDBOX_0_0_0_.sbs", true);
			File.Copy(savePath + "\\" + "Sandbox.sbc", tempDirectory + "\\" + "Sandbox.sbc", true);

			if(PluginSettings.Instance.BackupAsteroids)
			{
				foreach (string file in Directory.GetFiles(savePath))
				{
					FileInfo info = new FileInfo(file);
					if (!info.Extension.Equals(".vx2"))
						continue;

					File.Copy(file, tempDirectory + "\\" + info.Name, true);
				}
			}

			ZipFile.CreateFromDirectory(tempDirectory, finalDirectory + "\\" + string.Format("Backup-{0}", DateTime.Now.ToString("d-M-yyyy-HH-mm")) + ".zip");

			foreach(string file in Directory.GetFiles(tempDirectory))
			{
				File.Delete(file);
			}

			Logging.WriteLineAndConsole(string.Format("Backup created: {0}", finalDirectory + "\\" + string.Format("Backup-{0}", DateTime.Now.ToString("d-M-yyyy-hh-mm")) + ".zip"));
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
