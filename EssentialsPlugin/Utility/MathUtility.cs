using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace EssentialsPlugin.Utility
{
	public static class MathUtility
	{
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
	}
}
