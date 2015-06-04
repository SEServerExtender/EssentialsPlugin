namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using EssentialsPlugin.EntityManagers;

	public class ProcessTurrets : ProcessHandlerBase
	{
		private static DateTime _lastEnableCheck;
		private static DateTime _lastDisableCheck;

		public ProcessTurrets( )
		{
			//EntityManagement.RevealAll();
		}

		public override int GetUpdateResolution( )
		{
			return 1000;
		}

		public override void Handle( )
		{
			if ( PluginSettings.Instance.DynamicTurretManagmentEnabled )
			{
				if ( DateTime.Now - _lastEnableCheck > TimeSpan.FromSeconds( 6 ) )
				{
					TurretManagement.CheckAndEnableTurrets( );
					_lastEnableCheck = DateTime.Now;
				}

				if ( DateTime.Now - _lastDisableCheck > TimeSpan.FromSeconds( 45 ) )
				{
					TurretManagement.CheckAndDisableTurrets( );
					_lastDisableCheck = DateTime.Now;
				}
			}

			base.Handle( );
		}
	}
}