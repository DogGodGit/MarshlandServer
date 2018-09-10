using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MainServer
{
    public partial class RelocateConfirm : Form
    {
        internal List<Player> m_RelocatePlayers = null;
        public RelocateConfirm()
        {
            InitializeComponent();
            this.CancelButton = btnCancel;
        
        }

        private void btnOk_Click(object sender, EventArgs e)
        {            
            Program.refreshPlayersTimer_Tick(null, null);
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
