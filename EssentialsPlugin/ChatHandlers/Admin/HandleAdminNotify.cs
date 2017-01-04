namespace EssentialsPlugin.ChatHandlers.Admin
{
	using System;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Sandbox.Common;
	using VRage.Game;

    public class HandleAdminNotify : ChatHandlerBase
	{
		public override string GetHelp()
		{
			return "This will broadcast a notification to all users.  Usage: /admin notify <color> <time> <message>";
		}

		public override string GetCommandText()
		{
			return "/admin notify";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            string fontList = GetFontList( ).Replace( ",", "|" );
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "Admin Notification";
            DialogItem.content = "This will broadcast a notification to all users.|" +
                "Usage: /admin notify <color> <time> <message>|" +
                "Message will be displayed for the number of seconds given in the <time> argument.||" +
                "Colors:| " + fontList;
            DialogItem.buttonText = "close";
            return DialogItem;
        }

        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		// admin deletearea x y z radius
		public override bool HandleCommand(ulong userId, string[] words)
		{
			if (words.Length != 3)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			string font = words[0];
			//if (!Enum.TryParse<MyFontEnum>(colour, out font))
			//{
			//	Communication.SendPrivateInformation(userId, string.Format("Invalid colour value entered.  {0} is nto a valid value.  Please enter one of the following: {1}", colour, GetFontList()));
			//	return true;
			//}

			int timeInSeconds = 2;
			if (!int.TryParse(words[1], out timeInSeconds) || timeInSeconds < 1)
			{
				Communication.SendPrivateInformation(userId, string.Format("Invalid time value entered.  {0} is not a valid value.  Please enter a value above 0"));
				return true;
			}

			string message = string.Join(" ", words.Skip(2).ToArray());
			Communication.Notification(0, font, (timeInSeconds * 1000), message);
			return true;
		}

		private string GetFontList()
		{
			string result = "";
			foreach(string name in Enum.GetNames(typeof(MyFontEnum)))
			{
				if(result != "")
					result += ", ";

				result += name;
			}

			return result;
		}
	}
}
