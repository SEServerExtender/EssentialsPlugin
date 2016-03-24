namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Threading;
	using System.Windows.Forms;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox;
	using Sandbox.ModAPI;
	using SEModAPIExtensions.API;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Server;
	using SteamSDK;
	using VRage.Game;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;
	using VRageMath;

	class ProcessTicket : ProcessHandlerBase
	{
		public override int GetUpdateResolution()
		{
			return 60000;
		}
        
	    public override void Handle()
		{
			if(!PluginSettings.Instance.ReservedSlotsEnabled)
                return;

            List<ulong> toRemove = new List<ulong>();

	        foreach ( KeyValuePair<ulong, DateTime> ticketPlayer in PluginSettings.Instance.TicketPlayers )
	        {
	            string playerName = PlayerMap.Instance.GetFastPlayerNameFromSteamId( ticketPlayer.Key );

                if ( ticketPlayer.Value - DateTime.Now < TimeSpan.Zero )
                {
                    Communication.Notification(0, MyFontEnum.Blue, 10, $"Goodbye {playerName}, thanks for joining us!" );
                    toRemove.Add( ticketPlayer.Key );
                    continue;
                }
	            if ( ticketPlayer.Value - DateTime.Now < TimeSpan.FromMinutes( 1 ) )
	            {
                    Communication.Notification(0, MyFontEnum.Blue, 10, $"{playerName}, has one minute left!");
	                continue;
	            }
	            if ( ticketPlayer.Value - DateTime.Now < TimeSpan.FromMinutes( 5 ) )
	            {
                    Communication.Notification(0, MyFontEnum.Blue, 10, $"{playerName}, has five minutes left!");
                }
            }

            Thread.Sleep( 10000 );

	        foreach ( ulong id in toRemove )
	        {
                ServerNetworkManager.Instance.KickPlayer( id );
	            if ( PluginSettings.Instance.ReservedSlotsPlayers.Remove( id ) )
	                PluginSettings.Instance.TicketPlayers.Remove( id );
	        }

		}
	}
}
