using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

using Lidgren.Network;

namespace SamplesCommon
{
	public partial class NetPeerSettingsWindow : Form
	{
		public NetPeer Peer;
		public Timer timer;
        public int m_statsForConnection;
		public NetPeerSettingsWindow(string title, NetPeer peer)
		{
			Peer = peer;
			InitializeComponent();
			RefreshData();
			this.Text = title;

			// auto refresh now and then
			timer = new Timer();
			timer.Interval = 250;
			timer.Tick += new EventHandler(timer_Tick);
			timer.Enabled = true;
            m_statsForConnection = 0;
		}

		protected override void OnClosed(EventArgs e)
		{
			timer.Enabled = false;
			base.OnClosed(e);
		}

		void timer_Tick(object sender, EventArgs e)
		{
            try
            {
                RefreshData();
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception in stats" + ex.Message + " : " + ex.StackTrace);
            }
        }

		private void RefreshData()
		{
			LossTextBox.Text = "0";
			MinLatencyTextBox.Text = "0";
			textBox3.Text = "0";
			textBox2.Text = "0";
			DebugCheckBox.Checked = Peer.Configuration.IsMessageTypeEnabled(NetIncomingMessageType.DebugMessage);
			VerboseCheckBox.Checked = Peer.Configuration.IsMessageTypeEnabled(NetIncomingMessageType.VerboseDebugMessage);

			StringBuilder bdr = new StringBuilder();
			bdr.AppendLine(Peer.Statistics.ToString());

			if (Peer.ConnectionsCount > 0)
			{
                int connectionToShow=m_statsForConnection;
                if(connectionToShow>Peer.ConnectionsCount-1)
                {
                    connectionToShow=Peer.ConnectionsCount-1;
                }
				NetConnection conn = Peer.Connections[connectionToShow];
				bdr.AppendLine("Connection " +connectionToShow + ":");
				bdr.AppendLine("Average RTT: " + ((int)(conn.AverageRoundtripTime * 1000.0f)) + " ms");

				bdr.Append(conn.Statistics.ToString());
			}

			StatisticsLabel.Text = bdr.ToString();
             
		}

		private void DebugCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			Peer.Configuration.SetMessageTypeEnabled(NetIncomingMessageType.DebugMessage, DebugCheckBox.Checked);
		}

		private void VerboseCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			Peer.Configuration.SetMessageTypeEnabled(NetIncomingMessageType.VerboseDebugMessage, VerboseCheckBox.Checked);
		}

		private void LossTextBox_TextChanged(object sender, EventArgs e)
		{
		}

		private void textBox2_TextChanged(object sender, EventArgs e)
		{
		}

		private void MinLatencyTextBox_TextChanged(object sender, EventArgs e)
		{
		}

		private void textBox3_TextChanged(object sender, EventArgs e)
		{
		}

		private void button1_Click(object sender, EventArgs e)
		{
			RefreshData();
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
		}

		private void button2_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void ThrottleTextBox_TextChanged(object sender, EventArgs e)
		{
		}

        private void txtConnectionStat_TextChanged(object sender, EventArgs e)
        {
            if (!Int32.TryParse(((TextBox)sender).Text, out m_statsForConnection))
            {
                m_statsForConnection = 0;
                txtConnectionStat.Text = "0";
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
        }

	}
}