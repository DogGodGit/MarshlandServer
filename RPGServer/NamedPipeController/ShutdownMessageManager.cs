using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lidgren.Network;
using Newtonsoft.Json;
using MainServer.Localise;

namespace MainServer
{
    class ShutdownMessageManager
	{
		// #localisation
		public class ShutdownMessageManagerTextDB : TextEnumDB
		{
			public ShutdownMessageManagerTextDB() : base(nameof(ShutdownMessageManager), typeof(TextID)) { }

			public enum TextID
			{
				SHUTDOWN_IN_MINUTE,           // "{shutdownMessage0} in {totalMinutes1} minute."
				SHUTDOWN_IN_MINUTES           // "{shutdownMessage0} in {totalMinutes1} minutes."
			}
		}
		public static ShutdownMessageManagerTextDB textDB = new ShutdownMessageManagerTextDB();

		private readonly DateTime shutdownRecievedAt;
        private readonly DateTime shutdownAt;
        private readonly double updateInterval;
        private readonly string shutdownMessage;
        private DateTime timeForNextUpdate;

        internal ShutdownMessageManager(int shutdownIn, int interval, string message)
        {
            shutdownRecievedAt = DateTime.Now;
            shutdownAt = shutdownRecievedAt + TimeSpan.FromMinutes(shutdownIn);
            updateInterval = interval;
            shutdownMessage = message;

            if (!String.IsNullOrEmpty(shutdownMessage) && (int)updateInterval > 0) // if the update interval is 0, send the message telling players the server is coming down once only
            {
                SendMessage();
            }
        }

        internal bool Update()
        {
            if (DateTime.Now > shutdownAt)
            {
                Application.Exit();
                return false; // stop running the client thread
            }
            if (updateInterval == 0)
            {
                return true;
            }
            if (timeForNextUpdate < DateTime.Now)
            {
                timeForNextUpdate = DateTime.Now + TimeSpan.FromMinutes(updateInterval);
                SendMessage();
                return true;
            }
            return true;

        }

        private void SendMessage()
        {
            TimeSpan timeTillShutdown = shutdownAt - DateTime.Now;

            if (timeTillShutdown.TotalMinutes > 1)
            {
				sendShutdownSystemMessage(textDB, (int)ShutdownMessageManagerTextDB.TextID.SHUTDOWN_IN_MINUTES, string.Format("{0:0}", timeTillShutdown.TotalMinutes));
            }
            else
            {
				sendShutdownSystemMessage(textDB, (int)ShutdownMessageManagerTextDB.TextID.SHUTDOWN_IN_MINUTE, string.Format("{0:0}", timeTillShutdown.TotalMinutes));
			}
            //Program.sendSystemMessage(customMessage, true, false);
        }

		private void sendShutdownSystemMessage(TextEnumDB textDB, int textID, string shutDownTime)
		{
			String message;
			for (int i = 0; i < Program.MainForm.lvPlayerList.Items.Count; i++)
			{
				if (Program.MainForm.lvPlayerList.Items[i].Tag == null)
				{
					continue;
				}

				Player player = (Player)Program.MainForm.lvPlayerList.Items[i].Tag;
				message = Localiser.GetString(textDB, player, textID);
				message = String.Format(message, shutDownTime);
				Program.processor.sendSystemMessage(message, player, false, SYSTEM_MESSAGE_TYPE.POPUP);
			}
		}

	}
}
