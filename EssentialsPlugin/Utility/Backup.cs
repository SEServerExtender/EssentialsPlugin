namespace EssentialsPlugin.Utility
{
	using System;
	using System.IO;
	using System.IO.Compression;
	using SEModAPIExtensions.API;

	static class Backup
	{
		static public void Create(string baseDirectory, bool createSubDirectories, string dateFormat, string dateFormatSubDirectory, bool backupAsteroids, bool backupSettings = false)
		{
			Essentials.Log.Info( "Creating backup ..." );
			//string baseDirectory = PluginSettings.Instance.BackupBaseDirectory;
			if (!Directory.Exists(baseDirectory))
			{
				Directory.CreateDirectory(baseDirectory);
			}

			string savePath = Server.Instance.Config.LoadWorld;

			string finalDirectory = baseDirectory;
			//if (PluginSettings.Instance.BackupCreateSubDirectories)
			if(createSubDirectories)
			{
				string subDirectory = string.Format("Backup_{0}", DateTime.Now.ToString(dateFormatSubDirectory));
				if (!Directory.Exists(baseDirectory + "\\" + subDirectory))
					Directory.CreateDirectory(baseDirectory + "\\" + subDirectory);

				finalDirectory = baseDirectory + "\\" + subDirectory;
			}

			string tempDirectory = baseDirectory + "\\" + "temp";
			if (!Directory.Exists(tempDirectory))
				Directory.CreateDirectory(tempDirectory);

			File.Copy(Path.Combine( savePath, "SANDBOX_0_0_0_.sbs" ), Path.Combine( tempDirectory, "SANDBOX_0_0_0_.sbs" ), true);
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

			string fileName = string.Format("Backup_{0}.zip", DateTime.Now.ToString(dateFormat));
			ZipFile.CreateFromDirectory(tempDirectory, finalDirectory + "\\" + fileName);

			Directory.Delete(tempDirectory, true);

			Essentials.Log.Info( "Backup created: {0}\\{1}", finalDirectory, fileName );
		}
	}
}
