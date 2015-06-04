namespace EssentialsPlugin.Settings
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.Design;
	using System.Drawing.Design;

	public class SettingsGreetingDialogItem : INotifyPropertyChanged
	{		
		private bool enabled;
		[Description("Should we display a dialog to a user who logs in?")]
		public bool Enabled
		{
			get { return enabled; }
			set 
			{
				enabled = value;
				NotifyPropertyChanged();
			}
		}

		private string title;
		[Description("The title at the top of the dialog")]
		public string Title
		{
			get { return title; }
			set 
			{
				title = value;
				NotifyPropertyChanged();
			}
		}

		private string header;
		[Description("The subheader above the content")]
		public string Header
		{
			get { return header; }
			set 
			{
				header = value;
				NotifyPropertyChanged();
			}
		}

		private string contents;
		[Description("The contents of the dialog.  This area has a scroll bar and can be long and use carriage returns.")]
		[Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
		public string Contents
		{
			get { return contents; }
			set 
			{
				contents = value;
				NotifyPropertyChanged();
			}
		}

		private string buttonText;
		[Description("The text in the button at the bottom of the dialog")]
		public string ButtonText
		{
			get { return buttonText; }
			set
			{
				buttonText = value;
				NotifyPropertyChanged();
			}
		}

		public override string ToString()
		{
			return "<-- click to expand and set values";
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String propertyName = "")
		{			
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
