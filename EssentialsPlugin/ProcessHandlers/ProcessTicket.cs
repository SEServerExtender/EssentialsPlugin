namespace EssentialsPlugin.ProcessHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using EssentialsPlugin.Settings;
    using EssentialsPlugin.Utility;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.World;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Server;
    using VRage.Game;

    class ProcessTicket : ProcessHandlerBase
    {
        public override int GetUpdateResolution( )
        {
            return 60000;
        }

        public override void Handle( )
        {
            if ( !PluginSettings.Instance.ReservedSlotsEnabled )
                return;

            List<ulong> toRemove = new List<ulong>( );

            foreach ( TicketPlayerItem item in PluginSettings.Instance.TicketPlayers )
            {
                MyIdentity identity = MySession.Static.Players.TryGetIdentity( PlayerMap.Instance.GetFastPlayerIdFromSteamId( item.TicketId ) );
                if ( identity == null )
                    continue;
                if ( identity.IsDead )
                    continue;
                string playerName = identity.DisplayName;
                item.TimeUsed++;
                int timeLeft = item.TimeAllocated - item.TimeUsed;

                if ( timeLeft <= 0 )
                {
                    Communication.Notification( 0, MyFontEnum.Blue, 10000, $"Goodbye {playerName}, thanks for joining us!" );
                    //Communication.SendPublicInformation( $"Goodbye {playerName}, thanks for joining us!" );
                    if ( !toRemove.Contains( item.TicketId ) )
                        toRemove.Add( item.TicketId );
                    continue;
                }
                if ( timeLeft <= 1 )
                {
                    Communication.Notification( 0, MyFontEnum.Blue, 10000, $"{playerName} has one minute left!" );
                    //Communication.SendPublicInformation( $"{playerName} has one minute left!" );
                    continue;
                }
                if ( timeLeft <= 5 )
                {
                    Communication.Notification( 0, MyFontEnum.Blue, 10000, $"{playerName} has five minutes left!" );
                    //Communication.SendPublicInformation( $"{playerName} has five minutes left!" );
                }
            }

            Thread.Sleep( 10000 );

            foreach ( ulong steamId in toRemove )
            {
                MyMultiplayer.Static.KickClient( steamId );
                foreach ( var item in PluginSettings.Instance.TicketPlayers.Where( item => item.TicketId == steamId ) )
                {
                    PluginSettings.Instance.TicketPlayers.Remove( item );
                    break;
                }
            }
        }
    }
}