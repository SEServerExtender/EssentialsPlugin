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
    using SEModAPI.API.Definitions;

    public class Communication
	{
		private static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
		private static Random m_random = new Random( );
        private static int modStatus = 0;
        
        //workshop mod IDs
        private static string pubEssentials = "559202083";
        private static string testEssentials = "558596580";
        private static string midspaceMod = "316190120";


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

            ulong playerId = (ulong)PlayerMap.Instance.GetPlayerIdsFromSteamId( steamId )[0];

            //let's make sure we have the right mods installed for the client to use CommRelays.
            if ( PlayerManager.Instance.IsUserAdmin( steamId ) )
            {
                switch ( CheckMods( ) )
                {
                    case 0:
                        //couldn't read modslist. should we do something?
                        break;
                    case 1:
                        SendPrivateInformation( steamId, "Midspace's Admin Helper Commands mod is incompatible with Essentials. " +
                            "MOTD and some other features are disabled." );
                        return;
                    case 2:
                        SendPrivateInformation( steamId, "No Essentials mod detected. " +
                            "MOTD and some other features are disabled." );
                        return;
                    case 3:
                        //all good
                        break;
                }
            }
            else
            {
                if ( CheckMods( ) == 1 || CheckMods( ) == 2 )
                    return;
                //if mod conditions aren't met and user isn't admin, fail quietly
            }


            CubeGridEntity entity = new CubeGridEntity( new FileInfo( Essentials.PluginPath + "CommRelay.sbc" ) );
            long entityId = BaseEntity.GenerateEntityId( );
			entity.EntityId = entityId;
			entity.DisplayName = string.Format( "CommRelayOutput{0}", PlayerMap.Instance.GetPlayerIdsFromSteamId( steamId ).First( ) );
            //entity.PositionAndOrientation = new MyPositionAndOrientation(MathUtility.GenerateRandomEdgeVector(getPos(steamId)), Vector3.Forward, Vector3.Up);
            entity.PositionAndOrientation = new MyPositionAndOrientation( MathUtility.GenerateRandomEdgeVector( ), Vector3.Forward, Vector3.Up );

            foreach ( MyObjectBuilder_CubeBlock block in entity.BaseCubeBlocks )
			{
				MyObjectBuilder_Beacon beacon = block as MyObjectBuilder_Beacon;
				if ( beacon != null )
				{
					beacon.CustomName = message;
				}
			}


            //IMyReplicable reprelay = (IMyReplicable)entity.
            //MyReplicationServer repServ = (MyReplicationServer)MyMultiplayer.Static.ReplicationLayer;
            //repServ.ForceEverything(new EndpointId(steamId));

            //Sandbox.ModAPI.Ingame.IMyCubeGrid
            
            SectorObjectManager.Instance.AddEntity(entity);
            TimedEntityCleanup.Instance.Add( entityId );
		}

		public static void SendBroadcastMessage( string message )
		{
            //let's make sure we have the right mods installed for the client to use CommRelays.
            if ( CheckMods( ) == 1 || CheckMods( ) == 2 )
                return;

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


        //<summary>
        //Midspace's Admin Helper Commands mod can totally break the Essentials client mod
        //so we need to see if we have it installed, and disable some Essentials features.
        //While we're here, might as well check for missing Essentials mods.
        //this is a temporary measure (I hope)
        //0 = uninitialized
        //1 = incompatible mod (only Midspace's at the moment)
        //2 = missing Essentials mod
        //3 = mod requirements okay
        //</summary>
        public static int CheckMods( )
        {
            if ( modStatus == 0 )
            {
                if ( File.Exists( Path.Combine( Server.Instance.Path, "SpaceEngineers-Dedicated.cfg" ) ) )
                {
                    DedicatedConfigDefinition m_configList;
                    MyConfigDedicatedData<MyObjectBuilder_SessionSettings> config = DedicatedConfigDefinition.Load( new FileInfo( Path.Combine( Server.Instance.Path, "SpaceEngineers-Dedicated.cfg" ) ) );
                    m_configList = new DedicatedConfigDefinition( config );

                    if ( m_configList.Mods.Contains( midspaceMod ) )
                    {
                        modStatus = 1;
                        return modStatus;
                    }
                    else if ( !(m_configList.Mods.Contains( pubEssentials ) || m_configList.Mods.Contains( testEssentials )) )
                    {
                        modStatus = 2;
                        return modStatus;
                    }
                    else
                    {
                        modStatus = 3;
                        return modStatus;
                    }
                }
            }
            return modStatus;
        }

        public class ServerMessageItem
		{
			public string From { get; set; }
			public string Message { get; set; }
		}
	}
}
