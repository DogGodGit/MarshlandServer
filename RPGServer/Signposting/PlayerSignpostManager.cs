using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using XnaGeometry;

namespace MainServer.Signposting
{
    class PlayerSignpostManager
    {



        List<Signpost> m_checkList= null;
        /// <summary>
        /// Contains information of signposts that have been completed and have a max number of trigger times
        /// </summary>
        List<CompletedSignpost> m_redeemedSignposts = new List<CompletedSignpost>();
        /// <summary>
        /// The signpost that is currently active
        /// </summary>
        SignpostContainer m_activeSignpost = null;
        /// <summary>
        /// A list of signposts that could pass before a manditory recheck is called
        /// eg. signposts that fail on time/position data
        /// </summary>
        List<SignpostContainer> m_couldPassSignposts = new List<SignpostContainer>();
        /// <summary>
        /// The Minimum distance the character should have moved before position checks are carried out 
        /// </summary>
        static float MIN_MOVEMENT_RECHECK_DIST = 0.5f;
        /// <summary>
        ///  The Minimum time that has passed before time checks are carried out 
        /// </summary>
        static float MIN_TIME_BETWEEN_TIME_CHECKS = 1.0f;

        /// <summary>
        /// For time based conditions
        /// The time that time based conditions were last checked
        /// </summary>
        double m_timeAtLastCheck = 0;
        /// <summary>
        /// For Position based conditions
        /// the position at which position based conditions were last checked
        /// </summary>
        Vector3 m_positionAtLastCheck = new Vector3();

        /// <summary>
        /// A condition that may change the active signpost has changed it's pass result
        /// Check all conditions to see what signpost should be active
        /// </summary>
        bool m_recheckSignposts = true;
        /// <summary>
        /// A condition that may change the active signpost has changed it's pass result
        /// Check all conditions to see what signpost should be active
        /// </summary>
        internal bool RecheckSignposts
        {
            set { m_recheckSignposts = value; }
        }
        /// <summary>
        /// List.sort function
        /// used to sort a list of signposts where the highest priority signposts will be at the front of the list
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static int CompareSignpostsByPriorityDecending(SignpostContainer x, SignpostContainer y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're 
                    // equal.  
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y 
                    // is greater.  
                    return -1;
                }
            }
            else
            {
                // If x is not null... 
                // 
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the  
                    // priority
                    if (x.BaseSignpost.Priority > y.BaseSignpost.Priority)
                    {
                        return -1;
                    }
                    else if (x.BaseSignpost.Priority < y.BaseSignpost.Priority)
                    {
                        return 1;
                    }

                }
            }
            return 0;
        }
        /// <summary>
        /// Called when the character moves to check if any signpost conditions have changed because of the new position
        /// </summary>
        /// <param name="playerCharacter"></param>
        internal void DidMove(Character playerCharacter)
        {

            float posChange = Utilities.Difference2D(playerCharacter.CurrentPosition.m_position, m_positionAtLastCheck);
            if (posChange > MIN_MOVEMENT_RECHECK_DIST)
            {
                m_positionAtLastCheck = playerCharacter.CurrentPosition.m_position;
                bool activeChanged = false;

                if (m_activeSignpost != null)
                {
                    activeChanged = m_activeSignpost.PositionUpdated(playerCharacter);
                }
                if (activeChanged == true)
                {
                    SignpostContainer activepass = m_activeSignpost.BaseSignpost.TestConditions(playerCharacter);
                    if (activepass.PassLevel == SignpostCondition.PassType.CurrentFail || activepass.PassLevel == SignpostCondition.PassType.FailsCondition)
                    {
                        m_recheckSignposts = true;
                    }
                }

                for (int i = 0; i < m_couldPassSignposts.Count && m_recheckSignposts == false; i++)
                {
                    bool currentChanged = false;
                    SignpostContainer currentSignpost = m_couldPassSignposts[i];
                    currentChanged = currentSignpost.PositionUpdated(playerCharacter);
                    if (currentChanged == true)
                    {
                        SignpostContainer currentpass = currentSignpost.BaseSignpost.TestConditions(playerCharacter);
                        if (currentpass.PassLevel == SignpostCondition.PassType.PassesCondition || currentpass.PassLevel == SignpostCondition.PassType.CurrentPass || currentpass.PassLevel == SignpostCondition.PassType.CompletePass)
                        {
                            m_recheckSignposts = true;
                        }
                    }

                }
            }


        }
        /// <summary>
        /// Called when the player dies or is resurrected, or when a death event expires
        /// </summary>
        /// <param name="playerCharacter"></param>
        internal void CharacterDied(Character playerCharacter)
        {

            bool activeChanged = false;

            if (m_activeSignpost != null)
            {
                activeChanged = m_activeSignpost.CharacterDied(playerCharacter);
            }
            if (activeChanged == true)
            {
                SignpostContainer activepass = m_activeSignpost.BaseSignpost.TestConditions(playerCharacter);
                if (activepass.PassLevel == SignpostCondition.PassType.CurrentFail || activepass.PassLevel == SignpostCondition.PassType.FailsCondition)
                {
                    m_recheckSignposts = true;
                }
            }

            for (int i = 0; i < m_couldPassSignposts.Count && m_recheckSignposts == false; i++)
            {
                bool currentChanged = false;
                SignpostContainer currentSignpost = m_couldPassSignposts[i];
                currentChanged = currentSignpost.CharacterDied(playerCharacter);
                if (currentChanged == true)
                {
                    SignpostContainer currentpass = currentSignpost.BaseSignpost.TestConditions(playerCharacter);
                    if (currentpass.PassLevel == SignpostCondition.PassType.PassesCondition || currentpass.PassLevel == SignpostCondition.PassType.CurrentPass || currentpass.PassLevel == SignpostCondition.PassType.CompletePass)
                    {
                        m_recheckSignposts = true;
                    }
                }

            }
            


        }
        internal void Update(Character playerCharacter)
        {
            TimeUpdated(playerCharacter);
            if (m_recheckSignposts == true)
            {
                RecheckAllSignposts(playerCharacter);
            }
        }
        /// <summary>
        /// Checks if any signpost conditions have changed because of the current time
        /// </summary>
        /// <param name="controller"></param>
        void TimeUpdated(Character playerCharacter)
        {
            double currentTime = NetTime.Now;

            if ((currentTime - m_timeAtLastCheck) > MIN_TIME_BETWEEN_TIME_CHECKS)
            {
                m_timeAtLastCheck = currentTime;

                bool activeChanged = false;

                if (m_activeSignpost != null)
                {
                    activeChanged = m_activeSignpost.TimeUpdated(playerCharacter);
                }
                if (activeChanged == true)
                {
                    SignpostContainer activepass = m_activeSignpost.BaseSignpost.TestConditions(playerCharacter);
                    if (activepass.PassLevel == SignpostCondition.PassType.CurrentFail || activepass.PassLevel == SignpostCondition.PassType.FailsCondition)
                    {
                        m_recheckSignposts = true;
                    }
                }

                for (int i = 0; i < m_couldPassSignposts.Count && m_recheckSignposts == false; i++)
                {
                    bool currentChanged = false;
                    SignpostContainer currentSignpost = m_couldPassSignposts[i];
                    currentChanged = currentSignpost.TimeUpdated(playerCharacter);
                    if (currentChanged == true)
                    {
                        SignpostContainer currentpass = currentSignpost.BaseSignpost.TestConditions(playerCharacter);
                        if (currentpass.PassLevel == SignpostCondition.PassType.CurrentPass || currentpass.PassLevel == SignpostCondition.PassType.PassesCondition || currentpass.PassLevel == SignpostCondition.PassType.CompletePass)
                        {
                            m_recheckSignposts = true;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Checks all signposts to  see if the active signpost should be changed
        /// if the active signpost changes it will close down the old signpost while activating the new signpost
        /// </summary>
        /// <param name="playerCharacter"></param>
        void RecheckAllSignposts(Character playerCharacter)
        {
            List<Signpost> allSignposts = m_checkList;
            m_couldPassSignposts.Clear();

            List<SignpostContainer> passList = new List<SignpostContainer>();
            List<SignpostContainer> failList = new List<SignpostContainer>();
            SignpostContainer newSignpost = null;

            for (int i = (allSignposts.Count-1); i >=0; i--)
            {
                Signpost currentSignpost = allSignposts[i];
                SignpostContainer result = currentSignpost.TestConditions(playerCharacter);
                if (result.PassLevel == SignpostCondition.PassType.PassesCondition || result.PassLevel == SignpostCondition.PassType.CurrentPass || result.PassLevel == SignpostCondition.PassType.CompletePass)
                {
                    passList.Add(result);
                }
                else if (result.PassLevel == SignpostCondition.PassType.CurrentFail)
                {
                    failList.Add(result);
                }
                else if(result.PassLevel == SignpostCondition.PassType.CompleteFail)
                {
                    m_checkList.Remove(currentSignpost);
                }
                

            }
            if (passList.Count > 0)
            {
                passList.Sort(PlayerSignpostManager.CompareSignpostsByPriorityDecending);
                newSignpost = passList[0];
            }
            //fill the could pass list with all possible Signposts that may replace the current
            if (newSignpost != null)
            {
                int currentPriority = newSignpost.BaseSignpost.Priority;
                failList.Sort(PlayerSignpostManager.CompareSignpostsByPriorityDecending);
                bool fillComplete = false;
                for (int i = 0; i < failList.Count && fillComplete == false; i++)
                {
                    SignpostContainer currentContainer = failList[i];
                    if (currentContainer.BaseSignpost.Priority > currentPriority)
                    {
                        m_couldPassSignposts.Add(currentContainer);
                    }
                    else
                    {
                        fillComplete = true;
                    }

                }
            }
            else
            {
                m_couldPassSignposts.AddRange(failList);
            }

            if (newSignpost != null)
            {
                bool actiontaken = false;

                if (m_activeSignpost == null)
                {
                    newSignpost.BaseSignpost.TakeAction(playerCharacter,this);
                    actiontaken = true;
                }
                else if (newSignpost.BaseSignpost != m_activeSignpost.BaseSignpost)
                {

                    m_activeSignpost.BaseSignpost.UndoActions(playerCharacter);

                    newSignpost.BaseSignpost.TakeAction(playerCharacter,this);
                    actiontaken = true;

                }
                else
                {

                }
                if (actiontaken == true)
                {
                    if (newSignpost.BaseSignpost.Quantity > 0)
                    {
                        int characterID = (int) playerCharacter.m_character_id;
                            if(newSignpost.BaseSignpost.AccountBased==true){
                                characterID = -1;
                            }
                        CompletedSignpost completedData = GetCompletedSignpostForID(newSignpost.BaseSignpost.SignpostID);
                        if (completedData == null)
                        {


                            completedData = CompletedSignpost.CreateCompletedSignpost(newSignpost.BaseSignpost.SignpostID, 1, playerCharacter.m_player.m_account_id, characterID);
                            m_redeemedSignposts.Add(completedData);
                        }
                        else
                        {
                            completedData.SetNewCount(completedData.Count + 1, playerCharacter.m_account_id, characterID);
                        }
                        m_checkList.Remove(newSignpost.BaseSignpost);


                    }
                }
            }
            else if (m_activeSignpost != null)
            {
                m_activeSignpost.BaseSignpost.UndoActions(playerCharacter);
            }
            m_activeSignpost = newSignpost;
            m_positionAtLastCheck = playerCharacter.CurrentPosition.m_position;
            m_timeAtLastCheck = NetTime.Now;
        }
        CompletedSignpost GetCompletedSignpostForID(int signpostID)
        {
            CompletedSignpost signpostData = null;

            for (int signpostIndex = m_redeemedSignposts.Count - 1; signpostIndex >= 0 && signpostData==null; signpostIndex--)
            {
                CompletedSignpost currentData = m_redeemedSignposts[signpostIndex];

                if (currentData.SignpostID == signpostID)
                {
                    signpostData = currentData;
                }
            }

            return signpostData;
            
        }
        /// <summary>
        /// reads the selected characters completed signposts from the database
        /// This allows signpoststo be shown a set number of times
        /// </summary>
        /// <param name="accountID"></param>
        /// <param name="characterID"></param>
        void ReadCompletedSignpostsFromDatabase(long accountID, int characterID)
        {

            SqlQuery signpostQuery = new SqlQuery(Program.processor.m_universalHubDB, "select * from signpost_records where account_id = " + accountID + " and world_id =" + Program.m_worldID + " and character_id in( -1," + characterID + ")");

            if (signpostQuery.HasRows)
            {
                while (signpostQuery.Read())
                {
                    int signpostID = signpostQuery.GetInt32("signpost_id");
                    int triggerCount = signpostQuery.GetInt32("trigger_count");

                    CompletedSignpost newSignpost = new CompletedSignpost(signpostID, triggerCount);

                    m_redeemedSignposts.Add(newSignpost);

                }
            }

            signpostQuery.Close();
        }
        /// <summary>
        /// Fills the list of signposts to be checked for this character with the current list of signposts
        /// signposts that have a limited trigger rate will not be added if they have already been triggered the correct number of times
        /// </summary>
        void FillCheckList()
        {
            List<Signpost> baseList = SignpostManager.Signposts;
            m_checkList = new List<Signpost>(baseList.Count);

            for (int i = 0; i < baseList.Count; i++)
            {
                Signpost currentSignpost = baseList[i];
                bool addSignpost = true;
                if (currentSignpost.Quantity > 0)
                {
                    CompletedSignpost signpostData = GetCompletedSignpostForID(currentSignpost.SignpostID);
                    if (signpostData != null)
                    {
                        if (signpostData.Count >= currentSignpost.Quantity)
                        {
                            addSignpost = false;
                        }
                    }
                }
                if (addSignpost == true)
                {
                    m_checkList.Add(currentSignpost);
                }
            }
        }
        /// <summary>
        /// Populates the list of singposts and checks what signposts have already been completed
        /// </summary>
        /// <param name="accountID"></param>
        /// <param name="characterID"></param>
        internal void Setup(long accountID, int characterID)
        {
            ReadCompletedSignpostsFromDatabase(accountID, characterID);
            FillCheckList();

        }
    }

    class CompletedSignpost
    {
        int m_signpostID = -1;
        int m_count = 0;

        internal int SignpostID
        {
            get { return m_signpostID; }
        }
        internal int Count
        {
            get { return m_count; }
        }
        internal void SetNewCount(int newCount, int accountID, int characterID)
        {

            m_count = newCount;

            Program.processor.m_universalHubDB.runCommandSync("update signpost_records set trigger_count = " + newCount + " where account_id = " + accountID + " and world_id =" + Program.m_worldID + " and character_id = " + characterID + " and signpost_id = " + m_signpostID);

        }


        internal CompletedSignpost(int signpostID, int count)
        {
            m_signpostID = signpostID;
           
            m_count = count;
        }

        static internal CompletedSignpost CreateCompletedSignpost(int signpostID, int count, long accountID, int characterID)
        {
            CompletedSignpost newRedeemedSignpost = new CompletedSignpost(signpostID, count);

            Program.processor.m_universalHubDB.runCommandSync("insert into signpost_records (account_id,world_id,character_id,signpost_id,trigger_count) values ("+accountID+","+Program.m_worldID+","+characterID+","+signpostID+","+count+") ");

            return newRedeemedSignpost;
        }

    }
}
