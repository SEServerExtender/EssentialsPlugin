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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Sandbox;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Settings;
    using VRage.Collections;
    using VRage.Game;
    using VRage.ModAPI;

    public static class Communication
    {
        private static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
        private static Random _random = new Random( );

        public static void SendPublicInformation( string infoText )
        {
            if ( infoText == "" )
                return;
           
            ServerMessageItem MessageItem = new ServerMessageItem( );
            MessageItem.From = PluginSettings.Instance.ServerChatName;
            MessageItem.Message = infoText;

            string messageString = MyAPIGateway.Utilities.SerializeToXML( MessageItem );
            byte[ ] data = Encoding.UTF8.GetBytes( messageString );

            if ( ChatManager.EnableData )
            {
                BroadcastDataMessage( DataMessageType.Message, data );
            }
            else
                ChatManager.Instance.SendPublicChatMessage( infoText );

            ChatManager.Instance.AddChatHistory( new ChatManager.ChatEvent( DateTime.Now, 0, infoText ) );
        }

        public static void SendPrivateInformation( ulong playerId, string infoText, string from = null )
        {
            if ( infoText == "" )
                return;

            ServerMessageItem MessageItem = new ServerMessageItem( );

            if ( from == null )
                MessageItem.From = PluginSettings.Instance.ServerChatName;

            else if ( PluginSettings.Instance.WhisperChatPrefix )
                MessageItem.From = "<whisper> " + from;

            else
                MessageItem.From = from;

            MessageItem.Message = infoText;

            string messageString = MyAPIGateway.Utilities.SerializeToXML( MessageItem );
            byte[ ] data = Encoding.UTF8.GetBytes( messageString );

            if ( ChatManager.EnableData )
            {
                SendDataMessage( playerId, DataMessageType.Message, data );
            }
            else
                ChatManager.Instance.SendPrivateChatMessage( playerId, infoText );

            ChatManager.ChatEvent chatItem = new ChatManager.ChatEvent( );
            chatItem.Timestamp = DateTime.Now;
            chatItem.RemoteUserId = (from == null ? 0 : PlayerMap.Instance.GetSteamIdFromPlayerName( from ));
            chatItem.Message = (from == null ? infoText : ( $"{{whisper}} to {PlayerMap.Instance.GetFastPlayerNameFromSteamId( playerId )}: {infoText}" ));
            ChatManager.Instance.AddChatHistory( chatItem );
        }
        public static void SendAllianceClientMessage(ulong playerSteamId, string message)
        {
           
            ServerMessageItem MessageItem = new ServerMessageItem();
            if (PluginSettings.Instance.FactionChatPrefix)
                MessageItem.From = "<Alliance> " + PlayerMap.Instance.GetFastPlayerNameFromSteamId(playerSteamId);
            else
                MessageItem.From = PlayerMap.Instance.GetFastPlayerNameFromSteamId(playerSteamId);

            MessageItem.Message = message;

            string messageString = MyAPIGateway.Utilities.SerializeToXML(MessageItem);
            byte[] data = Encoding.UTF8.GetBytes(messageString);

            foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
            {
                if (!(playerSteamId == steamId))//fix
                {
                    if (Player.CheckPlayerSameAlliance(playerSteamId, steamId))
                    {
                        if (ChatManager.EnableData)
                        {
                            SendDataMessage(steamId, DataMessageType.Message, data);
                            ChatManager.Instance.AddChatHistory(new ChatManager.ChatEvent(DateTime.Now, playerSteamId, "{Alliance message}: " + message));
                        }
                        else
                            ChatManager.Instance.SendPrivateChatMessage(steamId, message);
                    }
                }
            }
        }
        public static void SendFactionClientMessage(ulong playerSteamId, string message)
        {
            ServerMessageItem MessageItem = new ServerMessageItem();
            if (PluginSettings.Instance.FactionChatPrefix)
                MessageItem.From = "<faction> " + PlayerMap.Instance.GetFastPlayerNameFromSteamId(playerSteamId);
            else
                MessageItem.From = PlayerMap.Instance.GetFastPlayerNameFromSteamId(playerSteamId);

            MessageItem.Message = message;

            string messageString = MyAPIGateway.Utilities.SerializeToXML(MessageItem);
            byte[] data = Encoding.UTF8.GetBytes(messageString);

            foreach (ulong steamId in PlayerManager.Instance.ConnectedPlayers)
            {
                if (!(playerSteamId == steamId))//fix
                {
                    if (Player.CheckPlayerSameFaction(playerSteamId, steamId))
                    {
                        if (ChatManager.EnableData)
                        {
                            SendDataMessage(steamId, DataMessageType.Message, data);
                            ChatManager.Instance.AddChatHistory(new ChatManager.ChatEvent(DateTime.Now, playerSteamId, "{faction message}: " + message));
                        }
                        else
                            ChatManager.Instance.SendPrivateChatMessage(steamId, message);
                    }
                }
            }
        }

        public static void Notification( ulong steamId, MyFontEnum color, int timeInSeconds, string message )
        {
            ServerNotificationItem messageItem = new ServerNotificationItem
                                                 {
                                                     color = color,
                                                     time = timeInSeconds,
                                                     message = message
                                                 };

            string messageString = MyAPIGateway.Utilities.SerializeToXML( messageItem );
            byte[ ] data = Encoding.UTF8.GetBytes( messageString );

            if ( steamId != 0 )
                SendDataMessage( steamId, DataMessageType.Notification, data );
            else
                BroadcastDataMessage( DataMessageType.Notification, data );
        }

        public static void DisplayDialog( ulong steamId, string header, string subheader, string content, string buttonText = "OK" )
        {
            ServerDialogItem messageItem = new ServerDialogItem
                                           {
                                               title = header,
                                               header = subheader,
                                               content = content,
                                               buttonText = buttonText
                                           };

            string messageString = MyAPIGateway.Utilities.SerializeToXML( messageItem );
            byte[ ] data = Encoding.UTF8.GetBytes( messageString );

            SendDataMessage( steamId, DataMessageType.Dialog, data );
        }

        public static void DisplayDialog( ulong steamId, ServerDialogItem messageItem )
        {
            string messageString = MyAPIGateway.Utilities.SerializeToXML( messageItem );
            byte[ ] data = Encoding.UTF8.GetBytes( messageString );

            SendDataMessage( steamId, DataMessageType.Dialog, data );
        }

        public static void MoveMessage( ulong steamId, string moveType, double x, double y, double z, long entityId = 0 )
        {
            ServerMoveItem MoveItem = new ServerMoveItem( );
            MoveItem.moveType = moveType;
            MoveItem.x = x;
            MoveItem.y = y;
            MoveItem.z = z;
            MoveItem.entityId = entityId;

            string messageString = MyAPIGateway.Utilities.SerializeToXML( MoveItem );
            byte[ ] data = Encoding.UTF8.GetBytes( messageString );
            if ( steamId != 0 )
                SendDataMessage( steamId, DataMessageType.Move, data );
            else
                BroadcastDataMessage( DataMessageType.Move, data );
        }

        public static void MoveMessage( ulong steamId, string moveType, Vector3D position )
        {
            ServerMoveItem MoveItem = new ServerMoveItem( );
            MoveItem.moveType = moveType;
            MoveItem.x = position.X;
            MoveItem.y = position.Y;
            MoveItem.z = position.Z;

            string messageString = MyAPIGateway.Utilities.SerializeToXML( MoveItem );
            byte[ ] data = Encoding.UTF8.GetBytes( messageString );

            SendDataMessage( steamId, DataMessageType.Move, data );
        }

        public static void WaypointMessage( WaypointItem item )
        {
            string messageString = MyAPIGateway.Utilities.SerializeToXML( item );
            byte[ ] data = Encoding.UTF8.GetBytes( messageString );
            SendDataMessage( item.SteamId, DataMessageType.Waypoint, data );
        }

        public static void WaypointMessage( ulong userId, string waypointString )
        {
            byte[ ] data = Encoding.UTF8.GetBytes( waypointString );
            SendDataMessage( userId, DataMessageType.Waypoint, data );
        }

        public static void WaypointMessage( ServerWaypointItem serverItem )
        {
            WaypointItem item = new WaypointItem( );
            item.Name = serverItem.Name;
            item.Position = new Vector3D( serverItem.X, serverItem.Y, serverItem.Z );
            //item.Remove = serverItem.Enabled;
            item.SteamId = 0;
            item.Text = serverItem.Name;

            string messageString = MyAPIGateway.Utilities.SerializeToXML( item );
            byte[ ] data = Encoding.UTF8.GetBytes( messageString );
            BroadcastDataMessage( DataMessageType.Waypoint, data );
        }
        
        public static void DisableGMenu( ulong steamId, string blockDef, bool subType, bool enable )
        {
                var message = Encoding.UTF8.GetBytes( blockDef );
                byte[] data = new byte[sizeof (bool) * 2 + message.Length];
                BitConverter.GetBytes( enable ).CopyTo( data, 0 );
                BitConverter.GetBytes( subType ).CopyTo( data, sizeof (bool) );
                message.CopyTo( data, sizeof (bool) * 2 );

                SendDataMessage( steamId, DataMessageType.GMenu, data );
                return;
            /*
            DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> allDefs = MyDefinitionManager.Static.GetAllDefinitions();
            HashSet<MyCubeBlockDefinition> blockDefs = new HashSet<MyCubeBlockDefinition>();

            if ( !subType )
            {
                foreach ( MyDefinitionBase definition in allDefs.Where( x => x is MyCubeBlockDefinition ) )
                {
                    if ( definition.Id.TypeId.ToString().Contains( blockDef ) )
                    {
                        blockDefs.Add( definition as MyCubeBlockDefinition );
                    }
                }
            }
            else
            {
                var message = Encoding.UTF8.GetBytes( blockDef );
                byte[] data = new byte[sizeof (bool) * 2 + message.Length];
                BitConverter.GetBytes( enable ).CopyTo( data, 0 );
                BitConverter.GetBytes( true ).CopyTo( data, sizeof (bool) );
                message.CopyTo( data, sizeof (bool) * 2 );

                SendDataMessage( steamId, DataMessageType.GMenu, data );
                return;
            }

            foreach ( var def in blockDefs )
            {
                var message = Encoding.UTF8.GetBytes( def.Id.SubtypeId.ToString() );
                byte[] data = new byte[sizeof (bool) * 2 + message.Length];
                BitConverter.GetBytes( enable ).CopyTo( data, 0 );
                BitConverter.GetBytes( true ).CopyTo( data, sizeof (bool) );
                message.CopyTo( data, sizeof (bool) * 2 );

                SendDataMessage( steamId, DataMessageType.GMenu, data );
                Thread.Sleep( 60000 );
            }
            */
        }

        public static void HandleAddConcealExempt( byte[] data )
        {
            //this is raised on the game thread which locks up the server for some reason, so run the action in a new thread
            Task.Run( () =>
                      {
                          var subtype = Encoding.UTF8.GetString( data );

                          if ( !PluginSettings.Instance.DynamicConcealIgnoreSubTypeList.Contains( subtype ) )
                          {
                              string[] newArray = new string[PluginSettings.Instance.DynamicConcealIgnoreSubTypeList.Length + 1];
                              PluginSettings.Instance.DynamicConcealIgnoreSubTypeList.CopyTo( newArray, 0 );
                              newArray[newArray.Length - 1] = subtype;
                              PluginSettings.Instance.DynamicConcealIgnoreSubTypeList = newArray;
                          }
                      } );
        }
        
        public static void SendDataMessage( ulong steamId, DataMessageType messageType, byte[ ] data )
        {
            /*
            long msgId = (long)messageType;

            string msgIdString = msgId.ToString( );
            byte[ ] newData = new byte[data.Length + msgIdString.Length + 1];
            newData[0] = (byte)msgIdString.Length;
            for ( int r = 0; r < msgIdString.Length; r++ )
                newData[r + 1] = (byte)msgIdString[r];

            Buffer.BlockCopy( data, 0, newData, msgIdString.Length + 1, data.Length );
            */

            //hash a random long with the current time to make a decent quality guid for each message
            byte[] randLong = new byte[sizeof(long)];
            _random.NextBytes(randLong);
            long uniqueId = 23;
            uniqueId = uniqueId * 37 + BitConverter.ToInt64(randLong, 0);
            uniqueId = uniqueId * 37 + DateTime.Now.GetHashCode();

            //this is a much more elegant and lightweight method
            byte[] newData = new byte[sizeof(long)*2 + data.Length];
            BitConverter.GetBytes( uniqueId ).CopyTo( newData, 0 );
            BitConverter.GetBytes((long)messageType).CopyTo(newData, sizeof(long));
            data.CopyTo( newData, sizeof(long)*2);

            if ( newData.Length > 4096 )
            {
                SendMessagePartsTo( steamId, newData );
                return;
            }

            //Wrapper.GameAction( ( ) =>
            MySandboxGame.Static.Invoke( () =>
                                         {
                                             MyAPIGateway.Multiplayer.SendMessageTo( 9000, newData, steamId );
                                         } );
        }

        public static void BroadcastDataMessage( DataMessageType messageType, byte[ ] data )
        {
            /*
            long msgId = (long)messageType;

            string msgIdString = msgId.ToString( );
            byte[ ] newData = new byte[data.Length + msgIdString.Length + 1];
            newData[0] = (byte)msgIdString.Length;
            for ( int r = 0; r < msgIdString.Length; r++ )
                newData[r + 1] = (byte)msgIdString[r];

            Buffer.BlockCopy( data, 0, newData, msgIdString.Length + 1, data.Length );
            */
            byte[] randLong = new byte[sizeof(long)];
            _random.NextBytes(randLong);
            long uniqueId = 23;
            uniqueId = uniqueId * 37 + BitConverter.ToInt64( randLong, 0 );
            uniqueId = uniqueId * 37 + DateTime.Now.GetHashCode();

            byte[] newData = new byte[sizeof(long) * 2 + data.Length];
            BitConverter.GetBytes(uniqueId).CopyTo(newData, 0);
            BitConverter.GetBytes((long)messageType).CopyTo(newData, sizeof(long));
            data.CopyTo(newData, sizeof(long) * 2);

            if (newData.Length > 4096)
            {
                BroadcastMessageParts(newData);
                return;
            }

            //Wrapper.GameAction( ( ) =>
            MySandboxGame.Static.Invoke(() =>
                               {
                                    MyAPIGateway.Multiplayer.SendMessageToOthers( 9000, newData );
                                } );
        }

        public static void SendMessagePartsTo(ulong steamId, byte[] data)
        {
            foreach(var packet in Segment( data ))
                MyAPIGateway.Multiplayer.SendMessageTo(9006, packet, steamId);
        }

        public static void BroadcastMessageParts( byte[] data)
        {
            foreach (var packet in Segment(data))
                MyAPIGateway.Multiplayer.SendMessageToOthers(9006, packet);
        }

        public static void ReceiveMessageParts(byte[] data)
        {
            //this is raised on the game thread which can lock up the server, so run the action in a new thread
            Task.Run( ( ) =>
                      {
                          byte[] message = Desegment( data );

                          if(message == null)
                              return;

                          ulong steamId = BitConverter.ToUInt64( message, 0 );
                          string chatMessage = Encoding.UTF8.GetString( message, sizeof (ulong), message.Length - sizeof (ulong) );
                          Essentials.Instance.HandleChatMessage( steamId, chatMessage );
                      } );
        }

    private static Dictionary<int, PartialMessage> messages = new Dictionary<int, PartialMessage>();
    private const int PACKET_SIZE = 4096;
    private const int META_SIZE = sizeof(int) * 2;
    private const int DATA_LENGTH = PACKET_SIZE - META_SIZE;
 
    /// <summary>
    /// Segments a byte array.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static List<byte[]> Segment(byte[] message)
    {
        var hash = BitConverter.GetBytes(message.GetHashCode());
        var packets = new List<byte[]>();
        int msgIndex = 0;
 
        int packetId = message.Length / DATA_LENGTH;
 
        while (packetId >= 0)
        {
            var id = BitConverter.GetBytes(packetId);
            byte[] segment;
 
            if (message.Length - msgIndex > DATA_LENGTH)
            {
                segment = new byte[PACKET_SIZE];
            }
            else
            {
                segment = new byte[META_SIZE + message.Length - msgIndex];
            }
 
            //Copy packet "header" data.
            Array.Copy(hash, segment, hash.Length);
            Array.Copy(id, 0, segment, hash.Length, id.Length);
 
            //Copy segment of original message.
            Array.Copy(message, msgIndex, segment, META_SIZE, segment.Length - META_SIZE);
 
            packets.Add(segment);
            msgIndex += DATA_LENGTH;
            packetId--;
        }
 
        return packets;
    }
 
    /// <summary>
    /// Reassembles a segmented byte array.
    /// </summary>
    /// <param name="packet">Array segment.</param>
    /// <param name="message">Full array, null if incomplete.</param>
    /// <returns>Message fully desegmented, "message" is assigned.</returns>
    public static byte[] Desegment(byte[] packet)
    {
        int hash = BitConverter.ToInt32(packet, 0);
        int packetId = BitConverter.ToInt32(packet, sizeof(int));
        byte[] dataBytes = new byte[packet.Length - META_SIZE];
        Array.Copy(packet, META_SIZE, dataBytes, 0, packet.Length - META_SIZE);
 
        if (!messages.ContainsKey(hash))
        {
            if (packetId == 0)
            {
                return dataBytes;
            }
            else
            {
                messages.Add(hash, new PartialMessage(packetId));
            }
        }
 
        var message = messages[hash];
        message.WritePart(packetId, dataBytes);
 
        if (message.IsComplete)
        {
            messages.Remove(hash);
            return message.Data;
        }
 
        return null;
    }
 
    private class PartialMessage
    {
        public byte[] Data;
        private HashSet<int> receivedPackets = new HashSet<int>();
        private readonly int MaxId;
        public bool IsComplete { get { return receivedPackets.Count == MaxId + 1; } }
 
 
        public PartialMessage(int startId)
        {
            MaxId = startId;
            Data = new byte[0];
        }
 
        public void WritePart(int id, byte[] data)
        {
            int index = MaxId - id;
            int requiredLength = (index * DATA_LENGTH) + data.Length;
 
            if (Data.Length < requiredLength)
            {
                Array.Resize(ref Data, requiredLength);
            }
 
            Array.Copy(data, 0, Data, index * DATA_LENGTH, data.Length);
            receivedPackets.Add(id);
        }
    }
        public class ServerMessageItem
        {
            public string From
            {
                get; set;
            }
            public string Message
            {
                get; set;
            }
        }

        public class ServerDialogItem
        {
            public string title
            {
                get; set;
            }
            public string header
            {
                get; set;
            }
            public string content
            {
                get; set;
            }
            public string buttonText
            {
                get; set;
            }
        }

        public class ServerNotificationItem
        {
            public MyFontEnum color
            {
                get; set;
            }
            public int time
            {
                get; set;
            }
            public string message
            {
                get; set;
            }
        }

        public class ServerMoveItem
        {
            public string moveType
            {
                get; set;
            }
            public double x
            {
                get; set;
            }
            public double y
            {
                get; set;
            }
            public double z
            {
                get; set;
            }
            public long entityId
            {
                get; set;
            }
        }
        
        public enum DataMessageType : long
        {
            Test = 5000,
            VoxelHeader,
            VoxelPart,
            Message,
            RemoveStubs,
            ChangeServer,
            ServerSpeed,
            Credits,

            //skipped a few addresses to avoid conflict
            //just in case
            Dialog = 5020,
            Move,
            Notification,
            MaxSpeed,
            ServerInfo,
            Waypoint,
            GMenu
        }
    }
}
