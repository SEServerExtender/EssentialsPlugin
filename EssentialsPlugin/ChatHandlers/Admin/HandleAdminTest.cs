namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Sandbox.Definitions;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Engine.Voxels;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Blocks;
    using Sandbox.Game.Entities.Character;
    using Sandbox.Game.Entities.Character.Components;
    using Sandbox.Game.Replication;
    using Sandbox.Game.SessionComponents;
    using Sandbox.Game.World;
    using SEModAPIInternal.API.Server;
    using SpaceEngineers.Game.World;
    using Utility;
    using VRage.Game.Entity;
    using VRage.Network;

    public class HandleAdminTest : ChatHandlerBase
	{

    public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin test";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "";
            DialogItem.content = GetHelp( );
            DialogItem.buttonText = "close";
            return DialogItem;
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
            if (words.Length < 1)
                return true;

            switch (words[0].ToLower( ))
            {
                case "reconnect":
                {
                    var inf = typeof(MyCampaignSessionComponent).GetMethod( "Reconnect", BindingFlags.NonPublic | BindingFlags.Static );
                    if (words.Length < 1 && words[1].ToLower( ) == "all")
                        ServerNetworkManager.Instance.RaiseStaticEvent( inf );
                    else
                        ServerNetworkManager.Instance.RaiseStaticEvent( inf, new EndpointId( userId ) );
                    break;
                }
                case "close":
                {
                    var inf = typeof(MyCampaignSessionComponent).GetMethod( "CloseGame", BindingFlags.NonPublic | BindingFlags.Static );
                    if (words.Length < 1 && words[1].ToLower( ) == "all")
                        ServerNetworkManager.Instance.RaiseStaticEvent( inf );
                    else
                        ServerNetworkManager.Instance.RaiseStaticEvent( inf, new EndpointId( userId ) );
                    break;
                }

                case "jump":
                {
                    var inf = typeof(MyCharacter).GetMethod( "Jump", BindingFlags.Public | BindingFlags.Instance );
                    MyEntity[] ents = new MyEntity[0];
                    Wrapper.GameAction( ( ) => ents = MyEntities.GetEntities( ).ToArray( ) );
                    foreach (var entity in ents)
                    {
                        var character = entity as MyCharacter;
                        if (character == null)
                            continue;

                        Wrapper.BeginGameAction( ( ) => ServerNetworkManager.Instance.RaiseEvent( inf, character ), null, null );
                    }
                    break;
                }
                case "character":
                {
                    var inf = typeof(MyCharacter).GetMethod( words[1], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static );
                    MyEntity[] ents = new MyEntity[0];
                    Wrapper.GameAction( ( ) => ents = MyEntities.GetEntities( ).ToArray( ) );
                    foreach (var entity in ents)
                    {
                        var character = entity as MyCharacter;
                        if (character == null)
                            continue;

                        if (words.Length == 2)
                            Wrapper.BeginGameAction( ( ) => ServerNetworkManager.Instance.RaiseEvent( inf, character ), null, null );
                        else
                            Wrapper.BeginGameAction( ( ) => ServerNetworkManager.Instance.RaiseEvent( inf, character, words[2] ), null, null );
                    }
                    break;
                }
                case "report":
                {
                    var inf = typeof(MyProgrammableBlock).GetMethod( "OpenEditorSucess", BindingFlags.NonPublic | BindingFlags.Instance );
                    MyEntity[] ents = new MyEntity[0];
                    Wrapper.GameAction( ( ) => ents = MyEntities.GetEntities( ).ToArray( ) );
                    foreach (var ent in ents)
                    {
                        var grid = ent as MyCubeGrid;
                        if (grid == null)
                            continue;

                        if (!grid.DisplayName.Contains( "Essentials" ))
                            continue;

                        var slim = grid.CubeBlocks.FirstOrDefault( s => s.FatBlock is MyProgrammableBlock );
                        if (slim == null)
                        {
                            Communication.SendPrivateInformation( userId, "fail" );
                            return true;
                        }
                        var pb = slim.FatBlock as MyProgrammableBlock;
                        string prog = $"Admin report ticket number: {pb.EntityId} \r\n" +
                                      "This report will be logged sent to the administrator. \r\n" +
                                      "The past 30 minutes of server activity will automatically be appended to the end of your report.\r\n\r\n" +
                                      "When you are done, click 'Remember and exit'. Do not click 'Check Code'\r\n\r\n";
                        pb.UpdateProgram( prog );

                        Wrapper.BeginGameAction( ( ) =>
                                                 {
                                                     try
                                                     {
                                                         typeof(MyProgrammableBlock).GetMethod( "SendUpdateProgramRequest", BindingFlags.NonPublic | BindingFlags.Instance ).Invoke( pb, new[] {prog} );
                                                         ServerNetworkManager.Instance.RaiseEvent( inf, pb, new EndpointId( userId ) );
                                                     }
                                                     catch (Exception ex)
                                                     {
                                                         Essentials.Log.Error( ex );
                                                     }
                                                 }, null, null );
                    }
                    break;
                }
                case "ragdoll":
                {
                    MyEntity[] ents = new MyEntity[0];
                    Wrapper.GameAction( ( ) => ents = MyEntities.GetEntities( ).ToArray( ) );
                    foreach (var ent in ents)
                    {
                        var character = ent as MyCharacter;

                        character?.Components.Remove<MyCharacterRagdollComponent>( );
                    }
                    break;
                }
                case "respawn":
                {
                    var inf = typeof(MySpaceRespawnComponent).GetMethod( "ShowMedicalScreen_Implementation", BindingFlags.NonPublic | BindingFlags.Static );
                    Wrapper.GameAction( ( ) => ServerNetworkManager.Instance.RaiseStaticEvent( inf, userId ) );
                    break;
                }
                case "update":
                {
                    bool set = words.Length > 1 && words[1].ToLower( ).Equals( "on" );
                    MyEntity[] ents = new MyEntity[0];
                    Wrapper.GameAction( ( ) => ents = MyEntities.GetEntities( ).ToArray( ) );
                    foreach (var ent in ents)
                    {
                        var character = ent as MyCharacter;
                        if (character == null)
                            continue;

                        if (set)
                            MyEntities.RegisterForUpdate( ent );
                        else
                            MyEntities.UnregisterForUpdate( ent );
                    }
                    break;
                }
                case "seated":
                {
                    bool set = words.Length > 1 && words[1].ToLower( ).Equals( "on" );
                    MyEntity[] ents = new MyEntity[0];
                    Wrapper.GameAction( ( ) => ents = MyEntities.GetEntities( ).ToArray( ) );

                    foreach (var ent in ents)
                    {
                        var grid = ent as MyCubeGrid;
                        if (grid == null)
                            continue;

                        var blocks = grid.GetBlocks( );

                        foreach (var block in blocks)
                        {
                            var cockpit = block?.FatBlock as MyCockpit;
                            if (cockpit?.Pilot == null)
                                continue;

                            Essentials.Log.Info( cockpit.Pilot.DisplayName );

                            if (set)
                                MyEntities.RegisterForUpdate( cockpit.Pilot );
                            else
                                MyEntities.UnregisterForUpdate( cockpit.Pilot );
                        }
                    }
                    break;
                }
                case "component":
                {
                    MyEntity[] ents = new MyEntity[0];
                    Wrapper.GameAction( ( ) => ents = MyEntities.GetEntities( ).ToArray( ) );
                    foreach (var ent in ents)
                    {
                        var character = ent as MyCharacter;

                        character?.Components.Clear( );
                    }
                    break;
                }
            }

            return true;
        }

	}

}

