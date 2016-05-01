namespace EssentialsPlugin.Utility
{
    using Sandbox.Game.Entities;
    using Sandbox.Game.World;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public static class Extensions
    {
        public static void Stop(this IMyEntity entity)
        {
            if (entity?.Physics == null || entity.Closed)
                return;

            Wrapper.GameAction(() =>
            {
                entity.Physics.LinearVelocity = Vector3D.Zero;
                entity.Physics.AngularVelocity = Vector3D.Zero;
            });
        }
        public static void Stop(this MyEntity entity)
        {
            ((IMyEntity)entity).Stop(  );
        }

        public static string GetOwner( this IMyCubeGrid grid )
        {
            if (grid.BigOwners.Count > 0 && grid.BigOwners[0] > 0)
            {
                MyIdentity ownerIdentity = MySession.Static.Players.TryGetIdentity( grid.BigOwners[0] );

                if (ownerIdentity != null)
                    return ownerIdentity.DisplayName;
            }
            return null;
        }

        public static string GetOwner( this MyCubeGrid grid )
        {
            return ((IMyCubeGrid)grid).GetOwner(  );
        }
    }
}
