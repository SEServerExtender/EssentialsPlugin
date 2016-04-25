namespace EssentialsPlugin.ProcessHandlers
{
    using EssentialsPlugin.Utility;

    class ProcessCargoShips : ProcessHandlerBase
	{
		public override int GetUpdateResolution()
		{
			return (int)(PluginSettings.Instance.AtmosphericCargoShipSpawnTime * 60000);
		}
        
	    public override void Handle()
		{
			if(!PluginSettings.Instance.AtmosphericCargoShipsEnabled)
                return;

			CargoShips.SpawnCargoShip( false );
		}
	}
}
