using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Localise;

namespace MainServer.player_offers
{
    // refactor to receiptFyber if we're keeping the classes separate
    internal class FyberReceipt
    {
        string m_orderID = string.Empty;
        int m_accountID = -1;
        int m_rewardAmount = 0;

        internal FyberReceipt(string orderID, int accountID, int rewardAmount) 
        {
            m_orderID = orderID;
            m_accountID = accountID;
            m_rewardAmount = rewardAmount;
        }

        internal string OrderID { get { return m_orderID; } }
        internal int AccountID { get { return m_accountID; } }
        internal int RewardAmount { get { return m_rewardAmount; } }
    }

    internal class FyberPlayer
    {
        Player m_player = null;
        List<FyberReceipt> m_receipts = new List<FyberReceipt>();

        internal FyberPlayer(Player player)
        {
            m_player = player;
        }

        internal List<FyberReceipt> Receipts
        {
            get { return m_receipts; }
        }

        internal Player CurrentPlayer
        {
            get { return m_player; }
        }

        internal static FyberPlayer GetPlayerForID(List<FyberPlayer> playerList, int accountID)
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

    internal class FyberOfferController
    {
		// #localisation
		public class FyberOfferTextDB : TextEnumDB
		{
			public FyberOfferTextDB() : base(nameof(FyberOfferController), typeof(TextID)) { }

			public enum TextID
			{
				RECEIVED_PLATINUM_FYBER    // "Received {amount0} Platinum from Fyber"
			}
		}
		public static FyberOfferTextDB textDB = new FyberOfferTextDB();

		const string k_callbacksTableName = "fyber_offer_callbacks";
        const int TIME_BETWEEN_CHECKS_SECONDS = 60;

        internal List<FyberReceipt> m_pendingReceipts = new List<FyberReceipt>();
        internal bool m_searchActive = false;
        DateTime m_dataLastCheckedTime = DateTime.Now;


        internal FyberOfferController()
        {
        }

        internal void Update()
        {
            DateTime currentTime = DateTime.Now;
            if (m_searchActive == false)
            {

                List<FyberReceipt> receiptsToDealWith = new List<FyberReceipt>();
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
                else //no pending receipts them check how long since the last check
                {
                    if ((currentTime - m_dataLastCheckedTime).TotalSeconds > TIME_BETWEEN_CHECKS_SECONDS)
                    {
                        ReadFyberOrdersTask newTask = new ReadFyberOrdersTask(Program.m_worldID, k_callbacksTableName);
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

        void TryToAwardPlatinumOnline(List<FyberReceipt> receiptsToDealWith)
        {
            List<FyberReceipt> offlineReceipts = new List<FyberReceipt>();
            List<FyberPlayer> onlinePlayers = new List<FyberPlayer>();
            for (int i = 0; i < receiptsToDealWith.Count; i++)
            {
                FyberReceipt currentReceipt = receiptsToDealWith[i];
                //have we already dealt with a receipt for this player
                FyberPlayer fyberPlayer = FyberPlayer.GetPlayerForID(onlinePlayers, currentReceipt.AccountID);

                if (fyberPlayer == null)
                {
                    //are they logged in
                    Player livePlayer = Program.processor.getPlayerFromAccountId(currentReceipt.AccountID);
                    //if logged in create a Trialpay player to hold the receipts
                    if (livePlayer != null)
                    {
                        fyberPlayer = new FyberPlayer(livePlayer);

                        onlinePlayers.Add(fyberPlayer);
                    }
                }
                if (fyberPlayer != null)
                {
                    fyberPlayer.Receipts.Add(currentReceipt);
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
                offlineUpdateString += "'" + offlineReceipts[i].OrderID + "'" ;
            }
            //prepare the list of receipts for online players
            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                FyberPlayer currentPlayer = onlinePlayers[i];
                int PlatToAdd = 0;
                for (int j = 0; j < currentPlayer.Receipts.Count; j++)
                {
                    FyberReceipt currentReceipt = currentPlayer.Receipts[j];
                    if (onlineUpdateString.Length > 0)
                    {
                        onlineUpdateString += ",";
                    }
                    onlineUpdateString += "'" + currentReceipt.OrderID + "'";
                    PlatToAdd += currentReceipt.RewardAmount;
                }
                currentPlayer.CurrentPlayer.m_platinum += PlatToAdd;

                transactionList.Add("update account_details set platinum=" + currentPlayer.CurrentPlayer.m_platinum + " where account_id=" + currentPlayer.CurrentPlayer.m_account_id);
                //send down the new platinum value without the inventory
                PremiumShop.SendPlatinumConfirmation(currentPlayer.CurrentPlayer, 1, "", "");
				string locText = Localiser.GetString(textDB, currentPlayer.CurrentPlayer, (int)FyberOfferTextDB.TextID.RECEIVED_PLATINUM_FYBER);
				locText = String.Format(locText, PlatToAdd);
				Program.processor.sendSystemMessage(locText, currentPlayer.CurrentPlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
			}

			if (offlineUpdateString.Length > 0)
            {
                // prevent server ticks from rechecking this account, they'll be sent their rewards on login
                transactionList.Add("update " + k_callbacksTableName + " set world_id=0 where " + ReadFyberOrdersTask.k_transactionColumnName + " in (" + offlineUpdateString + ")");
            }

            if (onlineUpdateString.Length > 0)
            {
                transactionList.Add("update " + k_callbacksTableName + " set world_id=0, rewarded = 1 where " + ReadFyberOrdersTask.k_transactionColumnName + " in (" + onlineUpdateString + ")");
            }

            Program.processor.m_universalHubDB.runCommandsInTransaction(transactionList);

            Program.DisplayDelayed("fyber orders processed:" + receiptsToDealWith.Count);
        }
    }
}
