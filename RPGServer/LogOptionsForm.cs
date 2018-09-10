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
    public partial class LogOptionsForm : Form
    {
        
        public LogOptionsForm()
        {
            InitializeComponent();
            chkShowText.Checked=Program.m_showText ;
            chkAutoScroll.Checked = Program.m_AutoScrollText;
            chkDebugDB.Checked = Database.debug_database;
            chkAllMessages.Checked = Program.m_showAllMsgs;
            chkMobAIDebug.Checked = Program.m_LogAIDebug;
            chkMobPathingProblems.Checked = Program.m_LogPathingErrors;
            chkPartitionUpdates.Checked = Program.m_LogPartitionUpdates;
            chkInterestChanges.Checked = Program.m_LogInterestLists;
            chkShowDamages.Checked = Program.m_LogDamage;
            chkSysSkills.Checked = Program.m_LogSysSkills;
            chkSysFriends.Checked = Program.m_LogSysFriends;
            chkSysClan.Checked = Program.m_LogSysClan;
            chkSysBattle.Checked = Program.m_LogSysBattle;
            chkSysParty.Checked = Program.m_LogSysParty;
            chkSysBlock.Checked = Program.m_LogSysBlock;
            chkNonSpawns.Checked = Program.m_LogNonSpawns;
            chkSpawns.Checked = Program.m_LogSpawns;
            chkRanking.Checked = Program.m_LogRanking;
            chkQuestStatus.Checked = Program.m_LogQuests;
            txtMessageLag.Text = (Program.m_longMessageThreshold * 1000).ToString();
            txtMobLag.Text = (Program.m_longMobUpdateThreshold * 1000).ToString();
            txtZoneLag.Text = (Program.m_longZoneUpdateThreshold * 1000).ToString();
            chkEnableAIMap.Checked = Program.m_AIMapEnabled;
            chkEnableCollisions.Checked = Program.m_CollisionsEnabled;
        }

        private void chkAutoScroll_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_AutoScrollText = chkAutoScroll.Checked;
            if (Program.m_AutoScrollText)
            {
                Program.MainForm.AddFocusToRichTextBox();
            }
            else
            {
                Program.MainForm.RemoveFocusFromRichTextBox();                
            }
        }

        private void chkDebugDB_CheckedChanged(object sender, EventArgs e)
        {
            Database.debug_database=chkDebugDB.Checked;
        }

        private void chkMobAIDebug_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogAIDebug = chkMobAIDebug.Checked;
        }

        private void chkAllMessages_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_showAllMsgs=chkAllMessages.Checked ;
        }

        private void chkMobPathingProblems_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogPathingErrors=chkMobPathingProblems.Checked  ;
        }

        private void chkInterestChanges_CheckedChanged(object sender, EventArgs e)
        {
           Program.m_LogInterestLists= chkInterestChanges.Checked  ;
        }

        private void chkShowDamages_CheckedChanged(object sender, EventArgs e)
        {
           Program.m_LogDamage= chkShowDamages.Checked  ;
        }

        private void chkSysSkills_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogSysSkills=chkSysSkills.Checked ;
        }

        private void chkSysFriends_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogSysFriends=chkSysFriends.Checked;
        }

        private void chkSysClan_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogSysClan=chkSysClan.Checked ;
        }

        private void chkSysBattle_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogSysBattle=chkSysBattle.Checked ;
        }

        private void chkSysParty_CheckedChanged(object sender, EventArgs e)
        {
             Program.m_LogSysParty=chkSysParty.Checked ;
        }

        private void chkSysBlock_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogSysBlock= chkSysBlock.Checked;
        }

        private void chkAStarDebug_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_A_Star_Debugging = chkAStarDebug.Checked;
        }

        private void chkAggroDebug_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_Aggro_debugging = chkAggroDebug.Checked;
        }

        private void chkNonSpawns_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogNonSpawns = chkNonSpawns.Checked;
        }

        private void chkQuestStatus_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogQuests = chkQuestStatus.Checked;
        }

        private void chkSpawns_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogSpawns = chkSpawns.Checked;
        }

        private void chkRanking_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_LogRanking = chkRanking.Checked;
        }

        private void txtZoneLag_TextChanged(object sender, EventArgs e)
        {
            float lag;
            if (float.TryParse(txtZoneLag.Text,out lag))
            {
                Program.m_longZoneUpdateThreshold = lag / 1000;
            }
        }

        private void txtMobLag_TextChanged(object sender, EventArgs e)
        {
            float lag;
            if (float.TryParse(txtMobLag.Text, out lag))
            {
                Program.m_longMobUpdateThreshold = lag / 1000;
            }
        }

        private void txtMessageLag_TextChanged(object sender, EventArgs e)
        {
            float lag;
            if (float.TryParse(txtMessageLag.Text, out lag))
            {
                Program.m_longMessageThreshold = lag / 1000;
            }
        }

        private void chkEnableAIMap_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_AIMapEnabled = chkEnableAIMap.Checked;
        }

        private void chkEnableCollisions_CheckedChanged(object sender, EventArgs e)
        {
            Program.m_CollisionsEnabled = chkEnableCollisions.Checked;
        }
    }
}
