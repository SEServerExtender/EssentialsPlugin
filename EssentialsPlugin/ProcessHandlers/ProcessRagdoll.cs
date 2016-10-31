namespace EssentialsPlugin.ProcessHandlers
{
    using System.Linq;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Character;
    using Sandbox.Game.Entities.Character.Components;
    using Utility;
    using VRage.Game.Entity;

    public class ProcessRagdoll : ProcessHandlerBase
    {
        public override int GetUpdateResolution( )
        {
            return 1000;
        }

        public override void Handle( )
        {
            if (!PluginSettings.Instance.DisableRagdoll)
                return;

            MyEntity[] entities = new MyEntity[0];
            Wrapper.GameAction( ( ) => entities = MyEntities.GetEntities( ).ToArray( ) );

            foreach (MyEntity entity in entities)
            {
                MyCharacter character = entity as MyCharacter;

                character?.Components.Remove<MyCharacterRagdollComponent>( );
            }

            base.Handle( );
        }
    }
}
