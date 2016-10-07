namespace EssentialsPlugin.Utility
{
    using System;
    using System.Linq;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Character;
    using Sandbox.Game.World;
    using SEModAPIExtensions.API;

    public static class ChatHistory
    {
        private static bool _init;
        /// <summary>
        ///     Adds incoming chat event into all players' global chat history.
        /// </summary>
        /// <param name="obj"></param>
        public static void AddChat( ChatManager.ChatEvent obj )
        {
            if ( !_init )
            {
                _init = true;
                Init();
            }
            Wrapper.GameAction( () =>
                                {
                                    try
                                    {
                                        var player = MySession.Static.Players.GetAllPlayers().First( x => x.SteamId == obj.SourceUserId );
                                        var characterId = MySession.Static.Players.ControlledEntities.First( x => x.Value.SteamId == obj.SourceUserId ).Key;
                                        var character = MyEntities.GetEntityById( characterId ) as MyCharacter;
                                        if ( character == null )
                                        {
                                            //okay, the sending player doesn't have a character. just find any character
                                            character = MyEntities.GetEntities().FirstOrDefault( x => (x as MyCharacter) != null ) as MyCharacter;

                                            if ( character == null )
                                            {
                                                //we gave it our best shot :(
                                                return;
                                            }
                                        }

                                        character.SendNewGlobalMessage( player, obj.Message );
                                    }
                                    catch ( Exception ex )
                                    {
                                        Essentials.Log.Error( ex, "Fail ChatHistory" );
                                    }
                                } );
        }

        private static void Init()
        {
            MySession.Static.ChatSystem.GlobalMessageReceived += ChatSystem_GlobalMessageReceived;
        }

        private static void ChatSystem_GlobalMessageReceived()
        {
            //do nothing
            //the very fact that there is a handler hooked to this event suppresses the HUD notification for new messages
            //I have no idea why, but SE doesn't use most of the chat system, so w/e
        }
    }
}
