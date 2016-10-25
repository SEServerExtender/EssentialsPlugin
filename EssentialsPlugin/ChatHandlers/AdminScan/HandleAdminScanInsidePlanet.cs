namespace EssentialsPlugin.ChatHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using EssentialsPlugin.Utility;
	using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Character;
    using VRageMath;

    public class HandleAdminScanInsidePlanet : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This command allows you to scan for entities that are trapped inside planets. Usage /admin scan insideplanet";
		}
		public override string GetCommandText()
		{
			return "/admin scan insideplanet";
		}
        
        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}
        
		public override bool HandleCommand(ulong userId, string[] words)
		{
		    var entities = MyEntities.GetEntities( ).ToArray( );
		    var planets = new HashSet<MyPlanet>( );
		    int count = 0;
		    foreach (var entity in entities)
		    {
		        MyPlanet item = entity as MyPlanet;
		        if (item != null)
		            planets.Add( item );
		    }

		    foreach (var planet in planets)
		    {
		        var sphere25 = new BoundingSphereD( planet.PositionComp.GetPosition( ), planet.MinimumRadius * 0.25 );
		        var sphere75 = new BoundingSphereD( planet.PositionComp.GetPosition( ), planet.MinimumRadius * 0.75 );
		        foreach (var entity in entities)
		        {
		            if (entity.MarkedForClose || entity.Physics == null || entity is MyCharacter)
		                continue;

		            if (sphere25.Contains( entity.PositionComp.GetPosition( ) ) != ContainmentType.Disjoint)
		            {
		                count++;
		                continue;
		            }

		            if (Vector3.IsZero( entity.Physics.LinearVelocity ))
		                continue;

		            if (sphere75.Contains( entity.PositionComp.GetPosition( ) ) == ContainmentType.Disjoint)
		                continue;

		            count++;
		        }
		    }
            
            Communication.SendPrivateInformation( userId, $"Found {count} entities trapped in planets." );
		    return true;
		}
	}
}
