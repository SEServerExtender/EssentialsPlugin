namespace EssentialsPlugin.Utility
{
    using VRage.Game.Entity;
    using VRage.ModAPI;
    using VRageMath;

    public static class Extensions
    {
        public static void Stop(this IMyEntity entity)
        {
            if (entity?.Physics == null)
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
    }
}
