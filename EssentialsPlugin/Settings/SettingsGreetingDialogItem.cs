using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace EssentialsPlugin.Settings
{
	public class SettingsGreetingDialogItem
	{
		private bool enabled;
		[Description("Should we display a dialog to a user who logs in?")]
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		private string title;
		[Description("The title at the top of the dialog")]
		public string Title
		{
			get { return title; }
			set { title = value; }
		}

		private string header;
		[Description("The subheader above the content")]
		public string Header
		{
			get { return header; }
			set { header = value; }
		}

		private string contents;
		[Description("The contents of the dialog.  This area has a scroll bar and can be long.  Use the '|' character as a carriage return.")]
		public string Contents
		{
			get { return contents; }
			set { contents = value; }
		}

		private string buttonText;
		[Description("The text in the button at the bottom of the dialog")]
		public string ButtonText
		{
			get { return buttonText; }
			set { buttonText = value; }
		}

		public override string ToString()
		{
			return "<-- click to expand and set values";
		}
	}
}
