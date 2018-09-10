using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MainServer
{
    public partial class SystemMessageForm : Form
    {
        public SystemMessageForm()
        {
            InitializeComponent();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if(txtMessage.Text.Length>0)
            {
                Program.sendSystemMessage(txtMessage.Text, chkAsPopup.Checked, chkSelectedOnly.Checked);
            }
            Close();
        }


    }
}
