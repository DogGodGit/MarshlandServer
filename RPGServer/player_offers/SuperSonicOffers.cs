using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Localise;

namespace MainServer.player_offers
{
    class SuperSonicReceipt
    {
        string m_orderID = "";
        int m_accountID = -1;
        int m_rewardAmount = 0;
        internal SuperSonicReceipt(string orderID, int accountID, int rewardAmount) 
        {
            m_orderID = orderID;
            m_accountID = accountID;
            m_rewardAmount = rewardAmount;
        }
        internal string OrderID { get { return m_orderID; } }
        internal int AccountID { get { return m_accountID; } }
        internal int RewardAmount { get { return m_rewardAmount; } }
    }
    class SuperSonicPlayer
    {

        Player m_player = null;
        List<SuperSonicReceipt> m_receipts = new List<SuperSonicReceipt>();

        internal SuperSonicPlayer(Player player)
        {
            m_player = player;
        }

        internal List<SuperSonicReceipt> Receipts
        {
            get { return m_receipts; }
        }
        internal Player CurrentPlayer
        {
            get { return m_player; }
        }
        internal static SuperSonicPlayer GetSuperSonicPlayerForID(List<SuperSonicPlayer> playerList, int accountID)
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
    class SuperSonicOfferController
    {
		// #localisation
		public class SuperSonicOfferTextDB : TextEnumDB
		{
			public SuperSonicOfferTextDB() : base(nameof(SuperSonicOfferController), typeof(TextID)) { }

			public enum TextID
			{
				RECEIVED_PLATINUM_SUPERSONIC,		// "Received {amount0} Platinum from SuperSonic"
			}
		}
		public static SuperSonicOfferTextDB textDB = new SuperSonicOfferTextDB();

		const int TIME_BETWEEN_CHECKS_SECONDS = 60;
        internal List<SuperSonicReceipt> m_pendingReceipts = new List<SuperSonicReceipt>();
        internal bool m_searchActive = false;
        DateTime m_dataLastCheckedTime = DateTime.Now;

        internal SuperSonicOfferController()
        {

        }

        internal void Update()
        {
            DateTime currentTime = DateTime.Now;
            if (m_searchActive == false)
            {

                List<SuperSonicReceipt> receiptsToDealWith = new List<SuperSonicReceipt>();
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
                        ReadSuperSonicOrdersTask newTask = new ReadSuperSonicOrdersTask(Program.m_worldID);
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
        void TryToAwardPlatinumOnline(List<SuperSonicReceipt> receiptsToDealWith)
        {
            List<SuperSonicReceipt> offlineReceipts = new List<SuperSonicReceipt>();
            List<SuperSonicPlayer> onlinePlayers = new List<SuperSonicPlayer>();
            int receiptsHandled = 0;
            for (int i = 0; i < receiptsToDealWith.Count; i++)
            {
                SuperSonicReceipt currentReceipt = receiptsToDealWith[i];
                //have we already dealt with a receipt for this player
                SuperSonicPlayer superSonicPlayer = SuperSonicPlayer.GetSuperSonicPlayerForID(onlinePlayers, currentReceipt.AccountID);

                if (superSonicPlayer == null)
                {
                    //are they logged in
                    Player livePlayer = Program.processor.getPlayerFromAccountId(currentReceipt.AccountID);
                    //if logged in create a Trialpay player to hold the receipts
                    if (livePlayer != null)
                    {
                        superSonicPlayer = new SuperSonicPlayer(livePlayer);

                        onlinePlayers.Add(superSonicPlayer);
                    }
                }
                if (superSonicPlayer != null)
                {
                    superSonicPlayer.Receipts.Add(currentReceipt);
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
                SuperSonicPlayer currentPlayer = onlinePlayers[i];
                int PlatToAdd = 0;
                for (int j = 0; j < currentPlayer.Receipts.Count; j++)
                {
                    SuperSonicReceipt currentReceipt = currentPlayer.Receipts[j];
                    if (onlineUpdateString.Length > 0)
                    {
                        onlineUpdateString += "','";
                    }
                    onlineUpdateString += currentReceipt.OrderID ;
                    PlatToAdd += currentReceipt.RewardAmount;
                }
                currentPlayer.CurrentPlayer.m_platinum += PlatToAdd;
                currentPlayer.CurrentPlayer.m_platRewarded += PlatToAdd;
                transactionList.Add("update account_details set platinum=" + currentPlayer.CurrentPlayer.m_platinum + ", plat_rewarded=" + currentPlayer.CurrentPlayer.m_platRewarded + " where account_id=" + currentPlayer.CurrentPlayer.m_account_id);
                //send down the new platinum value without the inventory
                PremiumShop.SendPlatinumConfirmation(currentPlayer.CurrentPlayer, 1, "", "");

				string locText = Localiser.GetString(textDB, currentPlayer.CurrentPlayer, (int)SuperSonicOfferTextDB.TextID.RECEIVED_PLATINUM_SUPERSONIC);
				locText = String.Format(locText, PlatToAdd);
				Program.processor.sendSystemMessage(locText, currentPlayer.CurrentPlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);

				if (onlineUpdateString.Length > 0)
                {
                    transactionList.Add("update supersonic_offer_callbacks set rewarded = 1 where event_id in ('" + onlineUpdateString + "')");
                }
                receiptsHandled += 1;
            }

            Program.processor.m_universalHubDB.runCommandsInTransaction(transactionList);

            Program.DisplayDelayed("Supersonic receipts handled:" + receiptsHandled);

        }


    }
}
