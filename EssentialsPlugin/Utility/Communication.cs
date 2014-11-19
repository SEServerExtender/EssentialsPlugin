using System;
using System.Linq;
using SEModAPIExtensions.API;

namespace EssentialsPlugin.Utility
{
	public class Communication
	{
		public static void SendPublicInformation(String infoText)
		{
			SendPublicInformation(infoText, true);
		}

		public static void SendPublicInformation(String infoText, bool wrap)
		{
			if (infoText == "")
				return;

			String[] lines;
			if (wrap)
			{
				Int32 maxLineLength = 32;
				Int32 charCount = 0;
				lines = infoText.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / maxLineLength).Select(g => string.Join(" ", g)).ToArray();
			}
			else
				lines = new string[] { infoText };

			foreach (String line in lines)
			{
				if (infoText != "")
					ChatManager.Instance.SendPublicChatMessage(line);
			}
		}

		public static void SendPrivateInformation(ulong playerId, String infoText, bool wrap = true)
		{
			if (infoText == "")
				return;

			String[] lines;

			if (wrap)
			{
				Int32 maxLineLength = 32;
				Int32 charCount = 0;
				lines = infoText.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / maxLineLength).Select(g => string.Join(" ", g)).ToArray();
			}
			else
				lines = new string[] { infoText };

			foreach (String line in lines)
			{
				if (infoText != "")
					ChatManager.Instance.SendPrivateChatMessage(playerId, line);
			}
		}
	}
}
