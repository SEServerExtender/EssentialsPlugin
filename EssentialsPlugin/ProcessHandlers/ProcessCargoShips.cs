namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Net;
	using System.Threading;
	using System.Windows.Forms;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox;
	using Sandbox.ModAPI;
	using SEModAPIExtensions.API;
	using SEModAPIInternal.API.Common;
	using SteamSDK;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;
	using VRageMath;

	class ProcessCargoShips : ProcessHandlerBase
	{
		public override int GetUpdateResolution()
		{
			return (int)(PluginSettings.Instance.CargoShipSpawnTime * 60000);
		}
        
	    public override void Handle()
		{
			if(!PluginSettings.Instance.CargoShipsEnabled)
                return;

			CargoShips.SpawnCargoShip(  );
		}
	}
}
