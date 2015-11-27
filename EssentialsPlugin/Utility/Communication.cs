namespace EssentialsPlugin.Utility
{
	using System;
	using System.IO;
	using System.Linq;
	using NLog;
	using Sandbox.Common;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
    using System.Collections.Generic;
	using SEModAPIExtensions.API;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity;
	using SEModAPIInternal.API.Entity.Sector.SectorObject;
	using SEModAPIInternal.API.Server;
	using VRage;
	using VRageMath;

	public class Communication
	{
		private static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
		private static Random m_random = new Random( );

		public static void SendPublicInformation( String infoText )
		{
			if ( infoText == "" )
				return;

			ChatManager.Instance.SendPublicChatMessage( infoText );
		}

		public static void SendPrivateInformation( ulong playerId, String infoText )
		{
			if ( infoText == "" )
				return;

			ChatManager.Instance.SendPrivateChatMessage( playerId, infoText );
		}

        public static Vector3D getPos(ulong userId)
        {
            Vector3D position = Vector3D.Zero;
            Wrapper.GameAction(() =>
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, x => x.SteamUserId == userId);

                if (players.Count > 0)
                {
                    IMyPlayer player = players.First();
                    position = player.GetPosition();
                }
            });

            return position;
        }

        public static void SendClientMessage( ulong steamId, string message )
		{
			if ( PlayerMap.Instance.GetPlayerIdsFromSteamId( steamId ).Count < 1 )
			{
				Log.Info( string.Format( "Unable to locate playerId for user with steamId: {0}", steamId ) );
				return;
			}

			CubeGridEntity entity = new CubeGridEntity( new FileInfo( Essentials.PluginPath + "CommRelay.sbc" ) );
            long entityId = BaseEntity.GenerateEntityId( );
			entity.EntityId = entityId;
			entity.DisplayName = string.Format( "CommRelayOutput{0}", PlayerMap.Instance.GetPlayerIdsFromSteamId( steamId ).First( ) );
			entity.PositionAndOrientation = new MyPositionAndOrientation( MathUtility.GenerateRandomEdgeVector( getPos(steamId) ), Vector3.Forward, Vector3.Up );

			foreach ( MyObjectBuilder_CubeBlock block in entity.BaseCubeBlocks )
			{
				MyObjectBuilder_Beacon beacon = block as MyObjectBuilder_Beacon;
				if ( beacon != null )
				{
					beacon.CustomName = message;
				}
			}

            SectorObjectManager.Instance.AddEntity( entity );
			//TimedEntityCleanup.Instance.Add( entityId );
		}

		public static void SendBroadcastMessage( string message )
		{
			CubeGridEntity entity = new CubeGridEntity( new FileInfo( Essentials.PluginPath + "CommRelay.sbc" ) );
			long entityId = BaseEntity.GenerateEntityId( );
			entity.EntityId = entityId;
			entity.DisplayName = string.Format( "CommRelayBroadcast{0}", m_random.Next( 1, 10000 ) );
			entity.PositionAndOrientation = new MyPositionAndOrientation( MathUtility.GenerateRandomEdgeVector(), Vector3.Forward, Vector3.Up );

			foreach ( MyObjectBuilder_CubeBlock block in entity.BaseCubeBlocks )
			{
				MyObjectBuilder_Beacon beacon = block as MyObjectBuilder_Beacon;
				if ( beacon != null )
				{
					beacon.CustomName = message;
				}
			}

			SectorObjectManager.Instance.AddEntity( entity );
			TimedEntityCleanup.Instance.Add( entityId );
		}

		public static void SendFactionClientMessage( ulong playerSteamId, String message )
		{
			foreach ( ulong steamId in PlayerManager.Instance.ConnectedPlayers )
			{
				if ( Player.CheckPlayerSameFaction( playerSteamId, steamId ) )
				{
					SendClientMessage( steamId, message );
				}
			}
		}

		public static void Notification( ulong steamId, MyFontEnum color, int timeInSeconds, string message )
		{
			SendClientMessage( steamId, string.Format( "/notification {0} {1} {2}", color, timeInSeconds, message ) );
		}

		// 

		public static void DisplayDialog( ulong steamId, string header, string subheader, string content, string buttonText = "OK" )
		{
			SendClientMessage( steamId, string.Format( "/dialog \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"", header, subheader, " ", content.Replace( "\r\n", "|" ), buttonText ) );
		}

		public static void SendDataMessage( ulong steamId, long msgId, byte[ ] data )
		{
			string msgIdString = msgId.ToString( );
			byte[ ] newData = new byte[ data.Length + msgIdString.Length + 1 ];
			newData[ 0 ] = (byte)msgIdString.Length;
			for ( int r = 0; r < msgIdString.Length; r++ )
				newData[ r + 1 ] = (byte)msgIdString[ r ];

			Buffer.BlockCopy( data, 0, newData, msgIdString.Length + 1, data.Length );

			ServerNetworkManager.SendDataMessage( 9000, newData, steamId );
		}

		public static void BroadcastDataMessage( long msgId, byte[ ] data )
		{
			string msgIdString = msgId.ToString( );
			byte[ ] newData = new byte[ data.Length + msgIdString.Length + 1 ];
			newData[ 0 ] = (byte)msgIdString.Length;
			for ( int r = 0; r < msgIdString.Length; r++ )
				newData[ r + 1 ] = (byte)msgIdString[ r ];

			Buffer.BlockCopy( data, 0, newData, msgIdString.Length + 1, data.Length );

			MyAPIGateway.Multiplayer.SendMessageToOthers( 9000, newData );
		}

		public class ServerMessageItem
		{
			public string From { get; set; }
			public string Message { get; set; }
		}
	}
}
