namespace MainServer
{
    partial class SystemMessageForm
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
            this.btnSend = new System.Windows.Forms.Button();
            this.chkAsPopup = new System.Windows.Forms.CheckBox();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chkSelectedOnly = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(503, 58);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 29);
            this.btnSend.TabIndex = 0;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // chkAsPopup
            // 
            this.chkAsPopup.AutoSize = true;
            this.chkAsPopup.Location = new System.Drawing.Point(503, 13);
            this.chkAsPopup.Name = "chkAsPopup";
            this.chkAsPopup.Size = new System.Drawing.Size(72, 17);
            this.chkAsPopup.TabIndex = 1;
            this.chkAsPopup.Text = "As Popup";
            this.chkAsPopup.UseVisualStyleBackColor = true;
            // 
            // txtMessage
            // 
            this.txtMessage.Location = new System.Drawing.Point(70, 13);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessage.Size = new System.Drawing.Size(423, 74);
            this.txtMessage.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Message";
            // 
            // chkSelectedOnly
            // 
            this.chkSelectedOnly.AutoSize = true;
            this.chkSelectedOnly.Location = new System.Drawing.Point(503, 36);
            this.chkSelectedOnly.Name = "chkSelectedOnly";
            this.chkSelectedOnly.Size = new System.Drawing.Size(92, 17);
            this.chkSelectedOnly.TabIndex = 4;
            this.chkSelectedOnly.Text = "Selected Only";
            this.chkSelectedOnly.UseVisualStyleBackColor = true;
            // 
            // SystemMessageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(605, 99);
            this.Controls.Add(this.chkSelectedOnly);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.chkAsPopup);
            this.Controls.Add(this.btnSend);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SystemMessageForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sytem Message Form";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.CheckBox chkAsPopup;
        private System.Windows.Forms.Label label1;
        internal System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.CheckBox chkSelectedOnly;
    }
}