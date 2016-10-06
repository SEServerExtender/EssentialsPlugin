namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using EssentialsPlugin.EntityManagers;

	public class ProcessTurrets : ProcessHandlerBase
	{
		public override int GetUpdateResolution( )
		{
			return 5000;
		}

		public override void Handle( )
		{
			if ( PluginSettings.Instance.DynamicTurretManagmentEnabled )
			{
			    TurretManagement.Instance.ProcessTurrets();
			}

			base.Handle( );
		}
	}
}