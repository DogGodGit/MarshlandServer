using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Net;

namespace MainServer
{

    class SMTPHandler
    {
        string m_email_server;
        public SMTPHandler(string email_server)
        {
            m_email_server = email_server;
        }
        
        internal bool sendMail(string toStr,string fromStr,string ccStr,string bccStr,string msgSubject,string msgBody,string msgAttachments){
            if (m_email_server == "")
            {
                return false;
            }
            MailMessage objMsg=new MailMessage();
            string[] recipients = toStr.Split(new char[] { ';' });
            if (recipients.Length == 0)
            {
                Program.Display( "no recipients");
                return false;
            }
            for (int i = 0; i < recipients.Length; i++)
            {
                if (ValidateEmail(recipients[i]) == false)
                {
                    Program.Display("invalid recipients:" + recipients[i] + ":" + msgSubject);
                    return false;
                }
                objMsg.To.Add(new MailAddress(recipients[i]));
            }
            if (ccStr != "")
            {
                string[] cc = ccStr.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < cc.Length; i++)
                {
                    if (ValidateEmail(cc[i]) == false && cc[i] != "")
                    {
                        Program.Display("invalid cc recipients");

                        return false;
                    }
                    objMsg.CC.Add(new MailAddress(cc[i]));
                }
            }
            if (bccStr != "")
            {
                string[] bcc = bccStr.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < bcc.Length; i++)
                {
                    if (ValidateEmail(bcc[i]) == false)
                    {
                        Program.Display("invalid bcc recipients");

                        return false;
                    }
                    objMsg.Bcc.Add(new MailAddress(bcc[i]));
                }
            }
            if (msgAttachments != "")
            {
                string[] attachments = msgAttachments.Split(new char[] { ';' },StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < attachments.Length; i++)
                {
                    if (attachments[i].Trim().Length>0)
                    {
                        objMsg.Attachments.Add(new Attachment(attachments[i]));
                    }
                }
            }
            objMsg.From = new MailAddress(fromStr);
            objMsg.Subject=msgSubject;
            objMsg.IsBodyHtml = true;
            objMsg.Body=msgBody;

            SmtpClient client = new SmtpClient(m_email_server);

            Program.Display("sending mail to " + toStr + " subject " + msgSubject);
            try
            {
                client.Credentials = CredentialCache.DefaultNetworkCredentials;
                client.Timeout = 10000;   
                client.Send(objMsg);
                Program.Display("sent");
                return true;
            }
            catch(SmtpException e)
            {
                Program.Display("email error:" + e);
                return false;

            }
    }

        public bool ValidateEmail(string sEmail)
        {

            Regex exp = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");

            Match m = exp.Match(sEmail);

            if (m.Success && m.Value.Equals(sEmail)) return true;

            else return false;

        }

    }
}
