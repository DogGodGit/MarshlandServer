using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainServer.Signposting
{
    class SignpostMailTask : BaseTask
    {

        private int m_characterID;
        private string m_mailSubject;
        private string m_mailMessage;
        private Item m_item;
        private int m_gold;
        private string m_senderName;
        private int m_isAttachedItem;

        internal SignpostMailTask(int characterID, string mailSubject, string mailMessage, Item item, int isAttachedItem, int attachedGold, string senderName)
        {
            m_characterID = characterID;
            m_mailSubject = mailSubject;
            m_mailMessage = mailMessage;
            m_item = item;
            m_gold = attachedGold;
            m_senderName = senderName;
            m_isAttachedItem = isAttachedItem;

            m_TaskType = TaskType.SignpostMail;
        }

        internal override void TakeAction(CommandProcessor processor)
        {
            int mailID = processor.getAvailableMailID();

            Mailbox.SaveOutMail(m_characterID, m_mailSubject, m_mailMessage, m_gold, m_isAttachedItem, mailID, -1, m_senderName, false);   

            if (m_isAttachedItem > 0)
            {
                string insertString = "(mail_id," + m_item.GetInsertFieldsString() + ") values (" + mailID + "," + m_item.GetInsertValuesString((int)m_characterID) + ")";
                Program.processor.m_worldDB.runCommandSync("insert into mail_inventory " + insertString);
            }

            Mailbox.NotifyOnlinePlayerWithCharacterID(-1, m_characterID);
        }
    }
}

