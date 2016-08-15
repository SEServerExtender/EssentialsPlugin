namespace EssentialsPlugin.Editors
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using Sandbox.Game.Entities;
    using VRage.Game.Entity;

    public partial class GridPicker : Form
    {
        public long SelectedEntity;
        private MyEntity[] _grids;
        public GridPicker()
        {
            InitializeComponent();
        }

        private void GridPicker_Load(object sender, EventArgs e)
        {
            LST_Entities.DoubleClick += LST_Entities_DoubleClick;
            _grids = MyEntities.GetEntities().Where( x => x is MyCubeGrid ).ToArray();
            foreach ( var grid in _grids )
                LST_Entities.Items.Add( $"{grid.DisplayName??""}:{grid.EntityId}" );
        }

        private void BTN_Ok_Click(object sender, EventArgs e)
        {
            SelectedEntity = _grids[LST_Entities.SelectedIndex].EntityId;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BTN_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        private void LST_Entities_DoubleClick(object sender, System.EventArgs e)
        {
            BTN_Ok_Click( sender, e );
        }
    }
}
