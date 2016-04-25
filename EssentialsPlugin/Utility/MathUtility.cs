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

        public static Vector3D GenerateRandomEdgeVector( )
        {
            float halfExtent = MyAPIGateway.Entities.WorldSafeHalfExtent( );
            int maxDist = (halfExtent == 0 ? 100000 : (int)halfExtent);
            //if the world is infinite, set an upper bound of 100km for spawn location
            int minDist = (halfExtent == 0 ? 150000 : 0);
            //if the world is infinite, set a lower bound of 150km to give us a better chance of not spawning in a planet
            //if the world has a limited distance, anywhere from origin to safe half extent is fair game.
            maxDist += minDist;
            //add our bounds together to create a shell of valid positions about the origin

            int trycount = 0;
            Vector3D vectorPosition;
            BoundingSphereD positionSphere = new BoundingSphereD( );

            vectorPosition = new Vector3D( GenerateRandomCoord( maxDist, minDist ), GenerateRandomCoord( maxDist, minDist ), GenerateRandomCoord( maxDist, minDist ) );
            positionSphere = new BoundingSphereD( vectorPosition, 5000 );

            ApplicationLog.BaseLog.Debug( "Trying first generated position..." );

            //check if there's anything within 5km of our generated position
            while ( MyAPIGateway.Entities.GetIntersectionWithSphere( ref positionSphere ) != null )
            {
                vectorPosition = new Vector3D( GenerateRandomCoord( maxDist, minDist ), GenerateRandomCoord( maxDist, minDist ), GenerateRandomCoord( maxDist, minDist ) );
                positionSphere = new BoundingSphereD( vectorPosition, 5000 );

                ApplicationLog.BaseLog.Debug( "Position failed, retrying new position..." );
                ApplicationLog.BaseLog.Debug( "Position - X:{0:F2} Y:{1:F2} Z:{2:F2}", vectorPosition.X, vectorPosition.Y, vectorPosition.Z );

                ++trycount;

                //try 20 times before giving up
                //we should probably do something other than break on fail, but the distances are so huge, it's unlikely
                if ( trycount > 20 )
                    break;
            }

            return vectorPosition;
        }

        public static float GenerateRandomCoord( int maxDist, int minDist )
        {            
            float result = (m_random.Next( minDist, maxDist )) * (m_random.Next( 2 ) == 0 ? -1 : 1);
            return result;
        }

        public static Vector3D? TraceVector(Vector3D position, Vector3D velocity, int distance, int radius = 100)
        {
            Vector3D normVelocity = Vector3D.Normalize(velocity);
            Vector3D result = normVelocity * distance + position;

            //make sure the point is clear
            int trycount = 1;
            BoundingSphereD checkSphere = new BoundingSphereD(result, radius);
            while (MyAPIGateway.Entities.GetIntersectionWithSphere(ref checkSphere) != null)
            {
                //try to find a location 20 times, increasing distance from start each try                
                trycount++;
                result = normVelocity * (distance * trycount) + position;
                checkSphere = new BoundingSphereD(result, radius);

                if (trycount > 20)
                    return null;
            }
            return result;
        }
    }
}

