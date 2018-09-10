using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MainServer
{
    public partial class KickConfirm : Form
    {
        internal List<Player> m_kickPlayers = null;
        public KickConfirm()
        {
            InitializeComponent();
            this.CancelButton = btnCancel;
        
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < m_kickPlayers.Count; i++)
            {
              
                Program.processor.disconnect(m_kickPlayers[i], true,"");
            }
            Program.refreshPlayersTimer_Tick(null, null);
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
