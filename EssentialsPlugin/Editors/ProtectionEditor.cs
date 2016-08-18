namespace EssentialsPlugin.Editors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using Sandbox.Game.Entities;
    using Sandbox.Game.World;
    using Settings;
    using VRage.Game.Entity;

    public partial class ProtectionEditor : Form
    {
        public ProtectionEditor()
        {
            InitializeComponent();
        }
        
        private readonly string[] _modeDesc = {
                                         "These players are allowed to add blocks to this grid.",
                                         "These players are allowed to remove blocks from this grid.",
                                         "These players are allowed to paint blocks on this grid.",
                                         "These players are allowed to change ownership of blocks.",
                                         "These players are allowed to rename blocks on this grid.",
                                         "These players are allowed to rename this grid.",
                                         "These players are allowed to convert this grid to a station.",
                                         "These players are allowed to convert this grid to a ship.",
                                         "These players are allowed to delete this grid.",
                                     };
        private ProtectedItem _currentItem;
        private ProtectedItem.ProtectionSettings _currentSettings;
        private void ProtectionEditor_Load(object sender, EventArgs e)
        {
            UpdateListbox();
            
            CMB_Mode.Items.AddRange( new object[]
                                     {
                                         "Block Add",
                                         "Block Remove",
                                         "Block Paint",
                                         "Block Owner Change",
                                         "Block Rename",
                                         "Grid Rename",
                                         "Grid Convert To Station",
                                         "Grid Convert To Ship",
                                         "Grid Delete"
                                     } );
            //CMB_Mode.Items.AddRange( Enum.GetNames( typeof(ProtectedItem.ProtectionModeEnum) ) );
            List<FactionListItem> factions = new List<FactionListItem>();
            foreach ( var faction in MySession.Static.Factions )
            {
                if (faction.Value == null)
                    continue;
                factions.Add( new FactionListItem( faction.Value ) );
            }

            factions.Sort( (a,b)=>string.Compare( a.ToString(  ), b.ToString(  ), StringComparison.Ordinal ) );
            
            foreach (var fac in factions)
            {
                LST_Factions.Items.Add( fac );
            }

            if ( LST_Entries.Items.Count > 0 )
            {
                LST_Entries.SelectedIndex = 0;
                CMB_Mode.SelectedIndex = 0;
            }
            else
                splitContainer1.Panel2.Enabled = false;
        }
        
        private void UpdateListbox()
        {
            int selectedIndex = LST_Entries.SelectedIndex;
            int selectedCmbIndex = CMB_Mode.SelectedIndex;
            LST_Entries.BeginUpdate();
            LST_Entries.Items.Clear();
            foreach ( var item in PluginSettings.Instance.ProtectedItems )
            {
                MyEntity entity;
                if ( !MyEntities.TryGetEntityById( item.EntityId, out entity ) )
                {
                    LST_Entries.Items.Add( $"Invalid entityId: {item.EntityId}" );
                    continue;
                }
                var grid = entity as MyCubeGrid;
                if(grid==null)
                {
                    LST_Entries.Items.Add($"Invalid entityId: {item.EntityId}");
                    continue;
                }
                LST_Entries.Items.Add( $"{grid.DisplayName ?? ""}: {item.EntityId}" );
            }
            LST_Entries.SelectedIndex = selectedIndex;
            CMB_Mode.SelectedIndex = selectedCmbIndex;
            LST_Entries.EndUpdate();
        }

        private void LST_Entries_SelectedIndexChanged(object sender, EventArgs e)
        {
            _currentItem = PluginSettings.Instance.ProtectedItems[LST_Entries.SelectedIndex];
            CMB_Mode.SelectedIndex = 0;
            TXT_EntityId.Text = _currentItem.EntityId.ToString();
            var modeEnum = (ProtectedItem.ProtectionModeEnum)CMB_Mode.SelectedIndex;
            if (!_currentItem.ProtectionSettingsDict.Dictionary.ContainsKey(modeEnum))
                _currentItem.ProtectionSettingsDict.Dictionary.Add(modeEnum, new ProtectedItem.ProtectionSettings());
            _currentSettings = _currentItem.ProtectionSettingsDict[modeEnum];
            LoadCurrentSettings();
        }

        private void BTN_AddItem_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2.Enabled = true;
            PluginSettings.Instance.ProtectedItems.Add( new ProtectedItem() );
            UpdateListbox();
            LST_Entries.SelectedIndex = PluginSettings.Instance.ProtectedItems.Count - 1;
        }

        private void BTN_RemoveItem_Click(object sender, EventArgs e)
        {
            PluginSettings.Instance.ProtectedItems.RemoveAt( LST_Entries.SelectedIndex );
            if ( PluginSettings.Instance.ProtectedItems.Count == 0 )
            {
                LST_Entries.ClearSelected(  );
                LST_Entries.Items.Clear(  );
                splitContainer1.Panel2.Enabled = false;
            }
            else if ( LST_Entries.SelectedIndex >= PluginSettings.Instance.ProtectedItems.Count )
            {
                LST_Entries.SelectedIndex--;
                UpdateListbox( );
            }
        }

        private void BTN_SaveItem_Click(object sender, EventArgs e)
        {
            PluginSettings.Instance.Save();
            UpdateListbox();
        }

        private void CMB_Mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            LBL_ModeDesc.Text = _modeDesc[CMB_Mode.SelectedIndex];
            var modeEnum = (ProtectedItem.ProtectionModeEnum)CMB_Mode.SelectedIndex;
                if ( !_currentItem.ProtectionSettingsDict.Dictionary.ContainsKey( modeEnum ) )
                    _currentItem.ProtectionSettingsDict.Dictionary.Add( modeEnum, new ProtectedItem.ProtectionSettings() );
            _currentSettings = _currentItem.ProtectionSettingsDict[modeEnum];
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            CHK_Enabled.Checked = _currentItem.Enabled;
            CHK_Damage.Checked = _currentItem.ProtectDamage;
            CHK_LogOnly.Checked = _currentItem.LogOnly;
            CHK_Anyone.Checked = _currentSettings.AllExempt;
            CHK_BigOwner.Checked = _currentSettings.BigOwnerExempt;
            CHK_SmallOwner.Checked = _currentSettings.SmallOwnerExempt;
            CHK_Admin.Checked = _currentSettings.AdminExempt;
            CHK_Faction.Checked = _currentSettings.FactionExempt;

            TXT_PrivateWarn.Text = _currentSettings.PrivateWarningMessage;
            TXT_PublicWarn.Text = _currentSettings.PublicWarningMessage;
            CHK_SendGPS.Checked = _currentSettings.BroadcastGPS;
            switch ( _currentSettings.PunishmentType )
            {
                case ProtectedItem.PunishmentEnum.Kick:
                    RAD_Kick.Checked = true;
                    break;
                case ProtectedItem.PunishmentEnum.Ban:
                    RAD_Ban.Checked = true;
                    break;
                    case ProtectedItem.PunishmentEnum.Speed:
                    RAD_Speed.Checked = true;
                    break;
                case ProtectedItem.PunishmentEnum.None:
                default:
                    RAD_None.Checked = true;
                    break;
            }
            TXT_SpeedVal.Text = _currentSettings.SpeedLimit.ToString();
            TXT_SpeedTime.Text = _currentSettings.SpeedTime.ToString();
            LST_Factions.SelectedIndexChanged -= LST_Factions_SelectedIndexChanged;
            LST_Factions.SelectedItems.Clear(  );
            if (_currentSettings.Factions != null)
            {
                var itemsCopy = new object[LST_Factions.Items.Count];
                LST_Factions.Items.CopyTo( itemsCopy, 0 );
                foreach (var facItem in itemsCopy)
                {
                    if (_currentSettings.Factions.Contains( ( (FactionListItem)facItem ).Faction.FactionId ))
                        //LST_Factions.SelectedItems.Add( facItem );
                        LST_Factions.SetSelected( LST_Factions.Items.IndexOf( facItem ), true );
                }
            }
            LST_Factions.SelectedIndexChanged += LST_Factions_SelectedIndexChanged;
        }

        private void TXT_EntityId_TextChanged(object sender, EventArgs e)
        {
            MyEntity entity;
            long entityId;
            if(!long.TryParse( TXT_EntityId.Text, out entityId ))
                throw new ArgumentException();
            _currentItem.EntityId = entityId;
            MyEntities.TryGetEntityById( entityId, out entity );
            var grid = entity as MyCubeGrid;
            if (grid == null)
            {
                LBL_GridName.Text = "Invalid EntityID";
                return;
            }
            LBL_GridName.Text=grid.DisplayName ?? "";
            UpdateListbox();
        }

        private void CHK_Enabled_CheckedChanged(object sender, EventArgs e)
        {
            _currentItem.Enabled = CHK_Enabled.Checked;
        }

        private void CHK_Damage_CheckedChanged(object sender, EventArgs e)
        {
            _currentItem.Enabled = CHK_Damage.Checked;
        }

        private void CHK_LogOnly_CheckedChanged(object sender, EventArgs e)
        {
            _currentItem.LogOnly = CHK_LogOnly.Checked;
        }

        private void CHK_Anyone_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.AllExempt = CHK_Anyone.Checked;
        }

        private void CHK_BigOwner_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.BigOwnerExempt = CHK_BigOwner.Checked;
        }

        private void CHK_SmallOwner_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.SmallOwnerExempt = CHK_SmallOwner.Checked;
        }

        private void CHK_Admin_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.AdminExempt = CHK_Admin.Checked;
        }

        private void TXT_PrivateWarn_TextChanged(object sender, EventArgs e)
        {
            _currentSettings.PrivateWarningMessage = TXT_PrivateWarn.Text;
        }

        private void TXT_PublicWarn_TextChanged(object sender, EventArgs e)
        {
            _currentSettings.PublicWarningMessage = TXT_PublicWarn.Text;
        }

        private void CHK_SendGPS_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.BroadcastGPS = CHK_SendGPS.Checked;
        }

        private void RAD_None_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.PunishmentType=ProtectedItem.PunishmentEnum.None;
        }

        private void RAD_Kick_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.PunishmentType=ProtectedItem.PunishmentEnum.Kick;
        }

        private void RAD_Ban_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.PunishmentType=ProtectedItem.PunishmentEnum.Ban;
        }

        private void RAD_Speed_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.PunishmentType=ProtectedItem.PunishmentEnum.Speed;
            _currentSettings.SpeedLimit = double.Parse( TXT_SpeedVal.Text );
            _currentSettings.SpeedTime = double.Parse( TXT_SpeedTime.Text );
        }

        private void BTN_SteamId_Click(object sender, EventArgs e)
        {
            var editor = new StringEditor( _currentSettings.ExemptSteamIds );
            var result = editor.ShowDialog();
            if ( result == DialogResult.OK )
                _currentSettings.ExemptSteamIds = editor.Collection;
        }

        private void BTN_GroupId_Click(object sender, EventArgs e)
        {
            var editor = new StringEditor(_currentSettings.ExemptGroupIds);
            var result = editor.ShowDialog();
            if (result == DialogResult.OK)
                _currentSettings.ExemptGroupIds = editor.Collection;
        }

        private void BTN_FindEntity_Click(object sender, EventArgs e)
        {
            var picker = new GridPicker();
            var result = picker.ShowDialog();
            if ( result == DialogResult.OK )
                TXT_EntityId.Text = picker.SelectedEntity.ToString();
        }

        private void CHK_Faction_CheckedChanged(object sender, EventArgs e)
        {
            _currentSettings.FactionExempt = CHK_Faction.Checked;
        }

        private void TXT_SpeedVal_TextChanged(object sender, EventArgs e)
        {
            _currentSettings.SpeedLimit = double.Parse( TXT_SpeedVal.Text );
        }

        private void TXT_SpeedTime_TextChanged(object sender, EventArgs e)
        {
            _currentSettings.SpeedTime = double.Parse( TXT_SpeedTime.Text );
        }

        private void LST_Factions_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<long> facIds = new List<long>();
            foreach(var faction in LST_Factions.SelectedItems)
                facIds.Add( ((FactionListItem)faction).Faction.FactionId );
            _currentSettings.Factions = facIds.ToArray( );
        }
    }
}
