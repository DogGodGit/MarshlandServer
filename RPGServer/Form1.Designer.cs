namespace MainServer
{
	partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.pn2Dmap = new MainServer.DoubleBufferPanel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.chIngameMap = new System.Windows.Forms.CheckBox();
            this.chkShowCollisionMap = new System.Windows.Forms.CheckBox();
            this.chShowAIMap = new System.Windows.Forms.CheckBox();
            this.chkRemoveAllMobs = new System.Windows.Forms.CheckBox();
            this.btnZoomOut = new System.Windows.Forms.Button();
            this.btnZoomIn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.cbZone = new System.Windows.Forms.ComboBox();
            this.tabPlayerList = new System.Windows.Forms.TabPage();
            this.lvPlayerList = new System.Windows.Forms.ListView();
            this.chAccID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chPlayerName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ConnectionNo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chCharID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chCharName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chZone = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLevel = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.PosX = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.PosZ = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chRTT = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLastHeard = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chStored = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chWithheld = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnCreatePlayers = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSendAlert = new System.Windows.Forms.Button();
            this.btnRelocate = new System.Windows.Forms.Button();
            this.btnKick = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtMaxDequeue = new System.Windows.Forms.TextBox();
            this.txtUpdateTime = new System.Windows.Forms.TextBox();
            this.lblMaxUsers = new System.Windows.Forms.Label();
            this.tbMaxUsers = new System.Windows.Forms.TextBox();
            this.tbCurrentUsers = new System.Windows.Forms.TextBox();
            this.lblCurrentUsers = new System.Windows.Forms.Label();
            this.tabMessages = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.lblLongestMessageProcess = new System.Windows.Forms.Label();
            this.labelActiveTask = new System.Windows.Forms.Label();
            this.labelAHUpdateTime = new System.Windows.Forms.Label();
            this.labelAHListingUpdateTime = new System.Windows.Forms.Label();
            this.labelAHExpiringListings = new System.Windows.Forms.Label();
            this.labelAHActiveListings = new System.Windows.Forms.Label();
            this.labelMaxUpdateTime = new System.Windows.Forms.Label();
            this.labelMaxMessageProcess = new System.Windows.Forms.Label();
            this.labelAvrMessageProcess = new System.Windows.Forms.Label();
            this.labelAvrUpdateLoop = new System.Windows.Forms.Label();
            this.labelUpdateLoops = new System.Windows.Forms.Label();
            this.labelPendingSyncStatements = new System.Windows.Forms.Label();
            this.labelInventoryPoolCount = new System.Windows.Forms.Label();
            this.labelPendingBGTasks = new System.Windows.Forms.Label();
            this.labelProcessedMessages = new System.Windows.Forms.Label();
            this.labelOutstandingIncMessages = new System.Windows.Forms.Label();
            this.labelOutgoingPackets = new System.Windows.Forms.Label();
            this.labelReceivedPackets = new System.Windows.Forms.Label();
            this.labelPlayerInfoRefresh = new System.Windows.Forms.Label();
            this.labelNonUpdating = new System.Windows.Forms.Label();
            this.labelMainUpdateTime = new System.Windows.Forms.Label();
            this.labelServerMsgWaitTime = new System.Windows.Forms.Label();
            this.lblMessageUpdateTime = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.groupBoxAdminList = new System.Windows.Forms.GroupBox();
            this.buttonReloadAdminList = new System.Windows.Forms.Button();
            this.checkBoxDebug = new System.Windows.Forms.CheckBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.checkBoxPerformanceLogging = new System.Windows.Forms.CheckBox();
            this.DailyRewards = new System.Windows.Forms.GroupBox();
            this.RewardsEnabled = new System.Windows.Forms.CheckBox();
            this.ReloadRewards = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.AHRadioButtonOffline = new System.Windows.Forms.RadioButton();
            this.resetDurationsCheckBox = new System.Windows.Forms.CheckBox();
            this.AHRadioButtonSafeMode = new System.Windows.Forms.RadioButton();
            this.AHRadioButtonOnline = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.buttonSpecialOffers = new System.Windows.Forms.Button();
            this.btnReinitialiseThirdPartyOptions = new System.Windows.Forms.Button();
            this.btnLogOptions = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.toolTipReadSleepMillis = new System.Windows.Forms.ToolTip(this.components);
            this.buttonReloadLocalisation = new System.Windows.Forms.Button();
            this.tabPage1.SuspendLayout();
            this.panel4.SuspendLayout();
            this.tabPlayerList.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabMessages.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBoxAdminList.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.DailyRewards.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPage1
            // 
            this.tabPage1.AutoScroll = true;
            this.tabPage1.Controls.Add(this.pn2Dmap);
            this.tabPage1.Controls.Add(this.panel4);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(862, 583);
            this.tabPage1.TabIndex = 3;
            this.tabPage1.Text = "2Dmap";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // pn2Dmap
            // 
            this.pn2Dmap.BackColor = System.Drawing.Color.White;
            this.pn2Dmap.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pn2Dmap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pn2Dmap.Location = new System.Drawing.Point(0, 37);
            this.pn2Dmap.Name = "pn2Dmap";
            this.pn2Dmap.Size = new System.Drawing.Size(862, 546);
            this.pn2Dmap.TabIndex = 4;
            this.pn2Dmap.Paint += new System.Windows.Forms.PaintEventHandler(this.pn2Dmap_Paint);
            this.pn2Dmap.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pn2Dmap_MouseDown);
            this.pn2Dmap.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pn2Dmap_MouseMove);
            this.pn2Dmap.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pn2Dmap_MouseUp);
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.chIngameMap);
            this.panel4.Controls.Add(this.chkShowCollisionMap);
            this.panel4.Controls.Add(this.chShowAIMap);
            this.panel4.Controls.Add(this.chkRemoveAllMobs);
            this.panel4.Controls.Add(this.btnZoomOut);
            this.panel4.Controls.Add(this.btnZoomIn);
            this.panel4.Controls.Add(this.label3);
            this.panel4.Controls.Add(this.cbZone);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel4.Location = new System.Drawing.Point(0, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(862, 37);
            this.panel4.TabIndex = 3;
            // 
            // chIngameMap
            // 
            this.chIngameMap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chIngameMap.AutoSize = true;
            this.chIngameMap.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chIngameMap.Location = new System.Drawing.Point(354, 10);
            this.chIngameMap.Name = "chIngameMap";
            this.chIngameMap.Size = new System.Drawing.Size(85, 17);
            this.chIngameMap.TabIndex = 8;
            this.chIngameMap.Text = "Ingame Map";
            this.chIngameMap.UseVisualStyleBackColor = true;
            this.chIngameMap.CheckedChanged += new System.EventHandler(this.ckIngameMap_CheckedChanged);
            // 
            // chkShowCollisionMap
            // 
            this.chkShowCollisionMap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkShowCollisionMap.AutoSize = true;
            this.chkShowCollisionMap.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkShowCollisionMap.Location = new System.Drawing.Point(445, 10);
            this.chkShowCollisionMap.Name = "chkShowCollisionMap";
            this.chkShowCollisionMap.Size = new System.Drawing.Size(118, 17);
            this.chkShowCollisionMap.TabIndex = 8;
            this.chkShowCollisionMap.Text = "Show Collision Map";
            this.chkShowCollisionMap.UseVisualStyleBackColor = true;
            // 
            // chShowAIMap
            // 
            this.chShowAIMap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chShowAIMap.AutoSize = true;
            this.chShowAIMap.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chShowAIMap.Location = new System.Drawing.Point(569, 10);
            this.chShowAIMap.Name = "chShowAIMap";
            this.chShowAIMap.Size = new System.Drawing.Size(66, 17);
            this.chShowAIMap.TabIndex = 7;
            this.chShowAIMap.Text = "Show AI";
            this.chShowAIMap.UseVisualStyleBackColor = true;
            // 
            // chkRemoveAllMobs
            // 
            this.chkRemoveAllMobs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkRemoveAllMobs.AutoSize = true;
            this.chkRemoveAllMobs.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkRemoveAllMobs.Location = new System.Drawing.Point(641, 10);
            this.chkRemoveAllMobs.Name = "chkRemoveAllMobs";
            this.chkRemoveAllMobs.Size = new System.Drawing.Size(109, 17);
            this.chkRemoveAllMobs.TabIndex = 6;
            this.chkRemoveAllMobs.Text = "Remove All Mobs";
            this.chkRemoveAllMobs.UseVisualStyleBackColor = true;
            this.chkRemoveAllMobs.Click += new System.EventHandler(this.chkRemoveAllMobs_Click);
            // 
            // btnZoomOut
            // 
            this.btnZoomOut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnZoomOut.Enabled = false;
            this.btnZoomOut.Location = new System.Drawing.Point(831, 7);
            this.btnZoomOut.Name = "btnZoomOut";
            this.btnZoomOut.Size = new System.Drawing.Size(23, 24);
            this.btnZoomOut.TabIndex = 5;
            this.btnZoomOut.Text = "-";
            this.btnZoomOut.UseVisualStyleBackColor = true;
            this.btnZoomOut.Click += new System.EventHandler(this.btnZoomOut_Click);
            // 
            // btnZoomIn
            // 
            this.btnZoomIn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnZoomIn.Location = new System.Drawing.Point(781, 7);
            this.btnZoomIn.Name = "btnZoomIn";
            this.btnZoomIn.Size = new System.Drawing.Size(23, 24);
            this.btnZoomIn.TabIndex = 4;
            this.btnZoomIn.Text = "+";
            this.btnZoomIn.UseVisualStyleBackColor = true;
            this.btnZoomIn.Click += new System.EventHandler(this.btnZoomIn_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Zone";
            // 
            // cbZone
            // 
            this.cbZone.FormattingEnabled = true;
            this.cbZone.Items.AddRange(new object[] {
            "(select zone)"});
            this.cbZone.Location = new System.Drawing.Point(55, 10);
            this.cbZone.Name = "cbZone";
            this.cbZone.Size = new System.Drawing.Size(150, 21);
            this.cbZone.TabIndex = 1;
            this.cbZone.SelectedIndexChanged += new System.EventHandler(this.cbZone_SelectedIndexChanged);
            // 
            // tabPlayerList
            // 
            this.tabPlayerList.Controls.Add(this.lvPlayerList);
            this.tabPlayerList.Controls.Add(this.panel2);
            this.tabPlayerList.Location = new System.Drawing.Point(4, 22);
            this.tabPlayerList.Name = "tabPlayerList";
            this.tabPlayerList.Padding = new System.Windows.Forms.Padding(3);
            this.tabPlayerList.Size = new System.Drawing.Size(862, 583);
            this.tabPlayerList.TabIndex = 1;
            this.tabPlayerList.Text = "Active Players";
            this.tabPlayerList.UseVisualStyleBackColor = true;
            // 
            // lvPlayerList
            // 
            this.lvPlayerList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chAccID,
            this.chPlayerName,
            this.ConnectionNo,
            this.chCharID,
            this.chCharName,
            this.chZone,
            this.chLevel,
            this.PosX,
            this.PosZ,
            this.chRTT,
            this.chLastHeard,
            this.chStored,
            this.chWithheld});
            this.lvPlayerList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvPlayerList.FullRowSelect = true;
            this.lvPlayerList.GridLines = true;
            this.lvPlayerList.HideSelection = false;
            this.lvPlayerList.Location = new System.Drawing.Point(3, 44);
            this.lvPlayerList.Name = "lvPlayerList";
            this.lvPlayerList.Size = new System.Drawing.Size(856, 536);
            this.lvPlayerList.TabIndex = 1;
            this.lvPlayerList.UseCompatibleStateImageBehavior = false;
            this.lvPlayerList.View = System.Windows.Forms.View.Details;
            // 
            // chAccID
            // 
            this.chAccID.Text = "Acc ID";
            // 
            // chPlayerName
            // 
            this.chPlayerName.Text = "Player Name";
            this.chPlayerName.Width = 112;
            // 
            // ConnectionNo
            // 
            this.ConnectionNo.Text = "Conn No";
            // 
            // chCharID
            // 
            this.chCharID.Text = "Char ID";
            // 
            // chCharName
            // 
            this.chCharName.Text = "Character";
            this.chCharName.Width = 104;
            // 
            // chZone
            // 
            this.chZone.Text = "Zone";
            this.chZone.Width = 83;
            // 
            // chLevel
            // 
            this.chLevel.Text = "Level";
            this.chLevel.Width = 42;
            // 
            // PosX
            // 
            this.PosX.Text = "PosX";
            this.PosX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.PosX.Width = 48;
            // 
            // PosZ
            // 
            this.PosZ.Text = "Pos Z";
            this.PosZ.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // chRTT
            // 
            this.chRTT.Text = "RTT";
            // 
            // chLastHeard
            // 
            this.chLastHeard.Text = "Last Heard";
            // 
            // chStored
            // 
            this.chStored.Text = "Stored";
            // 
            // chWithheld
            // 
            this.chWithheld.Text = "Withheld";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnCreatePlayers);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.btnSendAlert);
            this.panel2.Controls.Add(this.btnRelocate);
            this.panel2.Controls.Add(this.btnKick);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.txtMaxDequeue);
            this.panel2.Controls.Add(this.txtUpdateTime);
            this.panel2.Controls.Add(this.lblMaxUsers);
            this.panel2.Controls.Add(this.tbMaxUsers);
            this.panel2.Controls.Add(this.tbCurrentUsers);
            this.panel2.Controls.Add(this.lblCurrentUsers);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(3, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(856, 41);
            this.panel2.TabIndex = 0;
            // 
            // btnCreatePlayers
            // 
            this.btnCreatePlayers.Location = new System.Drawing.Point(800, 7);
            this.btnCreatePlayers.Name = "btnCreatePlayers";
            this.btnCreatePlayers.Size = new System.Drawing.Size(75, 25);
            this.btnCreatePlayers.TabIndex = 14;
            this.btnCreatePlayers.Text = "Create";
            this.btnCreatePlayers.UseVisualStyleBackColor = true;
            this.btnCreatePlayers.Click += new System.EventHandler(this.btnCreatePlayers_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(187, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "Update";
            // 
            // btnSendAlert
            // 
            this.btnSendAlert.Location = new System.Drawing.Point(500, 7);
            this.btnSendAlert.Name = "btnSendAlert";
            this.btnSendAlert.Size = new System.Drawing.Size(85, 25);
            this.btnSendAlert.TabIndex = 12;
            this.btnSendAlert.Text = "Server Alert";
            this.btnSendAlert.UseVisualStyleBackColor = true;
            this.btnSendAlert.Click += new System.EventHandler(this.btnSendAlert_Click);
            // 
            // btnRelocate
            // 
            this.btnRelocate.Location = new System.Drawing.Point(700, 7);
            this.btnRelocate.Name = "btnRelocate";
            this.btnRelocate.Size = new System.Drawing.Size(75, 25);
            this.btnRelocate.TabIndex = 11;
            this.btnRelocate.Text = "Relocate";
            this.btnRelocate.UseVisualStyleBackColor = true;
            this.btnRelocate.Click += new System.EventHandler(this.btnRelocate_Click);
            // 
            // btnKick
            // 
            this.btnKick.Location = new System.Drawing.Point(600, 7);
            this.btnKick.Name = "btnKick";
            this.btnKick.Size = new System.Drawing.Size(75, 25);
            this.btnKick.TabIndex = 10;
            this.btnKick.Text = "Kick";
            this.btnKick.UseVisualStyleBackColor = true;
            this.btnKick.Click += new System.EventHandler(this.btnKick_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(277, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Max Dequeue";
            // 
            // txtMaxDequeue
            // 
            this.txtMaxDequeue.Location = new System.Drawing.Point(357, 10);
            this.txtMaxDequeue.Name = "txtMaxDequeue";
            this.txtMaxDequeue.Size = new System.Drawing.Size(49, 20);
            this.txtMaxDequeue.TabIndex = 7;
            this.txtMaxDequeue.Text = "1000";
            this.txtMaxDequeue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtMaxDequeue.TextChanged += new System.EventHandler(this.txtMaxDequeue_TextChanged);
            // 
            // txtUpdateTime
            // 
            this.txtUpdateTime.Location = new System.Drawing.Point(233, 10);
            this.txtUpdateTime.Name = "txtUpdateTime";
            this.txtUpdateTime.Size = new System.Drawing.Size(38, 20);
            this.txtUpdateTime.TabIndex = 5;
            this.txtUpdateTime.Text = "50";
            this.txtUpdateTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtUpdateTime.TextChanged += new System.EventHandler(this.txtUpdateTime_TextChanged);
            // 
            // lblMaxUsers
            // 
            this.lblMaxUsers.AutoSize = true;
            this.lblMaxUsers.Location = new System.Drawing.Point(103, 12);
            this.lblMaxUsers.Name = "lblMaxUsers";
            this.lblMaxUsers.Size = new System.Drawing.Size(27, 13);
            this.lblMaxUsers.TabIndex = 3;
            this.lblMaxUsers.Text = "Max";
            // 
            // tbMaxUsers
            // 
            this.tbMaxUsers.Location = new System.Drawing.Point(135, 10);
            this.tbMaxUsers.Name = "tbMaxUsers";
            this.tbMaxUsers.Size = new System.Drawing.Size(45, 20);
            this.tbMaxUsers.TabIndex = 2;
            this.tbMaxUsers.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tbMaxUsers.TextChanged += new System.EventHandler(this.tbMaxUsers_TextChanged);
            // 
            // tbCurrentUsers
            // 
            this.tbCurrentUsers.BackColor = System.Drawing.SystemColors.Window;
            this.tbCurrentUsers.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbCurrentUsers.Location = new System.Drawing.Point(48, 10);
            this.tbCurrentUsers.Name = "tbCurrentUsers";
            this.tbCurrentUsers.ReadOnly = true;
            this.tbCurrentUsers.Size = new System.Drawing.Size(49, 20);
            this.tbCurrentUsers.TabIndex = 1;
            this.tbCurrentUsers.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // lblCurrentUsers
            // 
            this.lblCurrentUsers.AutoSize = true;
            this.lblCurrentUsers.Location = new System.Drawing.Point(3, 12);
            this.lblCurrentUsers.Name = "lblCurrentUsers";
            this.lblCurrentUsers.Size = new System.Drawing.Size(41, 13);
            this.lblCurrentUsers.TabIndex = 0;
            this.lblCurrentUsers.Text = "Current";
            // 
            // tabMessages
            // 
            this.tabMessages.Controls.Add(this.splitContainer1);
            this.tabMessages.Controls.Add(this.panel1);
            this.tabMessages.Location = new System.Drawing.Point(4, 22);
            this.tabMessages.Name = "tabMessages";
            this.tabMessages.Padding = new System.Windows.Forms.Padding(3);
            this.tabMessages.Size = new System.Drawing.Size(862, 614);
            this.tabMessages.TabIndex = 0;
            this.tabMessages.Text = "Message Data";
            this.tabMessages.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(3, 129);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.richTextBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox3);
            this.splitContainer1.Size = new System.Drawing.Size(851, 477);
            this.splitContainer1.SplitterDistance = 562;
            this.splitContainer1.TabIndex = 16;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(5, 3);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(554, 471);
            this.richTextBox1.TabIndex = 3;
            this.richTextBox1.Text = "";
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.BackColor = System.Drawing.Color.Transparent;
            this.groupBox3.Controls.Add(this.lblLongestMessageProcess);
            this.groupBox3.Controls.Add(this.labelActiveTask);
            this.groupBox3.Controls.Add(this.labelAHUpdateTime);
            this.groupBox3.Controls.Add(this.labelAHListingUpdateTime);
            this.groupBox3.Controls.Add(this.labelAHExpiringListings);
            this.groupBox3.Controls.Add(this.labelAHActiveListings);
            this.groupBox3.Controls.Add(this.labelMaxUpdateTime);
            this.groupBox3.Controls.Add(this.labelMaxMessageProcess);
            this.groupBox3.Controls.Add(this.labelAvrMessageProcess);
            this.groupBox3.Controls.Add(this.labelAvrUpdateLoop);
            this.groupBox3.Controls.Add(this.labelUpdateLoops);
            this.groupBox3.Controls.Add(this.labelPendingSyncStatements);
            this.groupBox3.Controls.Add(this.labelInventoryPoolCount);
            this.groupBox3.Controls.Add(this.labelPendingBGTasks);
            this.groupBox3.Controls.Add(this.labelProcessedMessages);
            this.groupBox3.Controls.Add(this.labelOutstandingIncMessages);
            this.groupBox3.Controls.Add(this.labelOutgoingPackets);
            this.groupBox3.Controls.Add(this.labelReceivedPackets);
            this.groupBox3.Controls.Add(this.labelPlayerInfoRefresh);
            this.groupBox3.Controls.Add(this.labelNonUpdating);
            this.groupBox3.Controls.Add(this.labelMainUpdateTime);
            this.groupBox3.Controls.Add(this.labelServerMsgWaitTime);
            this.groupBox3.Controls.Add(this.lblMessageUpdateTime);
            this.groupBox3.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox3.Location = new System.Drawing.Point(5, 3);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(277, 471);
            this.groupBox3.TabIndex = 15;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Server Update Message";
            // 
            // lblLongestMessageProcess
            // 
            this.lblLongestMessageProcess.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLongestMessageProcess.AutoSize = true;
            this.lblLongestMessageProcess.Location = new System.Drawing.Point(6, 424);
            this.lblLongestMessageProcess.Name = "lblLongestMessageProcess";
            this.lblLongestMessageProcess.Size = new System.Drawing.Size(70, 14);
            this.lblLongestMessageProcess.TabIndex = 27;
            this.lblLongestMessageProcess.Text = "<pending>";
            // 
            // labelActiveTask
            // 
            this.labelActiveTask.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelActiveTask.AutoSize = true;
            this.labelActiveTask.Location = new System.Drawing.Point(6, 400);
            this.labelActiveTask.Name = "labelActiveTask";
            this.labelActiveTask.Size = new System.Drawing.Size(70, 14);
            this.labelActiveTask.TabIndex = 26;
            this.labelActiveTask.Text = "<pending>";
            // 
            // labelAHUpdateTime
            // 
            this.labelAHUpdateTime.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAHUpdateTime.AutoSize = true;
            this.labelAHUpdateTime.Location = new System.Drawing.Point(6, 376);
            this.labelAHUpdateTime.Name = "labelAHUpdateTime";
            this.labelAHUpdateTime.Size = new System.Drawing.Size(70, 14);
            this.labelAHUpdateTime.TabIndex = 25;
            this.labelAHUpdateTime.Text = "<pending>";
            // 
            // labelAHListingUpdateTime
            // 
            this.labelAHListingUpdateTime.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAHListingUpdateTime.AutoSize = true;
            this.labelAHListingUpdateTime.Location = new System.Drawing.Point(6, 362);
            this.labelAHListingUpdateTime.Name = "labelAHListingUpdateTime";
            this.labelAHListingUpdateTime.Size = new System.Drawing.Size(70, 14);
            this.labelAHListingUpdateTime.TabIndex = 24;
            this.labelAHListingUpdateTime.Text = "<pending>";
            // 
            // labelAHExpiringListings
            // 
            this.labelAHExpiringListings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAHExpiringListings.AutoSize = true;
            this.labelAHExpiringListings.Location = new System.Drawing.Point(6, 348);
            this.labelAHExpiringListings.Name = "labelAHExpiringListings";
            this.labelAHExpiringListings.Size = new System.Drawing.Size(70, 14);
            this.labelAHExpiringListings.TabIndex = 23;
            this.labelAHExpiringListings.Text = "<pending>";
            // 
            // labelAHActiveListings
            // 
            this.labelAHActiveListings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAHActiveListings.AutoSize = true;
            this.labelAHActiveListings.Location = new System.Drawing.Point(6, 334);
            this.labelAHActiveListings.Name = "labelAHActiveListings";
            this.labelAHActiveListings.Size = new System.Drawing.Size(70, 14);
            this.labelAHActiveListings.TabIndex = 22;
            this.labelAHActiveListings.Text = "<pending>";
            // 
            // labelMaxUpdateTime
            // 
            this.labelMaxUpdateTime.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMaxUpdateTime.AutoSize = true;
            this.labelMaxUpdateTime.Location = new System.Drawing.Point(6, 231);
            this.labelMaxUpdateTime.Name = "labelMaxUpdateTime";
            this.labelMaxUpdateTime.Size = new System.Drawing.Size(70, 14);
            this.labelMaxUpdateTime.TabIndex = 21;
            this.labelMaxUpdateTime.Text = "<pending>";
            // 
            // labelMaxMessageProcess
            // 
            this.labelMaxMessageProcess.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMaxMessageProcess.AutoSize = true;
            this.labelMaxMessageProcess.Location = new System.Drawing.Point(6, 271);
            this.labelMaxMessageProcess.Name = "labelMaxMessageProcess";
            this.labelMaxMessageProcess.Size = new System.Drawing.Size(70, 14);
            this.labelMaxMessageProcess.TabIndex = 20;
            this.labelMaxMessageProcess.Text = "<pending>";
            // 
            // labelAvrMessageProcess
            // 
            this.labelAvrMessageProcess.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAvrMessageProcess.AutoSize = true;
            this.labelAvrMessageProcess.Location = new System.Drawing.Point(6, 257);
            this.labelAvrMessageProcess.Name = "labelAvrMessageProcess";
            this.labelAvrMessageProcess.Size = new System.Drawing.Size(70, 14);
            this.labelAvrMessageProcess.TabIndex = 19;
            this.labelAvrMessageProcess.Text = "<pending>";
            // 
            // labelAvrUpdateLoop
            // 
            this.labelAvrUpdateLoop.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAvrUpdateLoop.AutoSize = true;
            this.labelAvrUpdateLoop.Location = new System.Drawing.Point(6, 216);
            this.labelAvrUpdateLoop.Name = "labelAvrUpdateLoop";
            this.labelAvrUpdateLoop.Size = new System.Drawing.Size(70, 14);
            this.labelAvrUpdateLoop.TabIndex = 18;
            this.labelAvrUpdateLoop.Text = "<pending>";
            // 
            // labelUpdateLoops
            // 
            this.labelUpdateLoops.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelUpdateLoops.AutoSize = true;
            this.labelUpdateLoops.Location = new System.Drawing.Point(6, 202);
            this.labelUpdateLoops.Name = "labelUpdateLoops";
            this.labelUpdateLoops.Size = new System.Drawing.Size(70, 14);
            this.labelUpdateLoops.TabIndex = 17;
            this.labelUpdateLoops.Text = "<pending>";
            // 
            // labelPendingSyncStatements
            // 
            this.labelPendingSyncStatements.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPendingSyncStatements.AutoSize = true;
            this.labelPendingSyncStatements.Location = new System.Drawing.Point(6, 178);
            this.labelPendingSyncStatements.Name = "labelPendingSyncStatements";
            this.labelPendingSyncStatements.Size = new System.Drawing.Size(70, 14);
            this.labelPendingSyncStatements.TabIndex = 16;
            this.labelPendingSyncStatements.Text = "<pending>";
            // 
            // labelInventoryPoolCount
            // 
            this.labelInventoryPoolCount.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelInventoryPoolCount.AutoSize = true;
            this.labelInventoryPoolCount.Location = new System.Drawing.Point(6, 297);
            this.labelInventoryPoolCount.Name = "labelInventoryPoolCount";
            this.labelInventoryPoolCount.Size = new System.Drawing.Size(70, 14);
            this.labelInventoryPoolCount.TabIndex = 15;
            this.labelInventoryPoolCount.Text = "<pending>";
            // 
            // labelPendingBGTasks
            // 
            this.labelPendingBGTasks.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPendingBGTasks.AutoSize = true;
            this.labelPendingBGTasks.Location = new System.Drawing.Point(6, 164);
            this.labelPendingBGTasks.Name = "labelPendingBGTasks";
            this.labelPendingBGTasks.Size = new System.Drawing.Size(70, 14);
            this.labelPendingBGTasks.TabIndex = 14;
            this.labelPendingBGTasks.Text = "<pending>";
            // 
            // labelProcessedMessages
            // 
            this.labelProcessedMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelProcessedMessages.AutoSize = true;
            this.labelProcessedMessages.Location = new System.Drawing.Point(6, 136);
            this.labelProcessedMessages.Name = "labelProcessedMessages";
            this.labelProcessedMessages.Size = new System.Drawing.Size(70, 14);
            this.labelProcessedMessages.TabIndex = 13;
            this.labelProcessedMessages.Text = "<pending>";
            // 
            // labelOutstandingIncMessages
            // 
            this.labelOutstandingIncMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelOutstandingIncMessages.AutoSize = true;
            this.labelOutstandingIncMessages.Location = new System.Drawing.Point(6, 122);
            this.labelOutstandingIncMessages.Name = "labelOutstandingIncMessages";
            this.labelOutstandingIncMessages.Size = new System.Drawing.Size(70, 14);
            this.labelOutstandingIncMessages.TabIndex = 12;
            this.labelOutstandingIncMessages.Text = "<pending>";
            // 
            // labelOutgoingPackets
            // 
            this.labelOutgoingPackets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelOutgoingPackets.AutoSize = true;
            this.labelOutgoingPackets.Location = new System.Drawing.Point(6, 108);
            this.labelOutgoingPackets.Name = "labelOutgoingPackets";
            this.labelOutgoingPackets.Size = new System.Drawing.Size(70, 14);
            this.labelOutgoingPackets.TabIndex = 11;
            this.labelOutgoingPackets.Text = "<pending>";
            // 
            // labelReceivedPackets
            // 
            this.labelReceivedPackets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelReceivedPackets.AutoSize = true;
            this.labelReceivedPackets.Location = new System.Drawing.Point(6, 94);
            this.labelReceivedPackets.Name = "labelReceivedPackets";
            this.labelReceivedPackets.Size = new System.Drawing.Size(70, 14);
            this.labelReceivedPackets.TabIndex = 10;
            this.labelReceivedPackets.Text = "<pending>";
            // 
            // labelPlayerInfoRefresh
            // 
            this.labelPlayerInfoRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPlayerInfoRefresh.AutoSize = true;
            this.labelPlayerInfoRefresh.Location = new System.Drawing.Point(6, 311);
            this.labelPlayerInfoRefresh.Name = "labelPlayerInfoRefresh";
            this.labelPlayerInfoRefresh.Size = new System.Drawing.Size(70, 14);
            this.labelPlayerInfoRefresh.TabIndex = 9;
            this.labelPlayerInfoRefresh.Text = "<pending>";
            // 
            // labelNonUpdating
            // 
            this.labelNonUpdating.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNonUpdating.AutoSize = true;
            this.labelNonUpdating.Location = new System.Drawing.Point(6, 65);
            this.labelNonUpdating.Name = "labelNonUpdating";
            this.labelNonUpdating.Size = new System.Drawing.Size(70, 14);
            this.labelNonUpdating.TabIndex = 8;
            this.labelNonUpdating.Text = "<pending>";
            // 
            // labelMainUpdateTime
            // 
            this.labelMainUpdateTime.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMainUpdateTime.AutoSize = true;
            this.labelMainUpdateTime.Location = new System.Drawing.Point(6, 51);
            this.labelMainUpdateTime.Name = "labelMainUpdateTime";
            this.labelMainUpdateTime.Size = new System.Drawing.Size(70, 14);
            this.labelMainUpdateTime.TabIndex = 7;
            this.labelMainUpdateTime.Text = "<pending>";
            // 
            // labelServerMsgWaitTime
            // 
            this.labelServerMsgWaitTime.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelServerMsgWaitTime.AutoSize = true;
            this.labelServerMsgWaitTime.Location = new System.Drawing.Point(6, 37);
            this.labelServerMsgWaitTime.Name = "labelServerMsgWaitTime";
            this.labelServerMsgWaitTime.Size = new System.Drawing.Size(70, 14);
            this.labelServerMsgWaitTime.TabIndex = 6;
            this.labelServerMsgWaitTime.Text = "<pending>";
            // 
            // lblMessageUpdateTime
            // 
            this.lblMessageUpdateTime.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMessageUpdateTime.AutoSize = true;
            this.lblMessageUpdateTime.Location = new System.Drawing.Point(6, 23);
            this.lblMessageUpdateTime.Name = "lblMessageUpdateTime";
            this.lblMessageUpdateTime.Size = new System.Drawing.Size(70, 14);
            this.lblMessageUpdateTime.TabIndex = 5;
            this.lblMessageUpdateTime.Text = "<pending>";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel1.Controls.Add(this.groupBox5);
            this.panel1.Controls.Add(this.groupBoxAdminList);
            this.panel1.Controls.Add(this.groupBox4);
            this.panel1.Controls.Add(this.DailyRewards);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(856, 120);
            this.panel1.TabIndex = 0;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.comboBox1);
            this.groupBox5.Location = new System.Drawing.Point(422, 55);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(180, 62);
            this.groupBox5.TabIndex = 19;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Set Season";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(6, 16);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(158, 21);
            this.comboBox1.TabIndex = 0;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // groupBoxAdminList
            // 
            this.groupBoxAdminList.Controls.Add(this.buttonReloadAdminList);
            this.groupBoxAdminList.Location = new System.Drawing.Point(295, 55);
            this.groupBoxAdminList.Name = "groupBoxAdminList";
            this.groupBoxAdminList.Size = new System.Drawing.Size(121, 62);
            this.groupBoxAdminList.TabIndex = 18;
            this.groupBoxAdminList.TabStop = false;
            this.groupBoxAdminList.Text = "Admin";
            this.toolTipReadSleepMillis.SetToolTip(this.groupBoxAdminList, "Default: 20\r\nIncreasing will slow network read and gameplay updates, and will red" +
        "uce cpu usage.");
            // 
            // buttonReloadAdminList
            // 
            this.buttonReloadAdminList.Location = new System.Drawing.Point(6, 17);
            this.buttonReloadAdminList.Name = "buttonReloadAdminList";
            this.buttonReloadAdminList.Size = new System.Drawing.Size(109, 23);
            this.buttonReloadAdminList.TabIndex = 0;
            this.buttonReloadAdminList.Text = "ReloadAdminList";
            this.buttonReloadAdminList.UseVisualStyleBackColor = true;
            this.buttonReloadAdminList.Click += new System.EventHandler(this.buttonReloadAdminList_Click);
            // 
            // checkBoxDebug
            // 
            this.checkBoxDebug.AutoSize = true;
            this.checkBoxDebug.Location = new System.Drawing.Point(7, 38);
            this.checkBoxDebug.Name = "checkBoxDebug";
            this.checkBoxDebug.Size = new System.Drawing.Size(102, 17);
            this.checkBoxDebug.TabIndex = 18;
            this.checkBoxDebug.Text = "Equip Any Items";
            this.checkBoxDebug.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.checkBoxPerformanceLogging);
            this.groupBox4.Controls.Add(this.checkBoxDebug);
            this.groupBox4.Location = new System.Drawing.Point(148, 55);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(141, 62);
            this.groupBox4.TabIndex = 17;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Debug Options";
            // 
            // checkBoxPerformanceLogging
            // 
            this.checkBoxPerformanceLogging.AutoSize = true;
            this.checkBoxPerformanceLogging.Location = new System.Drawing.Point(7, 20);
            this.checkBoxPerformanceLogging.Name = "checkBoxPerformanceLogging";
            this.checkBoxPerformanceLogging.Size = new System.Drawing.Size(124, 17);
            this.checkBoxPerformanceLogging.TabIndex = 0;
            this.checkBoxPerformanceLogging.Text = "PerformanceLogging";
            this.checkBoxPerformanceLogging.UseVisualStyleBackColor = true;
            this.checkBoxPerformanceLogging.CheckedChanged += new System.EventHandler(this.checkBoxPerformanceLogging_CheckedChanged);
            // 
            // DailyRewards
            // 
            this.DailyRewards.Controls.Add(this.RewardsEnabled);
            this.DailyRewards.Controls.Add(this.ReloadRewards);
            this.DailyRewards.Location = new System.Drawing.Point(5, 55);
            this.DailyRewards.Name = "DailyRewards";
            this.DailyRewards.Size = new System.Drawing.Size(137, 62);
            this.DailyRewards.TabIndex = 16;
            this.DailyRewards.TabStop = false;
            this.DailyRewards.Text = "Daily Rewards";
            // 
            // RewardsEnabled
            // 
            this.RewardsEnabled.AutoSize = true;
            this.RewardsEnabled.Checked = true;
            this.RewardsEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.RewardsEnabled.Location = new System.Drawing.Point(8, 43);
            this.RewardsEnabled.Name = "RewardsEnabled";
            this.RewardsEnabled.Size = new System.Drawing.Size(110, 17);
            this.RewardsEnabled.TabIndex = 1;
            this.RewardsEnabled.Text = "Rewards Enabled";
            this.RewardsEnabled.UseVisualStyleBackColor = true;
            this.RewardsEnabled.CheckedChanged += new System.EventHandler(this.RewardsEnabled_CheckedChanged);
            // 
            // ReloadRewards
            // 
            this.ReloadRewards.Location = new System.Drawing.Point(6, 13);
            this.ReloadRewards.Name = "ReloadRewards";
            this.ReloadRewards.Size = new System.Drawing.Size(120, 25);
            this.ReloadRewards.TabIndex = 0;
            this.ReloadRewards.Text = "Reload Rewards";
            this.ReloadRewards.UseVisualStyleBackColor = true;
            this.ReloadRewards.Click += new System.EventHandler(this.ReloadRewards_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.AHRadioButtonOffline);
            this.groupBox2.Controls.Add(this.resetDurationsCheckBox);
            this.groupBox2.Controls.Add(this.AHRadioButtonSafeMode);
            this.groupBox2.Controls.Add(this.AHRadioButtonOnline);
            this.groupBox2.Location = new System.Drawing.Point(608, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(127, 110);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Auction House Status";
            // 
            // AHRadioButtonOffline
            // 
            this.AHRadioButtonOffline.AutoSize = true;
            this.AHRadioButtonOffline.Location = new System.Drawing.Point(6, 65);
            this.AHRadioButtonOffline.Name = "AHRadioButtonOffline";
            this.AHRadioButtonOffline.Size = new System.Drawing.Size(55, 17);
            this.AHRadioButtonOffline.TabIndex = 8;
            this.AHRadioButtonOffline.TabStop = true;
            this.AHRadioButtonOffline.Text = "Offline";
            this.AHRadioButtonOffline.UseVisualStyleBackColor = true;
            this.AHRadioButtonOffline.CheckedChanged += new System.EventHandler(this.AHRadioButtonOffline_CheckedChanged);
            // 
            // resetDurationsCheckBox
            // 
            this.resetDurationsCheckBox.AutoSize = true;
            this.resetDurationsCheckBox.Checked = true;
            this.resetDurationsCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.resetDurationsCheckBox.Location = new System.Drawing.Point(6, 88);
            this.resetDurationsCheckBox.Name = "resetDurationsCheckBox";
            this.resetDurationsCheckBox.Size = new System.Drawing.Size(107, 17);
            this.resetDurationsCheckBox.TabIndex = 12;
            this.resetDurationsCheckBox.Text = "Extend Durations";
            this.resetDurationsCheckBox.UseVisualStyleBackColor = true;
            // 
            // AHRadioButtonSafeMode
            // 
            this.AHRadioButtonSafeMode.AutoSize = true;
            this.AHRadioButtonSafeMode.Location = new System.Drawing.Point(6, 42);
            this.AHRadioButtonSafeMode.Name = "AHRadioButtonSafeMode";
            this.AHRadioButtonSafeMode.Size = new System.Drawing.Size(77, 17);
            this.AHRadioButtonSafeMode.TabIndex = 9;
            this.AHRadioButtonSafeMode.TabStop = true;
            this.AHRadioButtonSafeMode.Text = "Safe Mode";
            this.AHRadioButtonSafeMode.UseVisualStyleBackColor = true;
            this.AHRadioButtonSafeMode.CheckedChanged += new System.EventHandler(this.AHRadioButtonSafeMode_CheckedChanged);
            // 
            // AHRadioButtonOnline
            // 
            this.AHRadioButtonOnline.AutoSize = true;
            this.AHRadioButtonOnline.Location = new System.Drawing.Point(6, 19);
            this.AHRadioButtonOnline.Name = "AHRadioButtonOnline";
            this.AHRadioButtonOnline.Size = new System.Drawing.Size(55, 17);
            this.AHRadioButtonOnline.TabIndex = 10;
            this.AHRadioButtonOnline.TabStop = true;
            this.AHRadioButtonOnline.Text = "Online";
            this.AHRadioButtonOnline.UseVisualStyleBackColor = true;
            this.AHRadioButtonOnline.CheckedChanged += new System.EventHandler(this.AHRadioButtonOnline_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.groupBox1.Controls.Add(this.buttonReloadLocalisation);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.buttonSpecialOffers);
            this.groupBox1.Controls.Add(this.btnReinitialiseThirdPartyOptions);
            this.groupBox1.Controls.Add(this.btnLogOptions);
            this.groupBox1.Location = new System.Drawing.Point(5, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(597, 46);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Main";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(5, 14);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(89, 25);
            this.button1.TabIndex = 1;
            this.button1.Text = "Settings";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonSpecialOffers
            // 
            this.buttonSpecialOffers.Location = new System.Drawing.Point(99, 15);
            this.buttonSpecialOffers.Name = "buttonSpecialOffers";
            this.buttonSpecialOffers.Size = new System.Drawing.Size(120, 25);
            this.buttonSpecialOffers.TabIndex = 7;
            this.buttonSpecialOffers.Text = "Reload Special Offers";
            this.buttonSpecialOffers.UseVisualStyleBackColor = true;
            this.buttonSpecialOffers.Click += new System.EventHandler(this.buttonSpecialOffers_Click);
            // 
            // btnReinitialiseThirdPartyOptions
            // 
            this.btnReinitialiseThirdPartyOptions.Location = new System.Drawing.Point(225, 15);
            this.btnReinitialiseThirdPartyOptions.Name = "btnReinitialiseThirdPartyOptions";
            this.btnReinitialiseThirdPartyOptions.Size = new System.Drawing.Size(144, 25);
            this.btnReinitialiseThirdPartyOptions.TabIndex = 6;
            this.btnReinitialiseThirdPartyOptions.Text = "Reload AdSDK Options";
            this.btnReinitialiseThirdPartyOptions.UseVisualStyleBackColor = true;
            this.btnReinitialiseThirdPartyOptions.Click += new System.EventHandler(this.btnReinitialiseThirdPartyOptions_Click);
            // 
            // btnLogOptions
            // 
            this.btnLogOptions.Location = new System.Drawing.Point(375, 15);
            this.btnLogOptions.Name = "btnLogOptions";
            this.btnLogOptions.Size = new System.Drawing.Size(91, 25);
            this.btnLogOptions.TabIndex = 6;
            this.btnLogOptions.Text = "Log Options";
            this.btnLogOptions.UseVisualStyleBackColor = true;
            this.btnLogOptions.Click += new System.EventHandler(this.btnLogOptions_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabMessages);
            this.tabControl1.Controls.Add(this.tabPlayerList);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(870, 640);
            this.tabControl1.TabIndex = 2;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // toolTipReadSleepMillis
            // 
            this.toolTipReadSleepMillis.ToolTipTitle = "Network/Game Update sleep";
            // 
            // buttonReloadLocalisation
            // 
            this.buttonReloadLocalisation.Location = new System.Drawing.Point(473, 15);
            this.buttonReloadLocalisation.Name = "buttonReloadLocalisation";
            this.buttonReloadLocalisation.Size = new System.Drawing.Size(117, 25);
            this.buttonReloadLocalisation.TabIndex = 8;
            this.buttonReloadLocalisation.Text = "Reload Localisation";
            this.buttonReloadLocalisation.UseVisualStyleBackColor = true;
            this.buttonReloadLocalisation.Click += new System.EventHandler(this.buttonReloadLocalisation_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(870, 640);
            this.Controls.Add(this.tabControl1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(858, 615);
            this.Name = "Form1";
            this.Text = "Celtic Heroes Server";
            this.tabPage1.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.tabPlayerList.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.tabMessages.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBoxAdminList.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.DailyRewards.ResumeLayout(false);
            this.DailyRewards.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

        #endregion

        private System.Windows.Forms.TabPage tabPage1;
        public DoubleBufferPanel pn2Dmap;
        private System.Windows.Forms.Panel panel4;
        public System.Windows.Forms.CheckBox chIngameMap;
        public System.Windows.Forms.CheckBox chkShowCollisionMap;
        public System.Windows.Forms.CheckBox chShowAIMap;
        internal System.Windows.Forms.CheckBox chkRemoveAllMobs;
        private System.Windows.Forms.Button btnZoomOut;
        private System.Windows.Forms.Button btnZoomIn;
        private System.Windows.Forms.Label label3;
        internal System.Windows.Forms.ComboBox cbZone;
        private System.Windows.Forms.TabPage tabPlayerList;
        internal System.Windows.Forms.ListView lvPlayerList;
        private System.Windows.Forms.ColumnHeader chAccID;
        private System.Windows.Forms.ColumnHeader chPlayerName;
        private System.Windows.Forms.ColumnHeader ConnectionNo;
        private System.Windows.Forms.ColumnHeader chCharID;
        private System.Windows.Forms.ColumnHeader chCharName;
        private System.Windows.Forms.ColumnHeader chZone;
        private System.Windows.Forms.ColumnHeader chLevel;
        private System.Windows.Forms.ColumnHeader PosX;
        private System.Windows.Forms.ColumnHeader PosZ;
        private System.Windows.Forms.ColumnHeader chRTT;
        private System.Windows.Forms.ColumnHeader chLastHeard;
        private System.Windows.Forms.ColumnHeader chStored;
        private System.Windows.Forms.ColumnHeader chWithheld;
        private System.Windows.Forms.Panel panel2;
        internal System.Windows.Forms.Button btnCreatePlayers;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSendAlert;
        private System.Windows.Forms.Button btnRelocate;
        private System.Windows.Forms.Button btnKick;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtMaxDequeue;
        private System.Windows.Forms.TextBox txtUpdateTime;
        private System.Windows.Forms.Label lblMaxUsers;
        internal System.Windows.Forms.TextBox tbMaxUsers;
        internal System.Windows.Forms.TextBox tbCurrentUsers;
        private System.Windows.Forms.Label lblCurrentUsers;
        private System.Windows.Forms.TabPage tabMessages;
        public System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.GroupBox groupBox3;
        internal System.Windows.Forms.Label labelActiveTask;
        internal System.Windows.Forms.Label labelAHUpdateTime;
        internal System.Windows.Forms.Label labelAHListingUpdateTime;
        internal System.Windows.Forms.Label labelAHExpiringListings;
        internal System.Windows.Forms.Label labelAHActiveListings;
        internal System.Windows.Forms.Label labelMaxUpdateTime;
        internal System.Windows.Forms.Label labelMaxMessageProcess;
        internal System.Windows.Forms.Label labelAvrMessageProcess;
        internal System.Windows.Forms.Label labelAvrUpdateLoop;
        internal System.Windows.Forms.Label labelUpdateLoops;
        internal System.Windows.Forms.Label labelPendingSyncStatements;
        internal System.Windows.Forms.Label labelInventoryPoolCount;
        internal System.Windows.Forms.Label labelPendingBGTasks;
        internal System.Windows.Forms.Label labelProcessedMessages;
        internal System.Windows.Forms.Label labelOutstandingIncMessages;
        internal System.Windows.Forms.Label labelOutgoingPackets;
        internal System.Windows.Forms.Label labelReceivedPackets;
        internal System.Windows.Forms.Label labelPlayerInfoRefresh;
        internal System.Windows.Forms.Label labelNonUpdating;
        internal System.Windows.Forms.Label labelMainUpdateTime;
        internal System.Windows.Forms.Label labelServerMsgWaitTime;
        internal System.Windows.Forms.Label lblMessageUpdateTime;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox checkBoxPerformanceLogging;
        private System.Windows.Forms.GroupBox DailyRewards;
        private System.Windows.Forms.CheckBox RewardsEnabled;
        private System.Windows.Forms.Button ReloadRewards;
        private System.Windows.Forms.GroupBox groupBox2;
        public System.Windows.Forms.RadioButton AHRadioButtonOffline;
        public System.Windows.Forms.CheckBox resetDurationsCheckBox;
        public System.Windows.Forms.RadioButton AHRadioButtonSafeMode;
        public System.Windows.Forms.RadioButton AHRadioButtonOnline;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button buttonSpecialOffers;
        private System.Windows.Forms.Button btnReinitialiseThirdPartyOptions;
        private System.Windows.Forms.Button btnLogOptions;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox groupBoxAdminList;
        internal System.Windows.Forms.Label lblLongestMessageProcess;
        private System.Windows.Forms.ToolTip toolTipReadSleepMillis;
        private System.Windows.Forms.CheckBox checkBoxDebug;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button buttonReloadAdminList;
        private System.Windows.Forms.Button buttonReloadLocalisation;
    }
}

