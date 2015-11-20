﻿namespace EssentialsPlugin.Utility
{
	using System;
	using Sandbox.ModAPI;
	using VRageMath;


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

		public static Vector3 GenerateRandomEdgeVector()
		{
			float halfExtent = MyAPIGateway.Entities.WorldSafeHalfExtent();
            if (halfExtent == 0f)
                halfExtent = 900000f;
                //if world size is infinite, put the relay 900km away from center to prevent it spawning inside a planet
            else
                halfExtent -= 1000;

			return new Vector3(GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent), GenerateRandomCoord(halfExtent));
		}

		public static float GenerateRandomCoord(float halfExtent)
		{
			float result = (m_random.Next(1000) + halfExtent) * (m_random.Next(2) == 0 ? -1 : 1);
			return result;
		}
	}
}
