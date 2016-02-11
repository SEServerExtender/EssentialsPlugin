namespace EssentialsPlugin.Utility
{
    using System;
    using System.Reflection;
    using Sandbox.Game.Multiplayer;
    using Sandbox.Game.World;
    using SEModAPIExtensions.API;

    public static class ChatHistory
    {
        /// <summary>
        ///     Adds incoming chat event into all players' global chat history.
        /// </summary>
        /// <param name="obj"></param>
        public static void AddChat( ChatManager.ChatEvent obj )
        {
            Assembly assembly = typeof( Sync ).Assembly;
            Type syncCharacter = assembly.GetType( "MySyncCharacter" );
            MethodInfo globalMessageInfo = syncCharacter.GetMethod( "SendNewGlobalMessage",
                                                                    BindingFlags.Public | BindingFlags.Instance );
            globalMessageInfo = globalMessageInfo.MakeGenericMethod( typeof( MyPlayer.PlayerId ), typeof( string ) );
            object classInstance = Activator.CreateInstance( syncCharacter );
            globalMessageInfo.Invoke( classInstance, new object[ ] { new MyPlayer.PlayerId( obj.SourceUserId ), obj.Message } );
        }
    }
}
