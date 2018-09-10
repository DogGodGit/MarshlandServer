namespace AStarMesh
{
    partial class Form1
    {

        ASPathFinder m_pathFinder = new ASPathFinder();

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
            this.mapView = new AStarMesh.DoubleBufferPanel();
            this.nextStepButton = new System.Windows.Forms.Button();
            this.resetButton = new System.Windows.Forms.Button();
            this.startPosX = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.startPosY = new System.Windows.Forms.TextBox();
            this.startPosZ = new System.Windows.Forms.TextBox();
            this.endPosX = new System.Windows.Forms.TextBox();
            this.endPosY = new System.Windows.Forms.TextBox();
            this.endPosZ = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // mapView
            // 
            this.mapView.BackColor = System.Drawing.SystemColors.ControlText;
            this.mapView.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.mapView.Location = new System.Drawing.Point(2, 41);
            this.mapView.Name = "mapView";
            this.mapView.Size = new System.Drawing.Size(802, 478);
            this.mapView.TabIndex = 0;
            this.mapView.Paint += new System.Windows.Forms.PaintEventHandler(this.mapView_Paint);
            // 
            // nextStepButton
            // 
            this.nextStepButton.Location = new System.Drawing.Point(622, 12);
            this.nextStepButton.Name = "nextStepButton";
            this.nextStepButton.Size = new System.Drawing.Size(75, 23);
            this.nextStepButton.TabIndex = 1;
            this.nextStepButton.Text = "Next Step";
            this.nextStepButton.UseVisualStyleBackColor = true;
            this.nextStepButton.Click += new System.EventHandler(this.nextStepButton_Click);
            // 
            // resetButton
            // 
            this.resetButton.Location = new System.Drawing.Point(718, 12);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(75, 23);
            this.resetButton.TabIndex = 2;
            this.resetButton.Text = "Reset";
            this.resetButton.UseVisualStyleBackColor = true;
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
            // 
            // startPosX
            // 
            this.startPosX.Location = new System.Drawing.Point(93, 15);
            this.startPosX.Name = "startPosX";
            this.startPosX.Size = new System.Drawing.Size(38, 20);
            this.startPosX.TabIndex = 3;
            this.startPosX.Text = "150";
            this.startPosX.TextChanged += new System.EventHandler(this.startPosX_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(293, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "End Point";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(31, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Start Point";
            // 
            // startPosY
            // 
            this.startPosY.Location = new System.Drawing.Point(137, 15);
            this.startPosY.Name = "startPosY";
            this.startPosY.Size = new System.Drawing.Size(38, 20);
            this.startPosY.TabIndex = 7;
            this.startPosY.Text = "4";
            // 
            // startPosZ
            // 
            this.startPosZ.Location = new System.Drawing.Point(181, 14);
            this.startPosZ.Name = "startPosZ";
            this.startPosZ.Size = new System.Drawing.Size(38, 20);
            this.startPosZ.TabIndex = 8;
            this.startPosZ.Text = "150";
            // 
            // endPosX
            // 
            this.endPosX.Location = new System.Drawing.Point(352, 15);
            this.endPosX.Name = "endPosX";
            this.endPosX.Size = new System.Drawing.Size(38, 20);
            this.endPosX.TabIndex = 9;
            this.endPosX.Text = "-150";
            this.endPosX.TextChanged += new System.EventHandler(this.endPosX_TextChanged);
            // 
            // endPosY
            // 
            this.endPosY.Location = new System.Drawing.Point(396, 15);
            this.endPosY.Name = "endPosY";
            this.endPosY.Size = new System.Drawing.Size(38, 20);
            this.endPosY.TabIndex = 10;
            this.endPosY.Text = "4";
            // 
            // endPosZ
            // 
            this.endPosZ.Location = new System.Drawing.Point(440, 15);
            this.endPosZ.Name = "endPosZ";
            this.endPosZ.Size = new System.Drawing.Size(38, 20);
            this.endPosZ.TabIndex = 11;
            this.endPosZ.Text = "150";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(805, 531);
            this.Controls.Add(this.endPosZ);
            this.Controls.Add(this.endPosY);
            this.Controls.Add(this.endPosX);
            this.Controls.Add(this.startPosZ);
            this.Controls.Add(this.startPosY);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.startPosX);
            this.Controls.Add(this.resetButton);
            this.Controls.Add(this.nextStepButton);
            this.Controls.Add(this.mapView);
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal AStarMesh.DoubleBufferPanel mapView;
        private System.Windows.Forms.Button nextStepButton;
        private System.Windows.Forms.Button resetButton;
        private System.Windows.Forms.TextBox startPosX;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox startPosY;
        private System.Windows.Forms.TextBox startPosZ;
        private System.Windows.Forms.TextBox endPosX;
        private System.Windows.Forms.TextBox endPosY;
        private System.Windows.Forms.TextBox endPosZ;

    }
}

