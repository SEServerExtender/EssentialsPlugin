namespace EssentialsPlugin.UtilityClasses
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.Design;
	using System.Drawing.Design;
	using System.Windows.Forms;
	using System.Windows.Forms.Design;
	using System.Windows.Forms.VisualStyles;
	using Editors;

    internal class TimePickerEditor : UITypeEditor
	{
		IWindowsFormsEditorService editorService;
		string time;

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			if (provider != null)
			{
				editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
			}
			if (editorService != null)
			{
				if (value == null)
				{
					time = DateTime.Now.ToString("HH:mm");
				}

				DateTimePicker picker = new DateTimePicker();
				picker.Format = DateTimePickerFormat.Custom;
				picker.CustomFormat = "HH:mm";
				picker.ShowUpDown = true;

				if (value != null)
				{
					picker.Value = DateTime.Parse((string)value);
				}

				editorService.DropDownControl(picker);
				value = picker.Value.ToString("HH:mm");
			}
			return value;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.DropDown;
		}
	}
    /// <summary>
    /// Allows the description pane of the PropertyGrid to be shown when editing a collection of items within a PropertyGrid.
    /// </summary>
    internal class DescriptiveCollectionEditor : CollectionEditor
    {
        public DescriptiveCollectionEditor(Type type) : base(type) { }
        protected override CollectionForm CreateCollectionForm()
        {
            CollectionForm form = base.CreateCollectionForm();
            form.Shown += delegate
            {
                ShowDescription(form);
            };
            return form;
        }
        static void ShowDescription(Control control)
        {
            PropertyGrid grid = control as PropertyGrid;
            if (grid != null) grid.HelpVisible = true;
            foreach (Control child in control.Controls)
            {
                ShowDescription(child);
            }
        }
    }

    internal class ProtectionEditButton : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            ProtectionEditor form = new ProtectionEditor();
            form.Show();
            return value;
        }
    }
}
