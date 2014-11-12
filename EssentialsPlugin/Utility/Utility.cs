using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEModAPIInternal.Support;
using VRageMath;

namespace EssentialsPlugin.Utility
{
	public static class Logging
	{
		public static void WriteLineAndConsole(string text)
		{
			LogManager.APILog.WriteLineAndConsole(text);
		}
	}

	public static class General
	{
		public static string TimeSpanToString(TimeSpan ts)
		{
			if (ts.Days > 0)
				return String.Format("{0}d:{1:D2}h:{2:D2}m:{3:D2}s", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
			else if (ts.Hours > 0)
				return String.Format("{0:D2}h:{1:D2}m:{2:D2}s", ts.Hours, ts.Minutes, ts.Seconds);
			else
				return String.Format("{0:D2}m:{1:D2}s", ts.Minutes, ts.Seconds);
		}

		public static string Vector3DToString(Vector3D vector)
		{
			return string.Format("({0:F2}, {1:F2}, {2:F2})", vector.X, vector.Y, vector.Z);
		}
	}
}
