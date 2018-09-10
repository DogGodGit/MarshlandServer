using System;
using System.Collections.Generic;
using System.Linq;
using XnaGeometry;

namespace MainServer
{
    internal static class ServerBountyManager
    {
        public const string BB_BOUNTY_DATA = "BB-DATA";
        public const string BB_PURCHASE_BOUNTY = "BB-BUY";

        private static int BountyID = 4;
        private static int LevelRange = 5;

        private static DateTime LastClearTime = DateTime.Now;
        private static int ClearTimeHour = DateTime.Now.Hour;
        private static int ClearTimeMinute = DateTime.Now.Minute;

        public static int MaxFreeBounties { private set; get; }
        public static int MaxPaidBounties { private set; get; }
        public static int MaxTotalBounties { private set; get; }
        public static int MaxConcurrentBounties { private set; get; }
        public static int MinimumLevelRequiredForBounties { private set; get; }

        public static int PaidCost = 1;
        public static int PaidItemID = 36206;

        public delegate void AddBountyQuest(int questID);

        struct BountyQuest
        {
            public int QuestLevel { set; get; }
            public int QuestCost { set; get; }
            public int QuestWeight { set; get; }
        }
        private static Dictionary<int, BountyQuest> mBountyQuests = new Dictionary<int, BountyQuest>();

        static ServerBountyManager()
        {
            MaxFreeBounties = MaxPaidBounties = 3;
            MaxTotalBounties = 5;
            MaxConcurrentBounties = 5;
            MinimumLevelRequiredForBounties = 5;

            LastClearTime = DateTime.Now;
            ClearTimeHour = 0;
            ClearTimeMinute = 0;
        }

        public static void ReadWorldParam(string param, string value)
        {
            switch (param)
            {
                case "bounty_last_clear_time":
                {
                    DateTime.TryParse(value, out LastClearTime);
                    break;
                }
                case "bounty_clear_time":
                {
                    string[] str_array = value.Split(':');
                    if (str_array.Length == 2)
                    {
                        ClearTimeHour = Int32.Parse(str_array[0]);
                        ClearTimeMinute = Int32.Parse(str_array[1]);
                    }
                    break;
                }
                case "bounty_max_free_bounties":
                {
                    MaxFreeBounties = Int32.Parse(value);
                    break;
                }
                case "bounty_max_paid_bounties":
                {
                    MaxPaidBounties = Int32.Parse(value);
                    break;
                }
                case "bounty_paid_token":
                {
                    PaidItemID = Int32.Parse(value);
                    break;
                }
                case "bounty_max_total_bounties":
                {
                    MaxTotalBounties = Int32.Parse(value);
                    break;
                }
                case "bounty_max_concurrent_bounties":
                {
                    MaxConcurrentBounties = Int32.Parse(value);
                    break;
                }
                case "bounty_minimum_level_for_bounties":
                {
                    MinimumLevelRequiredForBounties = Int32.Parse(value);
                    break;
                }
            }
        }

        public static void Update(DateTime now)
        {
            if (now.Hour == ClearTimeHour && now.Minute == ClearTimeMinute && (now - LastClearTime).TotalMinutes >= 5)
            {
                var listOfOnlinePlayers = new List<Player>(Program.processor.m_players);
                // Ready to reset Bounty Board
                var task = new ResetBountyBoardTask(listOfOnlinePlayers);
                LastClearTime = now;
                lock (Program.processor.m_backgroundTasks)
                {
                    Program.processor.m_backgroundTasks.Enqueue(task);
                }
            }
        }
        // Build up a list of BountyQuest objects, that can be used to trigger a bounty.
        public static void Load(Database db)
        {
            // Load up free bounties
            SqlQuery query = new SqlQuery(db, "select quest_id, level_required, bounty_weight, repeatable from quest_templates where repeatable = " + BountyID + " order by quest_id");
            while (query.Read())
            {
                mBountyQuests[query.GetInt32("quest_id")] = new BountyQuest
                {
                    QuestLevel = query.GetInt32("level_required"),
                    QuestWeight = query.GetInt32("bounty_weight"),
                    QuestCost = 0
                };
            }
            query.Close();
        }

        public static bool QuestStarted(Character character, int questID)
        {
            if (character.CharacterBountyManager != null)
                return character.CharacterBountyManager.QuestStart(questID);
            return false;
        }

        public static void QuestCompleted(Character character, int questID)
        {
            if (character.CharacterBountyManager != null)
                character.CharacterBountyManager.QuestComplete(questID);
        }

        public static bool CheckBountyLevelIsWithinPlayerLevel(int level, int questId)
        {
            if (level >= (mBountyQuests[questId].QuestLevel - LevelRange) &&
                level <= (mBountyQuests[questId].QuestLevel + LevelRange))
            {
                return true;
            }
            return false;
        } 

        // Out of the available bounties that match the character level and cost, select (in random order) the requested number,
        // and place the quest id's into a list.
        public static void SelectRandomBounties(Character owningCharacter, int count, CharacterBountyManager manager, AddBountyQuest add)
        {
            if (manager == null)
                return;

            for (int loop = 0; loop < count; loop++)
            {
                // Select all the bounty quests that match our parameters, and which 
                // aren't in our current bounty list.
				//want to allow high level players to access the top bounties so clamp level
				//range to 230
	            int level = owningCharacter.Level;
	            level = (int)MathHelper.Clamp(level, 0, 230);
                var selection = mBountyQuests.Where(
                    b => (level >= (b.Value.QuestLevel - LevelRange)) &&
						 (level <= (b.Value.QuestLevel + LevelRange)) &&
                         (!manager.mBounties.ContainsKey(b.Key)) &&
                         (owningCharacter.m_QuestManager.IsAvailable(b.Key))
                         ).ToList();

                // Now, we select a random, weighted bounty from this list.
                // See http://stackoverflow.com/questions/1761626/weighted-random-numbers for algorithm

                // First, calculate weight aggregate
                int aggregate = selection.Aggregate(0, (current, bounty) => current + bounty.Value.QuestWeight);

                // Then, calculate random number within the limits of the aggregate
                int rand = Program.getRandomNumber(aggregate);

                // Then, pick the chosen bounty by subtracting the weight of each bounty in list
                for (int i = 0; i < selection.Count; i++)
                {
                    if (rand < selection[i].Value.QuestWeight)
                    {
                        // We have the chosen bounty
                        // Call the delegate that handles the new quest choice
                        if (add != null)
                        {
                            add(selection.ElementAt(i).Key);
                            break;
                        }
                    }
                    else
                    {
                        rand -= selection[i].Value.QuestWeight;
                    }
                }
            }
        }

        public static DateTime GetLastClearTime()
        {
            return LastClearTime;
        }
    }
}
