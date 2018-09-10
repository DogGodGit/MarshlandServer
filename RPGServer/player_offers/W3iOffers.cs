using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Localise;

namespace MainServer.player_offers
{
    class W3iReceipt
    {
        int m_orderID = 0;
        int m_accountID = -1;
        int m_rewardAmount = 0;
        internal W3iReceipt(int orderID, int accountID, int rewardAmount) 
        {
            m_orderID = orderID;
            m_accountID = accountID;
            m_rewardAmount = rewardAmount;
        }
        internal int OrderID { get { return m_orderID; } }
        internal int AccountID { get { return m_accountID; } }
        internal int RewardAmount { get { return m_rewardAmount; } }
    }
    class W3iPlayer
    {

        Player m_player = null;
        List<W3iReceipt> m_receipts = new List<W3iReceipt>();

        internal W3iPlayer(Player player)
        {
            m_player = player;
        }

        internal List<W3iReceipt> Receipts
        {
            get { return m_receipts; }
        }
        internal Player CurrentPlayer
        {
            get { return m_player; }
        }
        internal static W3iPlayer GetW3iPlayerForID(List<W3iPlayer> playerList, int accountID)
        {
            for (int i = 0; i < playerList.Count; i++)
            {
                if (playerList[i].CurrentPlayer.m_account_id == accountID)
                {
                    return playerList[i];
                }
            }
            return null;
        }
    }
    class W3iOfferController
    {
		// #localisation
		public class W3iTextDB : TextEnumDB
		{
			public W3iTextDB() : base(nameof(W3iOfferController), typeof(TextID)) { }

			public enum TextID
			{
				RECEIVED_PLATINUM_NATIVEX   // "Received {amount0} Platinum from NativeX"
			}
		}
		public static W3iTextDB textDB = new W3iTextDB();

		const int TIME_BETWEEN_CHECKS_SECONDS = 60;
        internal List<W3iReceipt> m_pendingReceipts = new List<W3iReceipt>();
        internal bool m_searchActive = false;
        DateTime m_dataLastCheckedTime = DateTime.Now;

        internal W3iOfferController()
        {

        }

        internal void Update()
        {
            DateTime currentTime = DateTime.Now;
            if (m_searchActive == false)
            {

                List<W3iReceipt> receiptsToDealWith = new List<W3iReceipt>();
                lock (m_pendingReceipts)
                {
                    if (m_pendingReceipts.Count > 0)
                    {
                        receiptsToDealWith.AddRange(m_pendingReceipts);
                        m_pendingReceipts.Clear();
                    }
                }
                if (receiptsToDealWith.Count > 0)
                {
                    TryToAwardPlatinumOnline(receiptsToDealWith);
                    m_dataLastCheckedTime = DateTime.Now;
                }
                //no pending receipts them check how long since the last check
                else
                {
                    if ((currentTime - m_dataLastCheckedTime).TotalSeconds > TIME_BETWEEN_CHECKS_SECONDS)
                    {
                        ReadW3iOrdersTask newTask = new ReadW3iOrdersTask(Program.m_worldID);
                        m_dataLastCheckedTime = DateTime.Now;
                        m_searchActive = true;
                        lock (Program.processor.m_backgroundTasks)
                        {
                            Program.processor.m_backgroundTasks.Enqueue(newTask);
                        }
                    }
                }
            }
        }
        void TryToAwardPlatinumOnline(List<W3iReceipt> receiptsToDealWith)
        {
            List<W3iReceipt> offlineReceipts = new List<W3iReceipt>();
            List<W3iPlayer> onlinePlayers = new List<W3iPlayer>();
            for (int i = 0; i < receiptsToDealWith.Count; i++)
            {
                W3iReceipt currentReceipt = receiptsToDealWith[i];
                //have we already dealt with a receipt for this player
                W3iPlayer w3iPlayer = W3iPlayer.GetW3iPlayerForID(onlinePlayers, currentReceipt.AccountID);

                if (w3iPlayer == null)
                {
                    //are they logged in
                    Player livePlayer = Program.processor.getPlayerFromAccountId(currentReceipt.AccountID);
                    //if logged in create a Trialpay player to hold the receipts
                    if (livePlayer != null)
                    {
                        w3iPlayer = new W3iPlayer(livePlayer);

                        onlinePlayers.Add(w3iPlayer);
                    }
                }
                if (w3iPlayer != null)
                {
                    w3iPlayer.Receipts.Add(currentReceipt);
                }
                else
                {
                    //the player is not on this server
                    offlineReceipts.Add(currentReceipt);
                }
            }

            List<string> transactionList = new List<string>();
            string offlineUpdateString = "";
            string onlineUpdateString = "";
            //prepare the list of receipts for offline players
            for (int i = 0; i < offlineReceipts.Count; i++)
            {
                if (offlineUpdateString.Length > 0)
                {
                    offlineUpdateString += ",";
                }
                offlineUpdateString += offlineReceipts[i].OrderID ;
            }
            //prepare the list of receipts for online players
            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                W3iPlayer currentPlayer = onlinePlayers[i];
                int PlatToAdd = 0;
                for (int j = 0; j < currentPlayer.Receipts.Count; j++)
                {
                    W3iReceipt currentReceipt = currentPlayer.Receipts[j];
                    if (onlineUpdateString.Length > 0)
                    {
                        onlineUpdateString += ",";
                    }
                    onlineUpdateString += currentReceipt.OrderID ;
                    PlatToAdd += currentReceipt.RewardAmount;
                }
                currentPlayer.CurrentPlayer.m_platinum += PlatToAdd;

                transactionList.Add("update account_details set platinum=" + currentPlayer.CurrentPlayer.m_platinum + " where account_id=" + currentPlayer.CurrentPlayer.m_account_id);
                //send down the new platinum value without the inventory
                PremiumShop.SendPlatinumConfirmation(currentPlayer.CurrentPlayer, 1, "", "");

				string locText = Localiser.GetString(textDB, currentPlayer.CurrentPlayer, (int)W3iTextDB.TextID.RECEIVED_PLATINUM_NATIVEX);
				locText = String.Format(locText, PlatToAdd);
				Program.processor.sendSystemMessage(locText, currentPlayer.CurrentPlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);

				//PremiumShop.SendItemBuyReply(currentPlayer.CurrentPlayer, "received "+PlatToAdd+" Platinum from Trialpay", false);
			}
            if (offlineUpdateString.Length > 0)
            {
                transactionList.Add("update w3i_offer_callbacks set world_id=0 where offer_callback_id in (" + offlineUpdateString + ")");
            }
            if (onlineUpdateString.Length > 0)
            {
                transactionList.Add("update w3i_offer_callbacks set world_id=0, rewarded = 1 where offer_callback_id in (" + onlineUpdateString + ")");
            }
            Program.processor.m_universalHubDB.runCommandsInTransaction(transactionList);

            Program.DisplayDelayed("w3i orders processed:" + receiptsToDealWith.Count);

        }


    }
}
