namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using EssentialsPlugin.Settings;
	using EssentialsPlugin.Utility;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using VRage.Game.ModAPI;

    class GreetingItem
	{
		private DateTime _start;
		public DateTime Start
		{
			get { return _start; }
			set { _start = value; }
		}

		private ulong _steamId;
		public ulong SteamId
		{
			get { return _steamId; }
			set { _steamId = value; }
		}

		private TimeSpan _timeout;
		public TimeSpan Timeout
		{
			get { return _timeout; }
			set { _timeout = value; }
		}

		private bool _isNewUser;
		public bool IsNewUser
		{
			get { return _isNewUser; }
			set { _isNewUser = value; }
		}
	}

	public class ProcessGreeting : ProcessHandlerBase
	{
		private List<GreetingItem> m_greetingList = new List<GreetingItem>();
		private DateTime m_start = DateTime.Now;

		public override int GetUpdateResolution()
		{
			return 1001;
		}

	    public override void Handle()
	    {
	        if ( !PluginSettings.Instance.GreetingEnabled )
	            return;

	        if ( MyAPIGateway.Players == null )
	            return;

	        try
	        {
	            List<IMyPlayer> players = new List<IMyPlayer>();
	            bool result = false;

	            //Wrapper.GameAction(() =>
	            //{
	            try
	            {
	                MyAPIGateway.Players.GetPlayers( players );
	                result = true;
	            }
	            catch ( Exception ex )
	            {
	                Essentials.Log.Error( ex );
	            }
	            //});

	            if ( !result )
	                return;

	            lock ( m_greetingList )
	            {
	                for ( int r = m_greetingList.Count - 1; r >= 0; r-- )
	                {
	                    GreetingItem item = m_greetingList[r];
	                    if ( DateTime.Now - item.Start > item.Timeout )
	                    {
	                        m_greetingList.RemoveAt( r );
	                        continue;
	                    }
	                    IMyPlayer player = players.FirstOrDefault( x => x.SteamUserId == item.SteamId && x.Controller != null && x.Controller.ControlledEntity != null );
	                    if ( player != null )
	                    {
	                        m_greetingList.RemoveAt( r );

	                        if ( PluginSettings.Instance.GreetingMessage != "" )
	                        {
	                            string message;

	                            if ( item.IsNewUser )
	                                message = PluginSettings.Instance.GreetingNewUserMessage.Replace( "%name%", player.DisplayName );
	                            else
	                                message = PluginSettings.Instance.GreetingMessage.Replace( "%name%", player.DisplayName );

	                            message = message.Replace( "%name%", player.DisplayName );
	                            message = message.Replace( "%players%", players.Count.ToString() );
	                            message = message.Replace( "%maxplayers%", MyAPIGateway.Session.SessionSettings.MaxPlayers.ToString() );

	                            string finalMessage = message;

	                            if ( PluginSettings.Instance.GreetingPublic )
	                                Communication.SendPublicInformation( finalMessage );
	                            else
	                                Communication.SendPrivateInformation( item.SteamId, finalMessage );
	                        }

	                        if ( item.IsNewUser )
	                        {
	                            if ( PluginSettings.Instance.GreetingNewUserItem.Enabled )
	                            {
	                                SettingsGreetingDialogItem gItem = PluginSettings.Instance.GreetingNewUserItem;
	                                Communication.DisplayDialog( item.SteamId, gItem.Title.Replace( "%name%", player.DisplayName ), gItem.Header.Replace( "%name%", player.DisplayName ), gItem.Contents.Replace( "%name%", player.DisplayName ).Replace( "\r", "" ).Replace( "\n", "|" ).Replace( "\"", "'" ), gItem.ButtonText );
	                            }
	                        }
	                        else
	                        {
	                            if ( PluginSettings.Instance.GreetingItem.Enabled )
	                            {
	                                SettingsGreetingDialogItem gItem = PluginSettings.Instance.GreetingItem;
	                                Communication.DisplayDialog( item.SteamId, gItem.Title.Replace( "%name%", player.DisplayName ), gItem.Header.Replace( "%name%", player.DisplayName ), gItem.Contents.Replace( "%name%", player.DisplayName ).Replace( "\r", "" ).Replace( "\n", "|" ).Replace( "\"", "'" ), gItem.ButtonText );
	                            }
	                        }

	                    }
	                }
	            }
	        }
	        catch ( Exception ex )
	        {
	            Essentials.Log.Error( ex );
	        }

	        base.Handle();
	    }

	    public override void OnPlayerJoined(ulong remoteUserId)
		{
			GreetingItem item = new GreetingItem();
			item.SteamId = remoteUserId;
			item.Timeout = TimeSpan.FromMinutes(10);
			item.Start = DateTime.Now;
			item.IsNewUser = !PlayerMap.Instance.GetPlayerIdsFromSteamId(remoteUserId).Any();

			Essentials.Log.Info( "New User: {0}", remoteUserId );

			lock (m_greetingList)
			{
				m_greetingList.Add(item);
				Essentials.Log.Info( "Greeting Added => {0} (New user: {1})", remoteUserId, item.IsNewUser );
			}
            
			base.OnPlayerJoined(remoteUserId);
		}
        
		public override void OnPlayerLeft(ulong remoteUserId)
		{
			lock (m_greetingList)
			{
				if (m_greetingList.Find(x => x.SteamId == remoteUserId) != null)
				{
					Essentials.Log.Info( "Greeting Removed => {0}", remoteUserId );
					m_greetingList.RemoveAll(x => x.SteamId == remoteUserId);
				}
			}

			base.OnPlayerLeft(remoteUserId);
		}
	}
}

