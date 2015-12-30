namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System.Linq;
    using Sandbox.ModAPI;
    using VRageMath;
    using Utility;
    using System.Collections.Generic;
    using Sandbox.Common.ObjectBuilders;
    using SEModAPI.API.Definitions;
    using System.IO;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Threading;
    using System.Windows.Forms;
    using NLog;
    using NLog.Targets;
    using Sandbox;
    using Sandbox.Common.ObjectBuilders;
    using SEModAPI.API;
    using SEModAPI.API.Definitions;
    using SEModAPI.API.Sandbox;
    using SEModAPI.API.Utility;
    using SEModAPIInternal.API.Chat;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Server;
    using SEModAPIInternal.Support;
    using Timer = System.Timers.Timer;
    using SEModAPIExtensions.API;
    using SEModAPIInternal.API.Entity.Sector.SectorObject;
    using SEModAPIInternal.API.Entity;
    using VRage;
    using EssentialsPlugin.Utility;
    using VRage.ModAPI;
    using Sandbox.Game.Replication;
    using Sandbox.Engine.Multiplayer;
    public class HandleAdminTest : ChatHandlerBase
	{

        private DedicatedConfigDefinition _dedicatedConfigDefinition;

    public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin test";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"Admin Test\" \"\"" +
                "\"For development testing.\" \"close\" ";
            return longMessage;
        }

        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

        public override bool HandleCommand( ulong userId, string[ ] words )
        {
            Communication.ServerMessageItem message = new Communication.ServerMessageItem( );
            message.From = "Server";
            message.Message = "Hello world!";

            string messageString = MyAPIGateway.Utilities.SerializeToXML<Communication.ServerMessageItem>( message );
            byte[ ] data = new byte[messageString.Length];

            for ( int r = 0; r < messageString.Length; r++ )
            {
                data[r] = (byte)messageString[r];
            }

            Communication.SendDataMessage( userId, 5003, data );

            try
            {
                Essentials.Log.Info( "test" );
                IMyEntity entity = MyAPIGateway.Entities.GetEntityById( PlayerMap.Instance.GetPlayerEntityId( userId ) );
                Vector3D position = new Vector3D( 100000,100000,100000 );
                Wrapper.GameAction( ( ) =>
                {
                    entity.SetPosition( position );
                    MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( entity ) );
                } );
            }
            catch ( Exception ex )
            {
                Essentials.Log.Info( ex, "test fail" );
            }
           

            return true;
        }

	}

}

