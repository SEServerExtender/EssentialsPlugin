namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Settings;
	using Utility;
	using SEModAPI.API.Utility;

    class ProcessCleanup : ProcessHandlerBase
	{
		private SettingsCleanupTriggerItem _triggeredItem;
		private DateTime _start = DateTime.Now;
		private readonly TimeSpan _oneSecond = new TimeSpan( 0, 0, 1 );
		private readonly TimeSpan _twentySeconds = new TimeSpan( 0, 0, 20 );
		private readonly TimeSpan _oneMinute = new TimeSpan( 0, 1, 0 );
		private readonly TimeSpan _fiveMinutes = new TimeSpan( 0, 5, 0 );

		public override int GetUpdateResolution( )
		{
			return 1000;
		}

		public override void Handle( )
		{
			try
			{
				if ( !PluginSettings.Instance.CleanupEnabled )
					return;

				foreach ( SettingsCleanupTimedItem item in PluginSettings.Instance.CleanupTimedItems.Where( item => item.Enabled ) )
				{
					ProcessTimedItem( item );
				}

				foreach ( SettingsCleanupTriggerItem item in PluginSettings.Instance.CleanupTriggerItems.Where( item => item.Enabled ) )
				{
					ProcessTriggerItem( item );
				}
			}
			catch ( OverflowException ex )
			{
				Essentials.Log.Error( ex );
			}

			base.Handle( );
		}

		/// <exception cref="OverflowException"></exception>
		private void ProcessTimedItem( SettingsCleanupTimedItem item )
		{
			_start = DateTime.Now; // this needs to be updated for each run so multi-day runtimes are handled properly
		    DateTime itemTime;

		    if ( !item.Interval )
		        itemTime = new DateTime( _start.Year, _start.Month, _start.Day, item.Restart.Hour, item.Restart.Minute, 0 );
		    else
		        itemTime = new DateTime( _start.Year, _start.Month, _start.Day, _start.Hour, item.Restart.Minute, 0 );

		    if ( DateTime.Now - itemTime > _twentySeconds )
		    {
		        itemTime = itemTime.AddDays( 1 );
		    }

		    if ( DateTime.Now - item.LastRan < _oneMinute )
				return;

			if ( itemTime - DateTime.Now < _oneSecond && DateTime.Now - item.LastRan > _oneMinute )
			{
				string command = $"{item.ScanCommand} quiet";
				HashSet<GridGroup> groups = CubeGrids.ScanGrids( 0, CommandParser.GetCommandParts( command ).ToArray( ) );
                
			    int groupCount = groups.Count;
			    int gridCount = 0;
			    foreach ( var group in groups )
			    {
			        gridCount += group.Grids.Count;
                    group.Close(  );
			    }
				Communication.SendPublicInformation( $"[NOTICE]: Timed cleanup has run. {gridCount} grids in {groupCount} groups removed." );
				item.LastRan = DateTime.Now;
				item.NotificationItemsRan.Clear( );
				return;
			}

			foreach ( SettingsCleanupNotificationItem notifyItem in PluginSettings.Instance.CleanupNotificationItems )
			{
				if ( item.NotificationItemsRan.Contains( notifyItem ) )
					continue;

				if ( itemTime - DateTime.Now < TimeSpan.FromMinutes( notifyItem.MinutesBeforeCleanup ) )
				{
					item.NotificationItemsRan.Add( notifyItem );

					if ( DateTime.Now - notifyItem.lastRan > _oneMinute )
					{
						notifyItem.lastRan = DateTime.Now;
						string notification = notifyItem.Message.Replace( "%cleanup_reason%", item.Reason );
						Communication.SendPublicInformation( notification );
					}

				}
			}
		}

		/// <exception cref="OverflowException"></exception>
		private void ProcessTriggerItem( SettingsCleanupTriggerItem item )
		{
			if ( _triggeredItem != null && _triggeredItem != item )
				return;

			if ( _triggeredItem == null )
			{
				// Increase to 5 minutes
				if ( DateTime.Now - item.LastRan > _fiveMinutes )
				{
					item.LastRan = DateTime.Now;
					string command = item.ScanCommand + " quiet";
					HashSet<GridGroup> groups = CubeGrids.ScanGrids( 0, CommandParser.GetCommandParts( command ).ToArray( ) );
				    int gridsCount = 0;
				    foreach (var group in groups)
				        gridsCount += group.Grids.Count;

                    if ( gridsCount >= item.MaxCapacity )
					{
						Communication.SendPublicInformation( $"[NOTICE]: Cleanup triggered.  ({gridsCount} of {item.MaxCapacity}) triggered grids found.  Cleanup will run in {item.MinutesAfterCapacity} minutes.{(item.Reason == string.Empty ? "" : $"  Reason: {item.Reason}")}" );
						item.NotificationItemsRan.Clear( );
						_triggeredItem = item;
					}
				}
			}
			else
			{
				if ( DateTime.Now - item.LastRan > TimeSpan.FromMinutes( item.MinutesAfterCapacity ) )
				{
					string command = item.ScanCommand + " quiet";
					HashSet<GridGroup> groups = CubeGrids.ScanGrids( 0, CommandParser.GetCommandParts( command ).ToArray( ) );

                    int groupCount = groups.Count;
                    int gridCount = 0;
                    foreach (var group in groups)
                    {
                        gridCount += group.Grids.Count;
                        group.Close();
                    }
                    Communication.SendPublicInformation( $"[NOTICE]: Triggered cleanup has run. {gridCount} grids in {groupCount} groups removed." );
					_triggeredItem = null;
					return;
				}

				foreach ( SettingsCleanupNotificationItem notifyItem in PluginSettings.Instance.CleanupNotificationItems )
				{
					if ( item.NotificationItemsRan.Contains( notifyItem ) )
						continue;

					if ( notifyItem.MinutesBeforeCleanup > item.MinutesAfterCapacity )
						continue;

					if ( DateTime.Now - item.LastRan > TimeSpan.FromMinutes( item.MinutesAfterCapacity - notifyItem.MinutesBeforeCleanup ) )
					{
						item.NotificationItemsRan.Add( notifyItem );
						string notification = notifyItem.Message.Replace( "%cleanup_reason%", item.Reason );
						Communication.SendPublicInformation( notification );
					}
				}
			}
		}
	}
}
