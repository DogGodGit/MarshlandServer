using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Localise;

namespace MainServer.player_offers
{
    class TrialpayReceipt
    {
        string m_orderID = "";
        int m_accountID = -1;
        int m_rewardAmount = 0;

        internal TrialpayReceipt(string orderID,int accountID,int rewardAmount ) 
        {
            m_orderID = orderID;
            m_accountID = accountID;
            m_rewardAmount = rewardAmount;
        }

        internal string OrderID { get { return m_orderID; } }
        internal int AccountID { get { return m_accountID; } }
        internal int RewardAmount { get { return m_rewardAmount; } }
    }
    class TrialpayPlayer 
    {

        Player m_player = null;
        List<TrialpayReceipt> m_receipts = new List<TrialpayReceipt>();

        internal TrialpayPlayer(Player player)
        {
            m_player = player;
        }

        internal List<TrialpayReceipt> Receipts
        {
            get { return m_receipts; }
        }
        internal Player CurrentPlayer
        {
            get { return m_player; }
        }
        internal static TrialpayPlayer GetTrialpayPlayerForID(List<TrialpayPlayer> playerList, int accountID)
        {
            for (int i = 0;i< playerList.Count; i++)
            {
                if (playerList[i].CurrentPlayer.m_account_id == accountID)
                {
                    return playerList[i];
                }
            }
            return null;
        }
    }
    class TrialpayController
    {
		// #localisation
		public class TrialpayTextDB : TextEnumDB
		{
			public TrialpayTextDB() : base(nameof(TrialpayController), typeof(TextID)) { }

			public enum TextID
			{
				RECEIVED_PLATINUM_TRIALPAY    // "Received {amount0} Platinum from Trialpay"
			}
		}
		public static TrialpayTextDB textDB = new TrialpayTextDB();

		const int TIME_BETWEEN_CHECKS_SECONDS = 60;
        internal List<TrialpayReceipt> m_pendingReceipts = new List<TrialpayReceipt>();
        internal bool m_searchActive = false;
        DateTime m_dataLastCheckedTime = DateTime.Now;

        internal TrialpayController()
        {

        }

        internal void Update()
        {
            DateTime currentTime = DateTime.Now;
            if (m_searchActive == false)
            {
                
                List<TrialpayReceipt> receiptsToDealWith = new List<TrialpayReceipt>();
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
                        ReadTrialPayOrdersTask newTask = new ReadTrialPayOrdersTask(Program.m_worldID);
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
        void TryToAwardPlatinumOnline(List<TrialpayReceipt> receiptsToDealWith)
        {
            List<TrialpayReceipt> offlineReceipts = new List<TrialpayReceipt>();
            List<TrialpayPlayer> onlinePlayers = new List<TrialpayPlayer>();
            for (int i = 0; i < receiptsToDealWith.Count; i++)
            {
                TrialpayReceipt currentReceipt = receiptsToDealWith[i];
                //have we already dealt with a receipt for this player
                TrialpayPlayer trialpayPlayer = TrialpayPlayer.GetTrialpayPlayerForID(onlinePlayers, currentReceipt.AccountID);

                if (trialpayPlayer == null)
                {
                    //are they logged in
                    Player livePlayer = Program.processor.getPlayerFromAccountId(currentReceipt.AccountID);
                    //if logged in create a Trialpay player to hold the receipts
                    if (livePlayer != null)
                    {
                        trialpayPlayer = new TrialpayPlayer(livePlayer);

                        onlinePlayers.Add(trialpayPlayer);
                    }
                }
                if (trialpayPlayer != null)
                {
                    trialpayPlayer.Receipts.Add(currentReceipt);
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
                offlineUpdateString += "\"" + offlineReceipts[i].OrderID + "\"";
            }
            //prepare the list of receipts for online players
            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                TrialpayPlayer currentPlayer = onlinePlayers[i];
                int PlatToAdd = 0;
                for (int j = 0; j < currentPlayer.Receipts.Count; j++)
                {
                    TrialpayReceipt currentReceipt = currentPlayer.Receipts[j];
                    if (onlineUpdateString.Length > 0)
                    {
                        onlineUpdateString += ",";
                    }
                    onlineUpdateString += "\"" + currentReceipt.OrderID + "\"";
                    PlatToAdd += currentReceipt.RewardAmount;
                }
                currentPlayer.CurrentPlayer.m_platinum += PlatToAdd;

                transactionList.Add("update account_details set platinum=" + currentPlayer.CurrentPlayer.m_platinum + " where account_id=" + currentPlayer.CurrentPlayer.m_account_id);
                //send down the new platinum value without the inventory
                PremiumShop.SendPlatinumConfirmation(currentPlayer.CurrentPlayer, 1, "", "");

				string locText = Localiser.GetString(textDB, currentPlayer.CurrentPlayer, (int)TrialpayTextDB.TextID.RECEIVED_PLATINUM_TRIALPAY);
				locText = String.Format(locText, PlatToAdd);
				Program.processor.sendSystemMessage(locText, currentPlayer.CurrentPlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);

				//PremiumShop.SendItemBuyReply(currentPlayer.CurrentPlayer, "received "+PlatToAdd+" Platinum from Trialpay", false);
			}
            if (offlineUpdateString.Length > 0)
            {
                transactionList.Add("update trialpay_orders set world_id=0 where order_id in (" + offlineUpdateString + ")");
            }
            if (onlineUpdateString.Length > 0)
            {
                transactionList.Add("update trialpay_orders set world_id=0, rewarded = 1 where order_id in (" + onlineUpdateString + ")");
            }
            Program.processor.m_universalHubDB.runCommandsInTransaction(transactionList);
        
            Program.DisplayDelayed("trialpay orders processed:"+receiptsToDealWith.Count);
            
        }


    }
}
