using System;
using System.IO;
using SEModAPIExtensions.API;
using System.IO.Compression;

namespace EssentialsPlugin.Utility
{
	static class Backup
	{
		static public void Create(string baseDirectory, bool createSubDirectories, bool backupAsteroids, bool backupSettings = false)
		{
			Logging.WriteLineAndConsole(string.Format("Creating backup ..."));
			//string baseDirectory = PluginSettings.Instance.BackupBaseDirectory;
			if (!Directory.Exists(baseDirectory))
			{
				Directory.CreateDirectory(baseDirectory);
			}

			var savePath = Server.Instance.Config.LoadWorld;

			string finalDirectory = baseDirectory;
			//if (PluginSettings.Instance.BackupCreateSubDirectories)
			if(createSubDirectories)
			{
				string subDirectory = string.Format("Backup-{0}", DateTime.Now.ToString("d-M-yyyy-HH-mm"));
				if (!Directory.Exists(baseDirectory + "\\" + subDirectory))
					Directory.CreateDirectory(baseDirectory + "\\" + subDirectory);

				finalDirectory = baseDirectory + "\\" + subDirectory;
			}

			string tempDirectory = baseDirectory + "\\" + "temp";
			if (!Directory.Exists(tempDirectory))
				Directory.CreateDirectory(tempDirectory);

			File.Copy(savePath + "\\" + "SANDBOX_0_0_0_.sbs", tempDirectory + "\\" + "SANDBOX_0_0_0_.sbs", true);
			File.Copy(savePath + "\\" + "Sandbox.sbc", tempDirectory + "\\" + "Sandbox.sbc", true);

			//if (PluginSettings.Instance.BackupAsteroids)
			if(backupAsteroids)
			{
				foreach (string file in Directory.GetFiles(savePath))
				{
					FileInfo info = new FileInfo(file);
					if (!info.Extension.Equals(".vx2"))
						continue;

					File.Copy(file, tempDirectory + "\\" + info.Name, true);
				}
			}

			if (backupSettings)
			{
				if(!Directory.Exists(tempDirectory + "\\Essentials"))
					Directory.CreateDirectory(tempDirectory + "\\Essentials");

				foreach(string file in Directory.GetFiles(Essentials.PluginPath))
				{
					FileInfo info = new FileInfo(file);
					if(!info.Extension.ToLower().Equals(".xml"))
						continue;

					File.Copy(file, tempDirectory + "\\Essentials\\" + info.Name, true);					
				}
			}

			ZipFile.CreateFromDirectory(tempDirectory, finalDirectory + "\\" + string.Format("Backup-{0}", DateTime.Now.ToString("d-M-yyyy-HH-mm")) + ".zip");

			foreach (string file in Directory.GetFiles(tempDirectory))
			{
				File.Delete(file);
			}

			Logging.WriteLineAndConsole(string.Format("Backup created: {0}", finalDirectory + "\\" + string.Format("Backup-{0}", DateTime.Now.ToString("d-M-yyyy-hh-mm")) + ".zip"));
		}
	}
}
