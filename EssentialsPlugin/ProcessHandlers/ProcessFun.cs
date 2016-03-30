namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Sandbox.Game.Entities;
	using Sandbox.Game.Entities.Character;
	using Sandbox.Game.World;
	using Sandbox.ModAPI;
	using Settings;
	using Utility;
	using VRage.Game.Entity;
	using VRage.Game.ModAPI;
	using VRage.ModAPI;
	using VRageMath;

    class ProcessFun : ProcessHandlerBase
	{
		public override int GetUpdateResolution()
		{
			return 2000;
		}

        private Random random = new Random();
		public override void Handle()
		{
		    //if ( DateTime.Now.Minute != 15 )
		    //    return;

		    HashSet<MyEntity> entities = new HashSet<MyEntity>();
            Wrapper.GameAction( ()=> entities = MyEntities.GetEntities( ) );
		    foreach (MyEntity entity in entities)
		    {
		        MyCharacter character = entity as MyCharacter;
		        if ( character == null )
		            continue;
                Communication.SendPublicInformation( "test" );

                Wrapper.GameAction(() => character.ChangeModelAndColor(character.ModelName, Color.AliceBlue.ColorToHSVDX11()));
		    }
			base.Handle();
		}
	}
}
