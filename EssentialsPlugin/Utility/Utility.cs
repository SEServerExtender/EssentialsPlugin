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

		public static bool InheritsOrImplements(this Type child, Type parent)
		{
			parent = ResolveGenericTypeDefinition(parent);

			var currentChild = child.IsGenericType
								   ? child.GetGenericTypeDefinition()
								   : child;

			while (currentChild != typeof(object))
			{
				if (parent == currentChild || HasAnyInterfaces(parent, currentChild))
					return true;

				currentChild = currentChild.BaseType != null
							   && currentChild.BaseType.IsGenericType
								   ? currentChild.BaseType.GetGenericTypeDefinition()
								   : currentChild.BaseType;

				if (currentChild == null)
					return false;
			}
			return false;
		}

		private static bool HasAnyInterfaces(Type parent, Type child)
		{
			return child.GetInterfaces()
				.Any(childInterface =>
				{
					var currentInterface = childInterface.IsGenericType
						? childInterface.GetGenericTypeDefinition()
						: childInterface;

					return currentInterface == parent;
				});
		}

		private static Type ResolveGenericTypeDefinition(Type parent)
		{
			var shouldUseGenericType = true;
			if (parent.IsGenericType && parent.GetGenericTypeDefinition() != parent)
				shouldUseGenericType = false;

			if (parent.IsGenericType && shouldUseGenericType)
				parent = parent.GetGenericTypeDefinition();
			return parent;
		}

		public static T[] RemoveAt<T>(this T[] source, int index)
		{
			T[] dest = new T[source.Length - 1];
			if (index > 0)
				Array.Copy(source, 0, dest, 0, index);

			if (index < source.Length - 1)
				Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

			return dest;
		}
	}
}
