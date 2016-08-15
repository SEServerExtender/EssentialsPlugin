namespace EssentialsPlugin.Editors
{
    using System;
    using System.Text;
    using System.Windows.Forms;

    public partial class StringEditor : Form
    {
        public string[] Collection;
        public StringEditor( string[] arg )
        {
            Collection = arg;
            InitializeComponent();
        }

        private void StringEditor_Load( object sender, EventArgs e )
        {
            StringBuilder sb = new StringBuilder();
            if ( Collection == null )
            {
                TXT_Box.Clear();
                return;
            }
            foreach ( string entry in Collection )
                sb.AppendLine( entry );
            TXT_Box.Text = sb.ToString();
        }

        private void BTN_Ok_Click(object sender, EventArgs e)
        {
            Collection = TXT_Box.Text.Split( new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries );
            this.DialogResult=DialogResult.OK;
            Close();
        }

        private void BTN_Cancel_Click(object sender, EventArgs e)
        {
            DialogResult=DialogResult.Cancel;
            Close();
        }
    }
}
