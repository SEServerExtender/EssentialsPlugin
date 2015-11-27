namespace EssentialsPlugin.Utility
{
	using System;
	using Sandbox.ModAPI;
	using VRageMath;
    using SEModAPIInternal.Support;
    using EssentialsPlugin;
    using NLog;


	public static class MathUtility
	{

		private static Random m_random = new Random();

		public static Quaternion GetRotation(Vector3 source, Vector3 dest, Vector3 up)
		{
			float dot = Vector3.Dot(source, dest);
			if (Math.Abs(dot - (-1.0f)) < 0.000001f)
			{
				// vector a and b point exactly in the opposite direction, 
				// so it is a 180 degrees turn around the up-axis
				return new Quaternion(up, MathHelper.ToRadians(180.0f));
			}
			if (Math.Abs(dot - (1.0f)) < 0.000001f)
			{
				// vector a and b point exactly in the same direction
				// so we return the identity quaternion
				return Quaternion.Identity;
			}

			float rotAngle = (float)Math.Acos(dot);
			Vector3 rotAxis = Vector3.Cross(source, dest);
			rotAxis = Vector3.Normalize(rotAxis);
			return Quaternion.CreateFromAxisAngle(rotAxis, rotAngle);
		}

		public static Vector3D RandomPositionFromPoint(Vector3D start, double distance)
		{
			Vector3 randomPoint = new Vector3(m_random.Next(-100000, 100000), m_random.Next(-100000, 100000), m_random.Next(-100000, 100000));
			Vector3 directionNormal = randomPoint - start;
			directionNormal.Normalize();
			directionNormal = (directionNormal * (long)distance) + start;
			return directionNormal;
		}

        public static Vector3D GenerateRandomEdgeVector(Vector3D? playerPosition = null)
        {
            float halfExtent = MyAPIGateway.Entities.WorldSafeHalfExtent();
            Vector3D playerPos = new Vector3D();

            float testRadius = (halfExtent == 0 ? 10000f : 3000f);

            if (playerPosition == null)
            {
                playerPos = new Vector3D(0, 0, 0);
            }
            else
            { 
                playerPos = (Vector3D)playerPosition;
            }

                Vector3D? freePlace = MyAPIGateway.Entities.FindFreePlace(playerPos, testRadius);

                if (freePlace != null)
                {
                    ApplicationLog.BaseLog.Debug("freePlace is not null");
                    return (Vector3D)freePlace;
                }

                else
                {
                    ApplicationLog.BaseLog.Debug("freePlace is null");
                    return new Vector3D(4000, 4000, 4000);
                }
                /*
                //We should fail if we pass too many iterations, but whatever

                halfExtent += (halfExtent == 0 ? 900000 : -1000);
                //if world is infinite (halfExtent == 0) set a bound of 900km. Else, set a bound of halfExtent -1000 so position isn't too close to world edge

                Vector3 vectorPosition = new Vector3(GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent)); ;
                //get a new random position vector

                int boundRadius = (halfExtent < 10000 ? 3000 : 10000);
                //if we're in a world less than 20km large, drop the radius to 3km, otherwise there are no valid positions

                BoundingSphereD positionSphere = new BoundingSphereD(vectorPosition, boundRadius);
                //create a sphere around the position with a radius of 10km

                ApplicationLog.BaseLog.Debug("Trying first generated position...");
                while (MyAPIGateway.Entities.GetIntersectionWithSphere(ref positionSphere) != null)
                {
                    vectorPosition = new Vector3(GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent));
                    positionSphere = new BoundingSphereD(vectorPosition, boundRadius);
                    //make sure there is nothing within 10km of position
                    ApplicationLog.BaseLog.Debug("Position failed, retrying new position...");
                }
                ApplicationLog.BaseLog.Debug("Position valid, continuing.");
                return vectorPosition;*/
            
            /*
            halfExtent += (halfExtent == 0 ? 900000 : -1000);
            //this is a hack. I can't get the client to load in grids we create, so we're putting it close enough to the player that it renders
            Vector3D vectorPosition = new Vector3D();
            BoundingSphereD positionSphere = new BoundingSphereD();
            if (playerPosition == null)
            {
                vectorPosition = new Vector3D(GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent));
                positionSphere = new BoundingSphereD(vectorPosition, 5000);
            }

            else
            {
                Vector3D playerPos = (Vector3D)playerPosition;
                vectorPosition = new Vector3D(GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent));
               Vector3D.ClampToSphere(ref vectorPosition, 400);
                vectorPosition += playerPos;
                positionSphere = new BoundingSphereD(vectorPosition, 700000);
            }


            ApplicationLog.BaseLog.Debug("Trying first generated position...");
            while (MyAPIGateway.Entities.GetIntersectionWithSphere(ref positionSphere) != null)
            {

                if (playerPosition == null)
                {
                    vectorPosition = new Vector3D(GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent));
                    positionSphere = new BoundingSphereD(vectorPosition, 5000);
                }

                else
                {
                    Vector3D playerPos = (Vector3D)playerPosition;
                    vectorPosition = new Vector3D(GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent));
                    Vector3D.ClampToSphere(ref vectorPosition, 800);
                    vectorPosition += playerPos;
                    positionSphere = new BoundingSphereD(vectorPosition, 700);
                }
                ApplicationLog.BaseLog.Debug("Position failed, retrying new position...");
                ApplicationLog.BaseLog.Debug("Position - X:{0:F2} Y:{1:F2} Z:{2:F2}", vectorPosition.X, vectorPosition.Y, vectorPosition.Z);

            }

            ApplicationLog.BaseLog.Debug("Position valid, continuing.");
            */

                //return vectorPosition;
        }

		public static float GenerateRandomCoord(float halfExtent)
		{
            float result = (m_random.Next((int)halfExtent)) * (m_random.Next(2) == 0 ? -1 : 1);
            //return a random distance between origin and +/- halfExtent
			return result;
            
		}
	}
}
