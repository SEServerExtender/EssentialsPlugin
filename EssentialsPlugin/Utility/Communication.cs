using System;
using System.Linq;
using SEModAPIExtensions.API;

namespace EssentialsPlugin.Utility
{
	public class Communication
	{
		public static void SendPublicInformation(String infoText)
		{
			if (infoText == "")
				return;

			Int32 maxLineLength = 45;
			Int32 charCount = 0;
			String[] lines = infoText.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / maxLineLength).Select(g => string.Join(" ", g)).ToArray();

			foreach (String line in lines)
			{
				if (infoText != "")
					ChatManager.Instance.SendPublicChatMessage(line);
			}
		}

		public static void SendPrivateInformation(ulong playerId, String infoText)
		{
			if (infoText == "")
				return;

			Int32 maxLineLength = 45;
			Int32 charCount = 0;
			String[] lines = infoText.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / maxLineLength).Select(g => string.Join(" ", g)).ToArray();

			foreach (String line in lines)
			{
				if (infoText != "")
					ChatManager.Instance.SendPrivateChatMessage(playerId, line);
			}
		}
	}
}
