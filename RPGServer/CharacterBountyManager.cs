using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using Newtonsoft.Json.Serialization;
using MainServer.Localise;

namespace MainServer
{
    class Bounty
    {
        public enum tStatus
        {
            NotReady = 0,
            Purchasable = 1,
            Available = 2,
            Started = 3,
            Completed = 4,
            HandedIn = 5,
            Dropped = 6
        }

        public tStatus Status { get; set; }
        public int Cost { get; set; }
        public DateTime LastRerolled { get; set; }

        public Bounty()
        {
            Status = tStatus.NotReady;
        }
    }

    class CharacterBountyManager
    {

		// #localisation
		public class CharacterBountyManagerTextDB : TextEnumDB
		{
			public CharacterBountyManagerTextDB() : base(nameof(CharacterBountyManager), typeof(TextID)) { }

			public enum TextID
			{
				ITEM_RETURNED,      // "Due to this error, a {itemName0} has been returned to your inventory"
			}
		}
		public static CharacterBountyManagerTextDB textDB = new CharacterBountyManagerTextDB();

        public Dictionary<int, Bounty> mBounties;
        private Character mOwningCharacter;
        private AnalyticsMain logAnalytics;

        public CharacterBountyManager()
        {
            mBounties = new Dictionary<int, Bounty>();
            logAnalytics = new AnalyticsMain(true);
        }

        internal void SetUp(Character owner)
        {
            mOwningCharacter = owner;
            RefreshBounties();
        }

        public void RerollBounties()
        {
            if (mOwningCharacter.Level >= ServerBountyManager.MinimumLevelRequiredForBounties)
            {
                var BountysThatNeedRerolled = new List<KeyValuePair<int, Bounty>>();

                for (int i = 0; i < mBounties.Count; i++)
                {
                    var bounty = mBounties.ElementAt(i).Value;
                    if (bounty.Cost > 0 && bounty.LastRerolled < ServerBountyManager.GetLastClearTime()
                        && (bounty.Status == Bounty.tStatus.Available || bounty.Status == Bounty.tStatus.Purchasable)
                        ||
                        (!ServerBountyManager.CheckBountyLevelIsWithinPlayerLevel(mOwningCharacter.Level,
                            mBounties.ElementAt(i).Key)
                         && (bounty.Status == Bounty.tStatus.Available || bounty.Status == Bounty.tStatus.Purchasable || bounty.Status == Bounty.tStatus.NotReady)))
                        // check if the purchased but not started bounty needs rerolled because of the date
                    {
                        BountysThatNeedRerolled.Add(mBounties.ElementAt(i));
                        mBounties.Remove(mBounties.ElementAt(i).Key);
                        i--;
                    }
                }

                if (BountysThatNeedRerolled.Count > 0)
                    // don't write to db + try to select stuff if there's nothing to reroll!
                {
                    ReplaceBountiesWithRandom(BountysThatNeedRerolled);
                    DeleteReplacedBountiesToDB(BountysThatNeedRerolled);
                    WriteToDB();
                }
            }
            UpdateBounties();
        }

        private void ReplaceBountiesWithRandom(List<KeyValuePair<int, Bounty>> replacedBounties)
        {
            int i = 0;
            // Get a list of available bounties (which may or may not match the requested number) and store them
            ServerBountyManager.SelectRandomBounties(mOwningCharacter, replacedBounties.Count, this,
                delegate(int questID)
                {
                    if (mBounties.Count < ServerBountyManager.MaxTotalBounties)
                    {
                        // Free bounties are immediately available, while paid need to be bought before becoming available.
                        if (i < replacedBounties.Count)
                        {
                            mBounties[questID] = new Bounty
                            {
                                Cost = replacedBounties[i].Value.Cost,
                                Status = replacedBounties[i].Value.Status,
                                LastRerolled = DateTime.Now         
                            };
                            i++;
                        }
                    }
                });
        }

		private void SendLocaliseBounty(Player player, ref string msg, KeyValuePair<int, Bounty> bounty)
		{
			int questID = bounty.Key;
			QuestTemplate questTemplate = Program.processor.m_QuestTemplateManager.GetQuestTemplate(questID);

			if (questTemplate != null)
			{
				msg += "~^" + QuestTemplateManager.GetLocaliseQuestName(player, questID) + "^" + questID + "^" + bounty.Value.Cost + "^" + (int)bounty.Value.Status + "^~";
			}
		}

		public void PurchaseBounty(Player player)
        {
            // A request to add a paid quest hs been received.
            // See if we have any paid bounties available to start - we'll pick the 1st one if we do.
            Func<KeyValuePair<int, Bounty>, bool> pred = (b => b.Value.Cost != 0 && b.Value.Status == Bounty.tStatus.Purchasable);
            if (!mBounties.Any(pred))
                return;

            // Set the first found bounty to 
            KeyValuePair<int, Bounty> result = mBounties.First(pred);

            int questID = result.Key;
            mBounties[questID].Status = Bounty.tStatus.Available;
            logAnalytics.BountyTracking(mOwningCharacter.m_player, mOwningCharacter.m_character_id, questID, BountyTrackingStatus.Purchased);

            WriteToDB();
            SendBounties(player);
        }

        // Send back to the client information about bounty board quests
        public void SendBounties(Player player)
        {
            if (player == null)
                return;

            StringBuilder builder = new StringBuilder();
            builder.Append("~").Append(ServerBountyManager.PaidItemID);
            builder.Append("~").Append(ServerBountyManager.MaxTotalBounties);
            builder.Append("~").Append(ServerBountyManager.MaxConcurrentBounties);
            builder.Append("~").Append(ServerBountyManager.MaxFreeBounties);
            builder.Append("~").Append(ServerBountyManager.MaxPaidBounties);
            builder.Append("~").Append(ServerBountyManager.MinimumLevelRequiredForBounties).Append("~");
            string bountyInfo = builder.ToString();
            NetOutgoingMessage outmsg = Program.processor.m_server.CreateMessage();

            foreach (KeyValuePair<int, Bounty> bounty in mBounties)
            {
				SendLocaliseBounty(player, ref bountyInfo, bounty);
                //SendBounty(ref bountyInfo, bounty);
            }

            outmsg.WriteVariableUInt32((uint)NetworkCommandType.RecServerDynamicData);
            outmsg.Write((byte)0);  // PDH - ? Session id, apparantly
            outmsg.Write(ServerBountyManager.BB_BOUNTY_DATA + bountyInfo);
            Program.processor.SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RecServerDynamicData);
        }

        // Bounty Board is notified about quest completion here.
        public void QuestComplete(int questID)
        {
            // Quest has been completed - is it in the bounty list?
            Bounty bounty;
            if (!mBounties.TryGetValue(questID, out bounty))
                return;

            bounty.Status = Bounty.tStatus.HandedIn;
            logAnalytics.BountyTracking(mOwningCharacter.m_player, mOwningCharacter.m_character_id, questID, BountyTrackingStatus.Completed);

            // Do a full update, in case completing this quest means
            // we have a free slot for a new bounty.
            UpdateBounties();
        }
        // Bounty Board is notified about quest start here.
        public bool QuestStart(int questID)
        {
            // Quest has been started - is it in the bounty list?
            Bounty bounty;
            if (!mBounties.TryGetValue(questID, out bounty))
                return true;

            int concurrentBounties = 0; // check it's not over the max concurrent before starting the quest
            foreach (var bountyCheck in mBounties)
            {
                if (bountyCheck.Value.Status == Bounty.tStatus.Started || bountyCheck.Value.Status == Bounty.tStatus.Completed)
                {
                    concurrentBounties++;
                    if (concurrentBounties > ServerBountyManager.MaxConcurrentBounties)
                    {
                        return false;
                    }
                }
            }

            bounty.Status = Bounty.tStatus.Started;
            logAnalytics.BountyTracking(mOwningCharacter.m_player, mOwningCharacter.m_character_id, questID, BountyTrackingStatus.Claimed);

            // Update 
            UpdateBounties();
            return true;
        }

        // Check we don't need to add any new bounties.
        public void UpdateBounties()
        {
            // Count how many free bounties we have completed , and add some if needed.
            int numFree = ServerBountyManager.MaxFreeBounties - CountFree();
            if (numFree >= 0)
            {
                AddNewBounties(numFree, 0);
            }
            else
            {
                DeleteOldBounties(numFree, 0);
            }

            // Count how many paid bounties we want to offer
            int numPaid = ServerBountyManager.MaxPaidBounties - CountPaid();
            if (numPaid >= 0)
            {
                AddNewBounties(numPaid, ServerBountyManager.PaidCost);
            }
            else
            {
                DeleteOldBounties(numPaid, ServerBountyManager.PaidCost);
            }

            CheckConcurrentBounties();

            SendBounties(mOwningCharacter.m_player);
        }

        private void DeleteOldBounties(int numFree, int cost)
        {
            var deletionSqlCommandBuilder = new StringBuilder("DELETE FROM bounties where ");
            deletionSqlCommandBuilder.Append("character_id = ").Append(mOwningCharacter.m_character_id).Append(" AND (");
            for (int i = 0; i < mBounties.Count && numFree < 0; i++)
            {
                var bounty = mBounties.ElementAt(i);
                if (bounty.Value.Cost == cost)
                {
                    switch (cost)
                    {
                        case 0:
                            if (bounty.Value.Status < Bounty.tStatus.Started)
                            {
                                WriteDeletionString(bounty, deletionSqlCommandBuilder);
                                mBounties.Remove(bounty.Key);
                                numFree++;
                            }
                            break;
                        default: // for any cost > 0
                            if (bounty.Value.Status < Bounty.tStatus.Available)
                            {
                                WriteDeletionString(bounty, deletionSqlCommandBuilder);
                                mBounties.Remove(bounty.Key);
                                numFree++;
                            }
                            break;
                    }
                }
            }

            string deleteSqlCommand = deletionSqlCommandBuilder.ToString();
            deleteSqlCommand = deleteSqlCommand.Substring(0, deleteSqlCommand.Count() - 4);
                // take the last OR statement out
            deleteSqlCommand += ");";

            if (deleteSqlCommand.Contains("quest_id")) // catch incase it's not found any bounties to delete, don't bother running the command
            {
                mOwningCharacter.m_db.runCommandSync(deleteSqlCommand);
            }
        }

        private void WriteDeletionString(KeyValuePair<int, Bounty> bounty, StringBuilder deletionSqlCommand)
        {
            deletionSqlCommand.Append("quest_id = ").Append(bounty.Key).Append(" OR ");
        }

        private void CheckConcurrentBounties()
        {
            int activeBounties = CountActive();

            if (activeBounties < ServerBountyManager.MaxConcurrentBounties)
            {
                int difference = ServerBountyManager.MaxConcurrentBounties-activeBounties;
                ChangeNotReadyBountiesToAvailable(difference);
                WriteToDB();
            }
            else
            {
                CheckAvailablityOverConcurrent();
            }
        }

        private void ChangeNotReadyBountiesToAvailable(int count)
        {
            if (mOwningCharacter.Level < ServerBountyManager.MinimumLevelRequiredForBounties)
            {
                return;
            }
            var NotReadyBounties = FindNotReadyFreeBounties();

            for (int i = 0; i < NotReadyBounties.Count && count > 0; i++)
            {
                mBounties[NotReadyBounties[i].Key].Status = Bounty.tStatus.Available;
                count--;
            }

            if (count != 0)
            {
                 NotReadyBounties = FindNotReadyPaidBounties();

                for (int i = 0; i < NotReadyBounties.Count && count > 0; i++)
                {
                    mBounties[NotReadyBounties[i].Key].Status = Bounty.tStatus.Purchasable;
                    count--;
                }
            }
        }

        // Add a certain number of bounties, of a certain cost.
        private void AddNewBounties(int count, int cost)
        {
            if (count > 0)
            {
                // Get a list of available bounties (which may or may not match the requested number) and store them
                ServerBountyManager.SelectRandomBounties(mOwningCharacter, count, this,
                    delegate(int questID)
                    {
                        if (mBounties.Count < ServerBountyManager.MaxTotalBounties)
                        {
                            // Free bounties are immediately available, while paid need to be bought before becoming available.
                            mBounties[questID] = new Bounty
                            {
                                Cost = cost,
                                // don't change the status till something checks the concurrent vs available
                                //Status = (cost == 0) ? Bounty.tStatus.Available : Bounty.tStatus.Purchasable,
                                LastRerolled = DateTime.Now
                            };
                        }
                    });
            }
            WriteToDB();
        }

        //private int CountPaidAvailable()
        //{
        //    return mBounties.Values.Count(b => b.Cost != 0 && b.Status == Bounty.tStatus.Available);
        //}

        private int CountActive()
        {
            return mBounties.Values.Count(b => b.Status >= Bounty.tStatus.Available && b.Status <= Bounty.tStatus.Completed);
        }

        private List<KeyValuePair<int, Bounty>> FindNotReadyFreeBounties()
        {
            var notReadyFreeBounties = new List<KeyValuePair<int, Bounty>>();
            for (int i = 0; i < mBounties.Count; i++)
            {
                if (mBounties.ElementAt(i).Value.Status == Bounty.tStatus.NotReady &&
                    mBounties.ElementAt(i).Value.Cost == 0)
                {
                    notReadyFreeBounties.Add(mBounties.ElementAt(i));
                }
            }
            return notReadyFreeBounties;
        }

        private List<KeyValuePair<int, Bounty>> FindNotReadyPaidBounties()
        {
            var NotReadyFreeBounties = new List<KeyValuePair<int, Bounty>>();
            for (int i = 0; i < mBounties.Count; i++)
            {
                if (mBounties.ElementAt(i).Value.Status == Bounty.tStatus.NotReady &&
                    mBounties.ElementAt(i).Value.Cost > 0)
                {
                    NotReadyFreeBounties.Add(mBounties.ElementAt(i));
                }
            }
            return NotReadyFreeBounties;
        }

        private int CountPaid()
        {
            return mBounties.Values.Count(b => b.Cost != 0);
        }
        private int CountFree()
        {
            return mBounties.Values.Count(b => b.Cost == 0);
        }

        private void ReadFromDB()
        {
            SqlQuery query = new SqlQuery(mOwningCharacter.m_db, "select * from bounties where character_id=" + mOwningCharacter.m_character_id);
            if (query.HasRows)
            {
                while (query.Read())
                {
                    int questID = query.GetInt32("quest_id");
                    int status = query.GetInt32("status");
                    int cost = query.GetInt32("cost");
                    DateTime lastRerolled = DateTime.Now;
                    if (!query.isNull("last_rerolled"))
                    {
                        lastRerolled = query.GetDateTime("last_rerolled");
                    }

                    var newBounty = new Bounty
                    {
                        Cost = cost,
                        Status = (Bounty.tStatus)status,
                        LastRerolled = lastRerolled
                    };
                    mBounties[questID] = newBounty;
                }
            }
        }

        public void WriteToDB()
        {
            foreach (KeyValuePair<int, Bounty> b in mBounties)
            {
                string command = "REPLACE INTO bounties SET character_id=" + mOwningCharacter.m_character_id;
                command += ", quest_id=" + b.Key + ", status=" + (int) b.Value.Status + ", cost=" + b.Value.Cost + ", last_rerolled='" + b.Value.LastRerolled.ToString("yyyy-MM-dd HH:mm:ss") + "' ";

                mOwningCharacter.m_db.runCommandSync(command);
            }
        }

        private void DeleteReplacedBountiesToDB(IEnumerable<KeyValuePair<int, Bounty>> bountiesToRemove)
        {
            foreach (KeyValuePair<int, Bounty> b in bountiesToRemove)
            {
                string command = "DELETE FROM bounties WHERE character_id=" + mOwningCharacter.m_character_id + " AND quest_id=" + b.Key;

                mOwningCharacter.m_db.runCommandSync(command);
            } 
        }

        public void RefreshBounties()
        {
            mBounties.Clear();
            ReadFromDB();
            RerollBounties();            
        }

        private void CheckAvailablityOverConcurrent()
        {
            int bountiesAvailable = 0;
            foreach (KeyValuePair<int, Bounty> b in mBounties)
            {
                if (b.Value.Status == Bounty.tStatus.Available)
                {
                    if (bountiesAvailable >= ServerBountyManager.MaxConcurrentBounties)
                    {
                        if (b.Value.Cost == 0) // free bounties should reset to not available
                        {
                            string command = "UPDATE bounties set status=" + (int) Bounty.tStatus.NotReady +
                                             " where character_id=" + mOwningCharacter.m_character_id + " AND quest_id=" + b.Key;

                            mOwningCharacter.m_db.runCommandSync(command);
                        }
                    }
                    else
                    {
                         bountiesAvailable += 1;
                    }                 
                }        
            } 
        }

        public void ProcessBountyBoardMessage(NetIncomingMessage msg)
        {
            int messageType = msg.ReadVariableInt32();
            switch ((BountyBoardMessageType) messageType)
            {
                case BountyBoardMessageType.DropBounty:
                {
                    int questId = msg.ReadVariableInt32();
                    DropBounty(questId);
                    break;
                }
            }
        }

        private void DropBounty(int questId)
        {
            // set to 6
            Bounty bounty;
            if (!mBounties.TryGetValue(questId, out bounty))
                return;

            bounty.Status = Bounty.tStatus.Dropped;   
            // want to check if its bugged or dropped - we do this by counting whether the quest exists
            var query = new SqlQuery(mOwningCharacter.m_db, "SELECT Count(quest_id) AS total FROM quest WHERE quest_id = " + questId + " AND character_id = " + mOwningCharacter.m_character_id);

            var rowsFound = false;
            while(query.Read())
            {
                rowsFound = query.GetInt32("total") > 0;
            }
            query.Close();

            if (rowsFound)
            {

                string whereClause = String.Format(" WHERE character_id={0} AND quest_id={1}",
                    mOwningCharacter.m_character_id, questId);


                Program.processor.m_worldDB.runCommand("DELETE from quest_stage" + whereClause);
                Program.processor.m_worldDB.runCommand("DELETE from quest" + whereClause);


                mOwningCharacter.m_QuestManager.DeleteQuest(questId, false); // dont update the database since we've just deleted it
                mOwningCharacter.m_QuestManager.SendQuestRefresh();
            }
            else
            {
                if (bounty.Cost > 0)
                {
                    Item itemAdded = mOwningCharacter.m_inventory.AddNewItemToCharacterInventory(ServerBountyManager.PaidItemID, bounty.Cost, false);
                    mOwningCharacter.m_inventory.SendInventoryUpdate();
					string locText = Localiser.GetString(textDB, mOwningCharacter.m_player, (int)CharacterBountyManagerTextDB.TextID.ITEM_RETURNED);
					locText = string.Format(locText, itemAdded.m_template.m_loc_item_name[mOwningCharacter.m_player.m_languageIndex]);
					Program.processor.sendSystemMessage(locText, mOwningCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.NONE);
                    Program.processor.updateShopHistory(-1, -1, itemAdded.m_inventory_id, itemAdded.m_template_id, bounty.Cost, 0, (int)mOwningCharacter.m_character_id, "Added for bugged bounty");
                }
            }

            logAnalytics.BountyTracking(mOwningCharacter.m_player, mOwningCharacter.m_character_id, questId, rowsFound ? BountyTrackingStatus.Dropped : BountyTrackingStatus.Bugged);

            // Do a full update, in case completing this quest means
            // we have a free slot for a new bounty.
            UpdateBounties();
        }
    }

    enum BountyBoardMessageType
    {
        DropBounty = 1
    }

    public enum BountyTrackingStatus
    {
        Purchased,
        Claimed,
        Completed,
        Dropped,
        Bugged
    }

}
