namespace EssentialsPlugin.Editors
{
    partial class ProtectionEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.BTN_SaveItem = new System.Windows.Forms.Button();
            this.BTN_RemoveItem = new System.Windows.Forms.Button();
            this.BTN_AddItem = new System.Windows.Forms.Button();
            this.LST_Entries = new System.Windows.Forms.ListBox();
            this.PNL_ItemDetails = new System.Windows.Forms.Panel();
            this.CHK_Faction = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.LST_Factions = new System.Windows.Forms.ListBox();
            this.BTN_GroupId = new System.Windows.Forms.Button();
            this.BTN_SteamId = new System.Windows.Forms.Button();
            this.CHK_SendGPS = new System.Windows.Forms.CheckBox();
            this.label10 = new System.Windows.Forms.Label();
            this.TXT_SpeedTime = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.TXT_SpeedVal = new System.Windows.Forms.TextBox();
            this.RAD_Speed = new System.Windows.Forms.RadioButton();
            this.CMB_Mode = new System.Windows.Forms.ComboBox();
            this.TXT_PublicWarn = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.TXT_PrivateWarn = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.RAD_Ban = new System.Windows.Forms.RadioButton();
            this.RAD_Kick = new System.Windows.Forms.RadioButton();
            this.RAD_None = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.CHK_Admin = new System.Windows.Forms.CheckBox();
            this.CHK_SmallOwner = new System.Windows.Forms.CheckBox();
            this.CHK_BigOwner = new System.Windows.Forms.CheckBox();
            this.CHK_Anyone = new System.Windows.Forms.CheckBox();
            this.LBL_ModeDesc = new System.Windows.Forms.Label();
            this.PNL_ItemBasic = new System.Windows.Forms.Panel();
            this.CHK_LogOnly = new System.Windows.Forms.CheckBox();
            this.LBL_GridName = new System.Windows.Forms.Label();
            this.BTN_FindEntity = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.TXT_EntityId = new System.Windows.Forms.TextBox();
            this.CHK_Enabled = new System.Windows.Forms.CheckBox();
            this.CHK_Damage = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.PNL_ItemDetails.SuspendLayout();
            this.PNL_ItemBasic.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.BTN_SaveItem);
            this.splitContainer1.Panel1.Controls.Add(this.BTN_RemoveItem);
            this.splitContainer1.Panel1.Controls.Add(this.BTN_AddItem);
            this.splitContainer1.Panel1.Controls.Add(this.LST_Entries);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.PNL_ItemDetails);
            this.splitContainer1.Panel2.Controls.Add(this.PNL_ItemBasic);
            this.splitContainer1.Size = new System.Drawing.Size(691, 393);
            this.splitContainer1.SplitterDistance = 211;
            this.splitContainer1.TabIndex = 0;
            // 
            // BTN_SaveItem
            // 
            this.BTN_SaveItem.Location = new System.Drawing.Point(139, 360);
            this.BTN_SaveItem.Name = "BTN_SaveItem";
            this.BTN_SaveItem.Size = new System.Drawing.Size(58, 23);
            this.BTN_SaveItem.TabIndex = 3;
            this.BTN_SaveItem.Text = "Save";
            this.BTN_SaveItem.UseVisualStyleBackColor = true;
            this.BTN_SaveItem.Click += new System.EventHandler(this.BTN_SaveItem_Click);
            // 
            // BTN_RemoveItem
            // 
            this.BTN_RemoveItem.Location = new System.Drawing.Point(76, 360);
            this.BTN_RemoveItem.Name = "BTN_RemoveItem";
            this.BTN_RemoveItem.Size = new System.Drawing.Size(58, 23);
            this.BTN_RemoveItem.TabIndex = 2;
            this.BTN_RemoveItem.Text = "Remove";
            this.BTN_RemoveItem.UseVisualStyleBackColor = true;
            this.BTN_RemoveItem.Click += new System.EventHandler(this.BTN_RemoveItem_Click);
            // 
            // BTN_AddItem
            // 
            this.BTN_AddItem.Location = new System.Drawing.Point(12, 360);
            this.BTN_AddItem.Name = "BTN_AddItem";
            this.BTN_AddItem.Size = new System.Drawing.Size(58, 23);
            this.BTN_AddItem.TabIndex = 1;
            this.BTN_AddItem.Text = "Add";
            this.BTN_AddItem.UseVisualStyleBackColor = true;
            this.BTN_AddItem.Click += new System.EventHandler(this.BTN_AddItem_Click);
            // 
            // LST_Entries
            // 
            this.LST_Entries.FormattingEnabled = true;
            this.LST_Entries.HorizontalScrollbar = true;
            this.LST_Entries.Location = new System.Drawing.Point(12, 12);
            this.LST_Entries.Name = "LST_Entries";
            this.LST_Entries.Size = new System.Drawing.Size(185, 342);
            this.LST_Entries.TabIndex = 0;
            this.LST_Entries.SelectedIndexChanged += new System.EventHandler(this.LST_Entries_SelectedIndexChanged);
            // 
            // PNL_ItemDetails
            // 
            this.PNL_ItemDetails.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PNL_ItemDetails.Controls.Add(this.CHK_Faction);
            this.PNL_ItemDetails.Controls.Add(this.label3);
            this.PNL_ItemDetails.Controls.Add(this.LST_Factions);
            this.PNL_ItemDetails.Controls.Add(this.BTN_GroupId);
            this.PNL_ItemDetails.Controls.Add(this.BTN_SteamId);
            this.PNL_ItemDetails.Controls.Add(this.CHK_SendGPS);
            this.PNL_ItemDetails.Controls.Add(this.label10);
            this.PNL_ItemDetails.Controls.Add(this.TXT_SpeedTime);
            this.PNL_ItemDetails.Controls.Add(this.label9);
            this.PNL_ItemDetails.Controls.Add(this.TXT_SpeedVal);
            this.PNL_ItemDetails.Controls.Add(this.RAD_Speed);
            this.PNL_ItemDetails.Controls.Add(this.CMB_Mode);
            this.PNL_ItemDetails.Controls.Add(this.TXT_PublicWarn);
            this.PNL_ItemDetails.Controls.Add(this.label8);
            this.PNL_ItemDetails.Controls.Add(this.TXT_PrivateWarn);
            this.PNL_ItemDetails.Controls.Add(this.label7);
            this.PNL_ItemDetails.Controls.Add(this.RAD_Ban);
            this.PNL_ItemDetails.Controls.Add(this.RAD_Kick);
            this.PNL_ItemDetails.Controls.Add(this.RAD_None);
            this.PNL_ItemDetails.Controls.Add(this.label6);
            this.PNL_ItemDetails.Controls.Add(this.CHK_Admin);
            this.PNL_ItemDetails.Controls.Add(this.CHK_SmallOwner);
            this.PNL_ItemDetails.Controls.Add(this.CHK_BigOwner);
            this.PNL_ItemDetails.Controls.Add(this.CHK_Anyone);
            this.PNL_ItemDetails.Controls.Add(this.LBL_ModeDesc);
            this.PNL_ItemDetails.Location = new System.Drawing.Point(3, 76);
            this.PNL_ItemDetails.Name = "PNL_ItemDetails";
            this.PNL_ItemDetails.Size = new System.Drawing.Size(478, 318);
            this.PNL_ItemDetails.TabIndex = 4;
            // 
            // CHK_Faction
            // 
            this.CHK_Faction.AutoSize = true;
            this.CHK_Faction.Location = new System.Drawing.Point(358, 41);
            this.CHK_Faction.Name = "CHK_Faction";
            this.CHK_Faction.Size = new System.Drawing.Size(95, 17);
            this.CHK_Faction.TabIndex = 30;
            this.CHK_Faction.Text = "Owner Faction";
            this.CHK_Faction.UseVisualStyleBackColor = true;
            this.CHK_Faction.CheckedChanged += new System.EventHandler(this.CHK_Faction_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(164, 64);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(50, 13);
            this.label3.TabIndex = 29;
            this.label3.Text = "Factions:";
            // 
            // LST_Factions
            // 
            this.LST_Factions.FormattingEnabled = true;
            this.LST_Factions.Location = new System.Drawing.Point(167, 80);
            this.LST_Factions.Name = "LST_Factions";
            this.LST_Factions.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.LST_Factions.Size = new System.Drawing.Size(286, 95);
            this.LST_Factions.TabIndex = 28;
            // 
            // BTN_GroupId
            // 
            this.BTN_GroupId.Location = new System.Drawing.Point(9, 111);
            this.BTN_GroupId.Name = "BTN_GroupId";
            this.BTN_GroupId.Size = new System.Drawing.Size(132, 25);
            this.BTN_GroupId.TabIndex = 27;
            this.BTN_GroupId.Text = "Allowed Steam Groups";
            this.BTN_GroupId.UseVisualStyleBackColor = true;
            this.BTN_GroupId.Click += new System.EventHandler(this.BTN_GroupId_Click);
            // 
            // BTN_SteamId
            // 
            this.BTN_SteamId.Location = new System.Drawing.Point(8, 80);
            this.BTN_SteamId.Name = "BTN_SteamId";
            this.BTN_SteamId.Size = new System.Drawing.Size(133, 25);
            this.BTN_SteamId.TabIndex = 26;
            this.BTN_SteamId.Text = "Allowed Steam IDs";
            this.BTN_SteamId.UseVisualStyleBackColor = true;
            this.BTN_SteamId.Click += new System.EventHandler(this.BTN_SteamId_Click);
            // 
            // CHK_SendGPS
            // 
            this.CHK_SendGPS.AutoSize = true;
            this.CHK_SendGPS.Location = new System.Drawing.Point(147, 243);
            this.CHK_SendGPS.Name = "CHK_SendGPS";
            this.CHK_SendGPS.Size = new System.Drawing.Size(99, 17);
            this.CHK_SendGPS.TabIndex = 25;
            this.CHK_SendGPS.Text = "Broadcast GPS";
            this.CHK_SendGPS.UseVisualStyleBackColor = true;
            this.CHK_SendGPS.CheckedChanged += new System.EventHandler(this.CHK_SendGPS_CheckedChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(351, 282);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(43, 13);
            this.label10.TabIndex = 23;
            this.label10.Text = "minutes";
            // 
            // TXT_SpeedTime
            // 
            this.TXT_SpeedTime.Location = new System.Drawing.Point(321, 279);
            this.TXT_SpeedTime.Name = "TXT_SpeedTime";
            this.TXT_SpeedTime.Size = new System.Drawing.Size(24, 20);
            this.TXT_SpeedTime.TabIndex = 22;
            this.TXT_SpeedTime.TextChanged += new System.EventHandler(this.TXT_SpeedTime_TextChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(281, 282);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(40, 13);
            this.label9.TabIndex = 21;
            this.label9.Text = "m/s for";
            // 
            // TXT_SpeedVal
            // 
            this.TXT_SpeedVal.Location = new System.Drawing.Point(254, 279);
            this.TXT_SpeedVal.Name = "TXT_SpeedVal";
            this.TXT_SpeedVal.Size = new System.Drawing.Size(24, 20);
            this.TXT_SpeedVal.TabIndex = 20;
            this.TXT_SpeedVal.TextChanged += new System.EventHandler(this.TXT_SpeedVal_TextChanged);
            // 
            // RAD_Speed
            // 
            this.RAD_Speed.AutoSize = true;
            this.RAD_Speed.Location = new System.Drawing.Point(167, 280);
            this.RAD_Speed.Name = "RAD_Speed";
            this.RAD_Speed.Size = new System.Drawing.Size(90, 17);
            this.RAD_Speed.TabIndex = 19;
            this.RAD_Speed.TabStop = true;
            this.RAD_Speed.Text = "Limit speed to";
            this.RAD_Speed.UseVisualStyleBackColor = true;
            this.RAD_Speed.CheckedChanged += new System.EventHandler(this.RAD_Speed_CheckedChanged);
            // 
            // CMB_Mode
            // 
            this.CMB_Mode.FormattingEnabled = true;
            this.CMB_Mode.Location = new System.Drawing.Point(6, 5);
            this.CMB_Mode.Name = "CMB_Mode";
            this.CMB_Mode.Size = new System.Drawing.Size(156, 21);
            this.CMB_Mode.TabIndex = 18;
            this.CMB_Mode.SelectedIndexChanged += new System.EventHandler(this.CMB_Mode_SelectedIndexChanged);
            // 
            // TXT_PublicWarn
            // 
            this.TXT_PublicWarn.Location = new System.Drawing.Point(147, 217);
            this.TXT_PublicWarn.Name = "TXT_PublicWarn";
            this.TXT_PublicWarn.Size = new System.Drawing.Size(318, 20);
            this.TXT_PublicWarn.TabIndex = 17;
            this.TXT_PublicWarn.TextChanged += new System.EventHandler(this.TXT_PublicWarn_TextChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 220);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(131, 13);
            this.label8.TabIndex = 16;
            this.label8.Text = "Public Warning Message: ";
            // 
            // TXT_PrivateWarn
            // 
            this.TXT_PrivateWarn.Location = new System.Drawing.Point(147, 191);
            this.TXT_PrivateWarn.Name = "TXT_PrivateWarn";
            this.TXT_PrivateWarn.Size = new System.Drawing.Size(318, 20);
            this.TXT_PrivateWarn.TabIndex = 15;
            this.TXT_PrivateWarn.TextChanged += new System.EventHandler(this.TXT_PrivateWarn_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 194);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(135, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Private Warning Message: ";
            // 
            // RAD_Ban
            // 
            this.RAD_Ban.AutoSize = true;
            this.RAD_Ban.Location = new System.Drawing.Point(117, 280);
            this.RAD_Ban.Name = "RAD_Ban";
            this.RAD_Ban.Size = new System.Drawing.Size(44, 17);
            this.RAD_Ban.TabIndex = 13;
            this.RAD_Ban.Text = "Ban";
            this.RAD_Ban.UseVisualStyleBackColor = true;
            this.RAD_Ban.CheckedChanged += new System.EventHandler(this.RAD_Ban_CheckedChanged);
            // 
            // RAD_Kick
            // 
            this.RAD_Kick.AutoSize = true;
            this.RAD_Kick.Location = new System.Drawing.Point(65, 280);
            this.RAD_Kick.Name = "RAD_Kick";
            this.RAD_Kick.Size = new System.Drawing.Size(46, 17);
            this.RAD_Kick.TabIndex = 12;
            this.RAD_Kick.Text = "Kick";
            this.RAD_Kick.UseVisualStyleBackColor = true;
            this.RAD_Kick.CheckedChanged += new System.EventHandler(this.RAD_Kick_CheckedChanged);
            // 
            // RAD_None
            // 
            this.RAD_None.AutoSize = true;
            this.RAD_None.Checked = true;
            this.RAD_None.Location = new System.Drawing.Point(8, 280);
            this.RAD_None.Name = "RAD_None";
            this.RAD_None.Size = new System.Drawing.Size(51, 17);
            this.RAD_None.TabIndex = 11;
            this.RAD_None.TabStop = true;
            this.RAD_None.Text = "None";
            this.RAD_None.UseVisualStyleBackColor = true;
            this.RAD_None.CheckedChanged += new System.EventHandler(this.RAD_None_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(5, 264);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(76, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Punishment:";
            // 
            // CHK_Admin
            // 
            this.CHK_Admin.AutoSize = true;
            this.CHK_Admin.Location = new System.Drawing.Point(263, 41);
            this.CHK_Admin.Name = "CHK_Admin";
            this.CHK_Admin.Size = new System.Drawing.Size(89, 17);
            this.CHK_Admin.TabIndex = 4;
            this.CHK_Admin.Text = "Server Admin";
            this.CHK_Admin.UseVisualStyleBackColor = true;
            this.CHK_Admin.CheckedChanged += new System.EventHandler(this.CHK_Admin_CheckedChanged);
            // 
            // CHK_SmallOwner
            // 
            this.CHK_SmallOwner.AutoSize = true;
            this.CHK_SmallOwner.Location = new System.Drawing.Point(179, 41);
            this.CHK_SmallOwner.Name = "CHK_SmallOwner";
            this.CHK_SmallOwner.Size = new System.Drawing.Size(78, 17);
            this.CHK_SmallOwner.TabIndex = 3;
            this.CHK_SmallOwner.Text = "Any Owner";
            this.CHK_SmallOwner.UseVisualStyleBackColor = true;
            this.CHK_SmallOwner.CheckedChanged += new System.EventHandler(this.CHK_SmallOwner_CheckedChanged);
            // 
            // CHK_BigOwner
            // 
            this.CHK_BigOwner.AutoSize = true;
            this.CHK_BigOwner.Location = new System.Drawing.Point(77, 41);
            this.CHK_BigOwner.Name = "CHK_BigOwner";
            this.CHK_BigOwner.Size = new System.Drawing.Size(96, 17);
            this.CHK_BigOwner.TabIndex = 2;
            this.CHK_BigOwner.Text = "Majority Owner";
            this.CHK_BigOwner.UseVisualStyleBackColor = true;
            this.CHK_BigOwner.CheckedChanged += new System.EventHandler(this.CHK_BigOwner_CheckedChanged);
            // 
            // CHK_Anyone
            // 
            this.CHK_Anyone.AutoSize = true;
            this.CHK_Anyone.Location = new System.Drawing.Point(9, 41);
            this.CHK_Anyone.Name = "CHK_Anyone";
            this.CHK_Anyone.Size = new System.Drawing.Size(62, 17);
            this.CHK_Anyone.TabIndex = 1;
            this.CHK_Anyone.Text = "Anyone";
            this.CHK_Anyone.UseVisualStyleBackColor = true;
            this.CHK_Anyone.CheckedChanged += new System.EventHandler(this.CHK_Anyone_CheckedChanged);
            // 
            // LBL_ModeDesc
            // 
            this.LBL_ModeDesc.AutoSize = true;
            this.LBL_ModeDesc.Location = new System.Drawing.Point(176, 8);
            this.LBL_ModeDesc.Name = "LBL_ModeDesc";
            this.LBL_ModeDesc.Size = new System.Drawing.Size(0, 13);
            this.LBL_ModeDesc.TabIndex = 0;
            // 
            // PNL_ItemBasic
            // 
            this.PNL_ItemBasic.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PNL_ItemBasic.Controls.Add(this.CHK_LogOnly);
            this.PNL_ItemBasic.Controls.Add(this.LBL_GridName);
            this.PNL_ItemBasic.Controls.Add(this.BTN_FindEntity);
            this.PNL_ItemBasic.Controls.Add(this.label2);
            this.PNL_ItemBasic.Controls.Add(this.label1);
            this.PNL_ItemBasic.Controls.Add(this.TXT_EntityId);
            this.PNL_ItemBasic.Controls.Add(this.CHK_Enabled);
            this.PNL_ItemBasic.Controls.Add(this.CHK_Damage);
            this.PNL_ItemBasic.Location = new System.Drawing.Point(3, 6);
            this.PNL_ItemBasic.Name = "PNL_ItemBasic";
            this.PNL_ItemBasic.Size = new System.Drawing.Size(478, 71);
            this.PNL_ItemBasic.TabIndex = 3;
            // 
            // CHK_LogOnly
            // 
            this.CHK_LogOnly.AutoSize = true;
            this.CHK_LogOnly.Location = new System.Drawing.Point(186, 45);
            this.CHK_LogOnly.Name = "CHK_LogOnly";
            this.CHK_LogOnly.Size = new System.Drawing.Size(68, 17);
            this.CHK_LogOnly.TabIndex = 6;
            this.CHK_LogOnly.Text = "Log Only";
            this.CHK_LogOnly.UseVisualStyleBackColor = true;
            this.CHK_LogOnly.CheckedChanged += new System.EventHandler(this.CHK_LogOnly_CheckedChanged);
            // 
            // LBL_GridName
            // 
            this.LBL_GridName.AutoSize = true;
            this.LBL_GridName.Location = new System.Drawing.Point(276, 23);
            this.LBL_GridName.Name = "LBL_GridName";
            this.LBL_GridName.Size = new System.Drawing.Size(50, 13);
            this.LBL_GridName.TabIndex = 5;
            this.LBL_GridName.Text = "gridname";
            // 
            // BTN_FindEntity
            // 
            this.BTN_FindEntity.Location = new System.Drawing.Point(189, 19);
            this.BTN_FindEntity.Name = "BTN_FindEntity";
            this.BTN_FindEntity.Size = new System.Drawing.Size(81, 20);
            this.BTN_FindEntity.TabIndex = 4;
            this.BTN_FindEntity.Text = "Find entity";
            this.BTN_FindEntity.UseVisualStyleBackColor = true;
            this.BTN_FindEntity.Click += new System.EventHandler(this.BTN_FindEntity_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "EntityId:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Basic:";
            // 
            // TXT_EntityId
            // 
            this.TXT_EntityId.Location = new System.Drawing.Point(51, 19);
            this.TXT_EntityId.Name = "TXT_EntityId";
            this.TXT_EntityId.Size = new System.Drawing.Size(132, 20);
            this.TXT_EntityId.TabIndex = 2;
            this.TXT_EntityId.TextChanged += new System.EventHandler(this.TXT_EntityId_TextChanged);
            // 
            // CHK_Enabled
            // 
            this.CHK_Enabled.AutoSize = true;
            this.CHK_Enabled.Checked = true;
            this.CHK_Enabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CHK_Enabled.Location = new System.Drawing.Point(6, 45);
            this.CHK_Enabled.Name = "CHK_Enabled";
            this.CHK_Enabled.Size = new System.Drawing.Size(65, 17);
            this.CHK_Enabled.TabIndex = 0;
            this.CHK_Enabled.Text = "Enabled";
            this.CHK_Enabled.UseVisualStyleBackColor = true;
            this.CHK_Enabled.CheckedChanged += new System.EventHandler(this.CHK_Enabled_CheckedChanged);
            // 
            // CHK_Damage
            // 
            this.CHK_Damage.AutoSize = true;
            this.CHK_Damage.Checked = true;
            this.CHK_Damage.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CHK_Damage.Location = new System.Drawing.Point(80, 45);
            this.CHK_Damage.Name = "CHK_Damage";
            this.CHK_Damage.Size = new System.Drawing.Size(103, 17);
            this.CHK_Damage.TabIndex = 1;
            this.CHK_Damage.Text = "Damage Protect";
            this.CHK_Damage.UseVisualStyleBackColor = true;
            this.CHK_Damage.CheckedChanged += new System.EventHandler(this.CHK_Damage_CheckedChanged);
            // 
            // ProtectionEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(691, 393);
            this.Controls.Add(this.splitContainer1);
            this.Name = "ProtectionEditor";
            this.Text = "ProtectionEditor";
            this.Load += new System.EventHandler(this.ProtectionEditor_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.PNL_ItemDetails.ResumeLayout(false);
            this.PNL_ItemDetails.PerformLayout();
            this.PNL_ItemBasic.ResumeLayout(false);
            this.PNL_ItemBasic.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button BTN_RemoveItem;
        private System.Windows.Forms.Button BTN_AddItem;
        private System.Windows.Forms.ListBox LST_Entries;
        private System.Windows.Forms.TextBox TXT_EntityId;
        private System.Windows.Forms.CheckBox CHK_Damage;
        private System.Windows.Forms.CheckBox CHK_Enabled;
        private System.Windows.Forms.Panel PNL_ItemBasic;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button BTN_SaveItem;
        private System.Windows.Forms.Panel PNL_ItemDetails;
        private System.Windows.Forms.CheckBox CHK_Admin;
        private System.Windows.Forms.CheckBox CHK_SmallOwner;
        private System.Windows.Forms.CheckBox CHK_BigOwner;
        private System.Windows.Forms.CheckBox CHK_Anyone;
        private System.Windows.Forms.Label LBL_ModeDesc;
        private System.Windows.Forms.Label LBL_GridName;
        private System.Windows.Forms.Button BTN_FindEntity;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.RadioButton RAD_Speed;
        private System.Windows.Forms.ComboBox CMB_Mode;
        private System.Windows.Forms.TextBox TXT_PublicWarn;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox TXT_PrivateWarn;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.RadioButton RAD_Ban;
        private System.Windows.Forms.RadioButton RAD_Kick;
        private System.Windows.Forms.RadioButton RAD_None;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox CHK_LogOnly;
        private System.Windows.Forms.CheckBox CHK_SendGPS;
        private System.Windows.Forms.TextBox TXT_SpeedTime;
        private System.Windows.Forms.TextBox TXT_SpeedVal;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox LST_Factions;
        private System.Windows.Forms.Button BTN_GroupId;
        private System.Windows.Forms.Button BTN_SteamId;
        private System.Windows.Forms.CheckBox CHK_Faction;
    }
}