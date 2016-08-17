namespace EssentialsPlugin.Editors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using Sandbox.Game.Entities;
    using VRage.Game.Entity;

    public partial class GridPicker : Form
    {
        public long SelectedEntity;
        public GridPicker()
        {
            InitializeComponent();
        }

        private void GridPicker_Load(object sender, EventArgs e)
        {
            LST_Entities.DoubleClick += LST_Entities_DoubleClick;
            List<GridListItem> grids = new List<GridListItem>();
            foreach (var entity in MyEntities.GetEntities( ))
            {
                if(entity is MyCubeGrid && !entity.Closed && entity.Physics!=null)
                    grids.Add( new GridListItem( (MyCubeGrid)entity ) );
            }

            grids.Sort( (a,b) => string.Compare( a.ToString(  ), b.ToString(  ), StringComparison.Ordinal ) );

            foreach ( var grid in grids )
                LST_Entities.Items.Add( grid );
        }

        private void BTN_Ok_Click(object sender, EventArgs e)
        {
            SelectedEntity = ((GridListItem)LST_Entities.SelectedItem).Grid.EntityId;
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
