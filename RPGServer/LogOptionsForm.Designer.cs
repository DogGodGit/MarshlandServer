namespace MainServer
{
    partial class LogOptionsForm
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
            this.chkShowText = new System.Windows.Forms.CheckBox();
            this.chkDebugDB = new System.Windows.Forms.CheckBox();
            this.chkAllMessages = new System.Windows.Forms.CheckBox();
            this.chkAutoScroll = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chkMobAIDebug = new System.Windows.Forms.CheckBox();
            this.chkMobPathingProblems = new System.Windows.Forms.CheckBox();
            this.chkPartitionUpdates = new System.Windows.Forms.CheckBox();
            this.chkInterestChanges = new System.Windows.Forms.CheckBox();
            this.chkShowDamages = new System.Windows.Forms.CheckBox();
            this.chkSysBlock = new System.Windows.Forms.CheckBox();
            this.chkSysParty = new System.Windows.Forms.CheckBox();
            this.chkSysBattle = new System.Windows.Forms.CheckBox();
            this.chkSysClan = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkSysSkills = new System.Windows.Forms.CheckBox();
            this.chkSysFriends = new System.Windows.Forms.CheckBox();
            this.chkAggroDebug = new System.Windows.Forms.CheckBox();
            this.chkAStarDebug = new System.Windows.Forms.CheckBox();
            this.chkNonSpawns = new System.Windows.Forms.CheckBox();
            this.chkQuestStatus = new System.Windows.Forms.CheckBox();
            this.chkSpawns = new System.Windows.Forms.CheckBox();
            this.chkRanking = new System.Windows.Forms.CheckBox();
            this.txtZoneLag = new System.Windows.Forms.TextBox();
            this.txtMobLag = new System.Windows.Forms.TextBox();
            this.txtMessageLag = new System.Windows.Forms.TextBox();
            this.lblZoneLag = new System.Windows.Forms.Label();
            this.lblMobLag = new System.Windows.Forms.Label();
            this.lblMessageLag = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.chkEnableAIMap = new System.Windows.Forms.CheckBox();
            this.chkEnableCollisions = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // chkShowText
            // 
            this.chkShowText.AutoSize = true;
            this.chkShowText.Checked = true;
            this.chkShowText.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowText.Location = new System.Drawing.Point(23, 12);
            this.chkShowText.Name = "chkShowText";
            this.chkShowText.Size = new System.Drawing.Size(77, 17);
            this.chkShowText.TabIndex = 11;
            this.chkShowText.Text = "Show Text";
            this.chkShowText.UseVisualStyleBackColor = true;
            // 
            // chkDebugDB
            // 
            this.chkDebugDB.AutoSize = true;
            this.chkDebugDB.Location = new System.Drawing.Point(23, 93);
            this.chkDebugDB.Name = "chkDebugDB";
            this.chkDebugDB.Size = new System.Drawing.Size(140, 17);
            this.chkDebugDB.TabIndex = 10;
            this.chkDebugDB.Text = "All Database Operations";
            this.chkDebugDB.UseVisualStyleBackColor = true;
            this.chkDebugDB.CheckedChanged += new System.EventHandler(this.chkDebugDB_CheckedChanged);
            // 
            // chkAllMessages
            // 
            this.chkAllMessages.AutoSize = true;
            this.chkAllMessages.Location = new System.Drawing.Point(23, 116);
            this.chkAllMessages.Name = "chkAllMessages";
            this.chkAllMessages.Size = new System.Drawing.Size(131, 17);
            this.chkAllMessages.TabIndex = 9;
            this.chkAllMessages.Text = "All Network Messages";
            this.chkAllMessages.UseVisualStyleBackColor = true;
            this.chkAllMessages.CheckedChanged += new System.EventHandler(this.chkAllMessages_CheckedChanged);
            // 
            // chkAutoScroll
            // 
            this.chkAutoScroll.AutoSize = true;
            this.chkAutoScroll.Checked = true;
            this.chkAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoScroll.Location = new System.Drawing.Point(229, 12);
            this.chkAutoScroll.Name = "chkAutoScroll";
            this.chkAutoScroll.Size = new System.Drawing.Size(119, 17);
            this.chkAutoScroll.TabIndex = 8;
            this.chkAutoScroll.Text = "Auto Scroll Window";
            this.chkAutoScroll.UseVisualStyleBackColor = true;
            this.chkAutoScroll.CheckedChanged += new System.EventHandler(this.chkAutoScroll_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(20, 62);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 15);
            this.label1.TabIndex = 12;
            this.label1.Text = "Optional Logging";
            // 
            // chkMobAIDebug
            // 
            this.chkMobAIDebug.AutoSize = true;
            this.chkMobAIDebug.Location = new System.Drawing.Point(23, 139);
            this.chkMobAIDebug.Name = "chkMobAIDebug";
            this.chkMobAIDebug.Size = new System.Drawing.Size(95, 17);
            this.chkMobAIDebug.TabIndex = 13;
            this.chkMobAIDebug.Text = "Mob AI Debug";
            this.chkMobAIDebug.UseVisualStyleBackColor = true;
            this.chkMobAIDebug.CheckedChanged += new System.EventHandler(this.chkMobAIDebug_CheckedChanged);
            // 
            // chkMobPathingProblems
            // 
            this.chkMobPathingProblems.AutoSize = true;
            this.chkMobPathingProblems.Location = new System.Drawing.Point(23, 162);
            this.chkMobPathingProblems.Name = "chkMobPathingProblems";
            this.chkMobPathingProblems.Size = new System.Drawing.Size(132, 17);
            this.chkMobPathingProblems.TabIndex = 14;
            this.chkMobPathingProblems.Text = "Mob Pathing Problems";
            this.chkMobPathingProblems.UseVisualStyleBackColor = true;
            this.chkMobPathingProblems.CheckedChanged += new System.EventHandler(this.chkMobPathingProblems_CheckedChanged);
            // 
            // chkPartitionUpdates
            // 
            this.chkPartitionUpdates.AutoSize = true;
            this.chkPartitionUpdates.Location = new System.Drawing.Point(23, 185);
            this.chkPartitionUpdates.Name = "chkPartitionUpdates";
            this.chkPartitionUpdates.Size = new System.Drawing.Size(107, 17);
            this.chkPartitionUpdates.TabIndex = 15;
            this.chkPartitionUpdates.Text = "Partition Updates";
            this.chkPartitionUpdates.UseVisualStyleBackColor = true;
            // 
            // chkInterestChanges
            // 
            this.chkInterestChanges.AutoSize = true;
            this.chkInterestChanges.Location = new System.Drawing.Point(23, 208);
            this.chkInterestChanges.Name = "chkInterestChanges";
            this.chkInterestChanges.Size = new System.Drawing.Size(106, 17);
            this.chkInterestChanges.TabIndex = 16;
            this.chkInterestChanges.Text = "Interest Changes";
            this.chkInterestChanges.UseVisualStyleBackColor = true;
            this.chkInterestChanges.CheckedChanged += new System.EventHandler(this.chkInterestChanges_CheckedChanged);
            // 
            // chkShowDamages
            // 
            this.chkShowDamages.AutoSize = true;
            this.chkShowDamages.Location = new System.Drawing.Point(23, 231);
            this.chkShowDamages.Name = "chkShowDamages";
            this.chkShowDamages.Size = new System.Drawing.Size(71, 17);
            this.chkShowDamages.TabIndex = 17;
            this.chkShowDamages.Text = "Damages";
            this.chkShowDamages.UseVisualStyleBackColor = true;
            this.chkShowDamages.CheckedChanged += new System.EventHandler(this.chkShowDamages_CheckedChanged);
            // 
            // chkSysBlock
            // 
            this.chkSysBlock.AutoSize = true;
            this.chkSysBlock.Location = new System.Drawing.Point(229, 208);
            this.chkSysBlock.Name = "chkSysBlock";
            this.chkSysBlock.Size = new System.Drawing.Size(53, 17);
            this.chkSysBlock.TabIndex = 24;
            this.chkSysBlock.Text = "Block";
            this.chkSysBlock.UseVisualStyleBackColor = true;
            this.chkSysBlock.CheckedChanged += new System.EventHandler(this.chkSysBlock_CheckedChanged);
            // 
            // chkSysParty
            // 
            this.chkSysParty.AutoSize = true;
            this.chkSysParty.Location = new System.Drawing.Point(229, 185);
            this.chkSysParty.Name = "chkSysParty";
            this.chkSysParty.Size = new System.Drawing.Size(50, 17);
            this.chkSysParty.TabIndex = 23;
            this.chkSysParty.Text = "Party";
            this.chkSysParty.UseVisualStyleBackColor = true;
            this.chkSysParty.CheckedChanged += new System.EventHandler(this.chkSysParty_CheckedChanged);
            // 
            // chkSysBattle
            // 
            this.chkSysBattle.AutoSize = true;
            this.chkSysBattle.Location = new System.Drawing.Point(229, 162);
            this.chkSysBattle.Name = "chkSysBattle";
            this.chkSysBattle.Size = new System.Drawing.Size(53, 17);
            this.chkSysBattle.TabIndex = 22;
            this.chkSysBattle.Text = "Battle";
            this.chkSysBattle.UseVisualStyleBackColor = true;
            this.chkSysBattle.CheckedChanged += new System.EventHandler(this.chkSysBattle_CheckedChanged);
            // 
            // chkSysClan
            // 
            this.chkSysClan.AutoSize = true;
            this.chkSysClan.Location = new System.Drawing.Point(229, 139);
            this.chkSysClan.Name = "chkSysClan";
            this.chkSysClan.Size = new System.Drawing.Size(47, 17);
            this.chkSysClan.TabIndex = 21;
            this.chkSysClan.Text = "Clan";
            this.chkSysClan.UseVisualStyleBackColor = true;
            this.chkSysClan.CheckedChanged += new System.EventHandler(this.chkSysClan_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(226, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(122, 15);
            this.label2.TabIndex = 20;
            this.label2.Text = "System Messages";
            // 
            // chkSysSkills
            // 
            this.chkSysSkills.AutoSize = true;
            this.chkSysSkills.Location = new System.Drawing.Point(229, 93);
            this.chkSysSkills.Name = "chkSysSkills";
            this.chkSysSkills.Size = new System.Drawing.Size(50, 17);
            this.chkSysSkills.TabIndex = 19;
            this.chkSysSkills.Text = "Skills";
            this.chkSysSkills.UseVisualStyleBackColor = true;
            this.chkSysSkills.CheckedChanged += new System.EventHandler(this.chkSysSkills_CheckedChanged);
            // 
            // chkSysFriends
            // 
            this.chkSysFriends.AutoSize = true;
            this.chkSysFriends.Location = new System.Drawing.Point(229, 116);
            this.chkSysFriends.Name = "chkSysFriends";
            this.chkSysFriends.Size = new System.Drawing.Size(60, 17);
            this.chkSysFriends.TabIndex = 18;
            this.chkSysFriends.Text = "Friends";
            this.chkSysFriends.UseVisualStyleBackColor = true;
            this.chkSysFriends.CheckedChanged += new System.EventHandler(this.chkSysFriends_CheckedChanged);
            // 
            // chkAggroDebug
            // 
            this.chkAggroDebug.AutoSize = true;
            this.chkAggroDebug.Location = new System.Drawing.Point(23, 277);
            this.chkAggroDebug.Name = "chkAggroDebug";
            this.chkAggroDebug.Size = new System.Drawing.Size(89, 17);
            this.chkAggroDebug.TabIndex = 25;
            this.chkAggroDebug.Text = "Aggro Debug";
            this.chkAggroDebug.UseVisualStyleBackColor = true;
            this.chkAggroDebug.CheckedChanged += new System.EventHandler(this.chkAggroDebug_CheckedChanged);
            // 
            // chkAStarDebug
            // 
            this.chkAStarDebug.AutoSize = true;
            this.chkAStarDebug.Location = new System.Drawing.Point(23, 254);
            this.chkAStarDebug.Name = "chkAStarDebug";
            this.chkAStarDebug.Size = new System.Drawing.Size(90, 17);
            this.chkAStarDebug.TabIndex = 26;
            this.chkAStarDebug.Text = "A Star Debug";
            this.chkAStarDebug.UseVisualStyleBackColor = true;
            this.chkAStarDebug.CheckedChanged += new System.EventHandler(this.chkAStarDebug_CheckedChanged);
            // 
            // chkNonSpawns
            // 
            this.chkNonSpawns.AutoSize = true;
            this.chkNonSpawns.Location = new System.Drawing.Point(23, 300);
            this.chkNonSpawns.Name = "chkNonSpawns";
            this.chkNonSpawns.Size = new System.Drawing.Size(87, 17);
            this.chkNonSpawns.TabIndex = 27;
            this.chkNonSpawns.Text = "Non Spawns";
            this.chkNonSpawns.UseVisualStyleBackColor = true;
            this.chkNonSpawns.CheckedChanged += new System.EventHandler(this.chkNonSpawns_CheckedChanged);
            // 
            // chkQuestStatus
            // 
            this.chkQuestStatus.AutoSize = true;
            this.chkQuestStatus.Location = new System.Drawing.Point(23, 323);
            this.chkQuestStatus.Name = "chkQuestStatus";
            this.chkQuestStatus.Size = new System.Drawing.Size(87, 17);
            this.chkQuestStatus.TabIndex = 28;
            this.chkQuestStatus.Text = "Quest Status";
            this.chkQuestStatus.UseVisualStyleBackColor = true;
            this.chkQuestStatus.CheckedChanged += new System.EventHandler(this.chkQuestStatus_CheckedChanged);
            // 
            // chkSpawns
            // 
            this.chkSpawns.AutoSize = true;
            this.chkSpawns.Location = new System.Drawing.Point(23, 347);
            this.chkSpawns.Name = "chkSpawns";
            this.chkSpawns.Size = new System.Drawing.Size(115, 17);
            this.chkSpawns.TabIndex = 29;
            this.chkSpawns.Text = "Spawn / Despawn";
            this.chkSpawns.UseVisualStyleBackColor = true;
            this.chkSpawns.CheckedChanged += new System.EventHandler(this.chkSpawns_CheckedChanged);
            // 
            // chkRanking
            // 
            this.chkRanking.AutoSize = true;
            this.chkRanking.Location = new System.Drawing.Point(23, 371);
            this.chkRanking.Name = "chkRanking";
            this.chkRanking.Size = new System.Drawing.Size(66, 17);
            this.chkRanking.TabIndex = 30;
            this.chkRanking.Text = "Ranking";
            this.chkRanking.UseVisualStyleBackColor = true;
            this.chkRanking.CheckedChanged += new System.EventHandler(this.chkRanking_CheckedChanged);
            // 
            // txtZoneLag
            // 
            this.txtZoneLag.Location = new System.Drawing.Point(459, 93);
            this.txtZoneLag.Name = "txtZoneLag";
            this.txtZoneLag.Size = new System.Drawing.Size(72, 20);
            this.txtZoneLag.TabIndex = 31;
            this.txtZoneLag.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtZoneLag.TextChanged += new System.EventHandler(this.txtZoneLag_TextChanged);
            // 
            // txtMobLag
            // 
            this.txtMobLag.Location = new System.Drawing.Point(459, 120);
            this.txtMobLag.Name = "txtMobLag";
            this.txtMobLag.Size = new System.Drawing.Size(72, 20);
            this.txtMobLag.TabIndex = 32;
            this.txtMobLag.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtMobLag.TextChanged += new System.EventHandler(this.txtMobLag_TextChanged);
            // 
            // txtMessageLag
            // 
            this.txtMessageLag.Location = new System.Drawing.Point(459, 147);
            this.txtMessageLag.Name = "txtMessageLag";
            this.txtMessageLag.Size = new System.Drawing.Size(72, 20);
            this.txtMessageLag.TabIndex = 33;
            this.txtMessageLag.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtMessageLag.TextChanged += new System.EventHandler(this.txtMessageLag_TextChanged);
            // 
            // lblZoneLag
            // 
            this.lblZoneLag.AutoSize = true;
            this.lblZoneLag.Location = new System.Drawing.Point(400, 97);
            this.lblZoneLag.Name = "lblZoneLag";
            this.lblZoneLag.Size = new System.Drawing.Size(53, 13);
            this.lblZoneLag.TabIndex = 34;
            this.lblZoneLag.Text = "Zone Lag";
            this.lblZoneLag.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMobLag
            // 
            this.lblMobLag.AutoSize = true;
            this.lblMobLag.Location = new System.Drawing.Point(404, 123);
            this.lblMobLag.Name = "lblMobLag";
            this.lblMobLag.Size = new System.Drawing.Size(49, 13);
            this.lblMobLag.TabIndex = 35;
            this.lblMobLag.Text = "Mob Lag";
            this.lblMobLag.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMessageLag
            // 
            this.lblMessageLag.AutoSize = true;
            this.lblMessageLag.Location = new System.Drawing.Point(382, 147);
            this.lblMessageLag.Name = "lblMessageLag";
            this.lblMessageLag.Size = new System.Drawing.Size(71, 13);
            this.lblMessageLag.TabIndex = 36;
            this.lblMessageLag.Text = "Message Lag";
            this.lblMessageLag.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(417, 64);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 13);
            this.label3.TabIndex = 37;
            this.label3.Text = "Diagnostics";
            // 
            // chkEnableAIMap
            // 
            this.chkEnableAIMap.AutoSize = true;
            this.chkEnableAIMap.Location = new System.Drawing.Point(438, 173);
            this.chkEnableAIMap.Name = "chkEnableAIMap";
            this.chkEnableAIMap.Size = new System.Drawing.Size(96, 17);
            this.chkEnableAIMap.TabIndex = 38;
            this.chkEnableAIMap.Text = "Enable AI Map";
            this.chkEnableAIMap.UseVisualStyleBackColor = true;
            this.chkEnableAIMap.CheckedChanged += new System.EventHandler(this.chkEnableAIMap_CheckedChanged);
            // 
            // chkEnableCollisions
            // 
            this.chkEnableCollisions.AutoSize = true;
            this.chkEnableCollisions.Location = new System.Drawing.Point(438, 196);
            this.chkEnableCollisions.Name = "chkEnableCollisions";
            this.chkEnableCollisions.Size = new System.Drawing.Size(105, 17);
            this.chkEnableCollisions.TabIndex = 39;
            this.chkEnableCollisions.Text = "Enable Collisions";
            this.chkEnableCollisions.UseVisualStyleBackColor = true;
            this.chkEnableCollisions.CheckedChanged += new System.EventHandler(this.chkEnableCollisions_CheckedChanged);
            // 
            // LogOptionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(558, 417);
            this.Controls.Add(this.chkEnableCollisions);
            this.Controls.Add(this.chkEnableAIMap);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblMessageLag);
            this.Controls.Add(this.lblMobLag);
            this.Controls.Add(this.lblZoneLag);
            this.Controls.Add(this.txtMessageLag);
            this.Controls.Add(this.txtMobLag);
            this.Controls.Add(this.txtZoneLag);
            this.Controls.Add(this.chkRanking);
            this.Controls.Add(this.chkSpawns);
            this.Controls.Add(this.chkQuestStatus);
            this.Controls.Add(this.chkNonSpawns);
            this.Controls.Add(this.chkAStarDebug);
            this.Controls.Add(this.chkAggroDebug);
            this.Controls.Add(this.chkSysBlock);
            this.Controls.Add(this.chkSysParty);
            this.Controls.Add(this.chkSysBattle);
            this.Controls.Add(this.chkSysClan);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.chkSysSkills);
            this.Controls.Add(this.chkSysFriends);
            this.Controls.Add(this.chkShowDamages);
            this.Controls.Add(this.chkInterestChanges);
            this.Controls.Add(this.chkPartitionUpdates);
            this.Controls.Add(this.chkMobPathingProblems);
            this.Controls.Add(this.chkMobAIDebug);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkShowText);
            this.Controls.Add(this.chkDebugDB);
            this.Controls.Add(this.chkAllMessages);
            this.Controls.Add(this.chkAutoScroll);
            this.Name = "LogOptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Log Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkShowText;
        private System.Windows.Forms.CheckBox chkDebugDB;
        private System.Windows.Forms.CheckBox chkAllMessages;
        public System.Windows.Forms.CheckBox chkAutoScroll;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkMobAIDebug;
        private System.Windows.Forms.CheckBox chkMobPathingProblems;
        private System.Windows.Forms.CheckBox chkPartitionUpdates;
        private System.Windows.Forms.CheckBox chkInterestChanges;
        private System.Windows.Forms.CheckBox chkShowDamages;
        private System.Windows.Forms.CheckBox chkSysBlock;
        private System.Windows.Forms.CheckBox chkSysParty;
        private System.Windows.Forms.CheckBox chkSysBattle;
        private System.Windows.Forms.CheckBox chkSysClan;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkSysSkills;
        private System.Windows.Forms.CheckBox chkSysFriends;
        private System.Windows.Forms.CheckBox chkAggroDebug;
        private System.Windows.Forms.CheckBox chkAStarDebug;
        private System.Windows.Forms.CheckBox chkNonSpawns;
        private System.Windows.Forms.CheckBox chkQuestStatus;
        private System.Windows.Forms.CheckBox chkSpawns;
        private System.Windows.Forms.CheckBox chkRanking;
        private System.Windows.Forms.Label lblZoneLag;
        private System.Windows.Forms.Label lblMobLag;
        private System.Windows.Forms.Label lblMessageLag;
        internal System.Windows.Forms.TextBox txtZoneLag;
        internal System.Windows.Forms.TextBox txtMobLag;
        internal System.Windows.Forms.TextBox txtMessageLag;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkEnableAIMap;
        private System.Windows.Forms.CheckBox chkEnableCollisions;
    }
}