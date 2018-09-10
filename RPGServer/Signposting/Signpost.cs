using System;
using System.Collections.Generic;
using System.Globalization;
using MainServer.Localise;

namespace MainServer.Signposting
{
    class Signpost
    {
        int m_baseConditionID = -1;
        int m_signpostID = -1;
        int m_priority = -1;
        int m_quantity = -1;
        bool m_accountBased = false;
        SignpostCondition m_baseCondition = null;
        List<SignpostAction> m_actions = null;
        static bool Debugging = false;

        internal int Priority
        {
            get { return m_priority; }
        }
        internal int SignpostID
        {
            get { return m_signpostID; }
        }
        internal List<SignpostAction> Actions
        {
            get { return m_actions; }
        }

        internal int Quantity
        {
            get { return m_quantity; }
        }
        internal bool AccountBased
        {
            get { return m_accountBased; }
        }
        internal Signpost(int signpostID,int baseConditionID, int priority, int quantity,bool accountBased)
        {
            m_baseConditionID = baseConditionID;
            m_signpostID=signpostID;
            m_priority = priority;
            m_quantity = quantity;
            m_accountBased = accountBased;
        }
        internal void ReadAllConditionsFromDatabase(Database db)
        {
            List<SignpostCondition> conditionList = new List<SignpostCondition>();
            List<SignpostLogicCondition> logicConditionList = new List<SignpostLogicCondition>();
            SqlQuery query = new SqlQuery(db,"select * from signpost_conditions where signpost_id = "+m_signpostID);
            if(query.HasRows){

                while(query.Read())
                {
                    int conditionID = query.GetInt32("signpost_condition_id");
                    bool inverted = query.GetBoolean("inverted");
                    string parameterString = query.GetString("params");
                    DateTime paramDate = DateTime.Now;
                    SignpostCondition.ConditionType conditionType = (SignpostCondition.ConditionType)query.GetInt32("condition_type_id");
                    List<float> paramFloats = new List<float>();
                    string[] paramStringList = parameterString.Split(new char[] {';'},StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < paramStringList.Length; i++)
                    {
                        string currentString = paramStringList[i];
                        float floatVal = 0;

                        if (float.TryParse(currentString, out floatVal))
                        {
                            paramFloats.Add(floatVal);
                        }
                        else
                        {
                            if (conditionType == SignpostCondition.ConditionType.DateGreaterThan)
                            {
                                CultureInfo enUS = new CultureInfo("en-US");

                                // DateTimeFormatInfo formatInfo = new DateTimeFormatInfo();


                                string dateformat = "yyyy/MM/dd HH:mm:ss";

                                // paramDate = DateTime.Parse(currentString,
                                // paramDate = DateTime.ParseExact(currentString, dateformat, CultureInfo.InvariantCulture);
                                bool parsedDate = DateTime.TryParseExact(currentString, dateformat, enUS, DateTimeStyles.None, out paramDate);
                                if (parsedDate == false)
                                {
                                    Program.Display("date " + currentString + " failed to parse");
                                }
                            }
                            else
                            {
                                Program.Display("signpost found non float param " + currentString);
                            }
                        }

                    }
                    SignpostCondition newCondition = null; 
                    if (conditionType == SignpostCondition.ConditionType.And || conditionType == SignpostCondition.ConditionType.Or)
                    {
                        SignpostLogicCondition logicCondition = new SignpostLogicCondition(conditionID, conditionType, inverted, paramFloats);
                        logicConditionList.Add(logicCondition);
                        newCondition = logicCondition;
                    }
                    else
                    {
                        newCondition = new SignpostCondition(conditionID, conditionType, inverted, paramFloats);
                        if (conditionType == SignpostCondition.ConditionType.DateGreaterThan)
                        {
                            newCondition.ParamDate = paramDate;

                        }
                    }
                    conditionList.Add(newCondition);
                    if (conditionID == m_baseConditionID)
                    {
                        m_baseCondition = newCondition;
                    }
                }
            }
            query.Close();

            for (int i = 0; i < logicConditionList.Count; i++)
            {
                SignpostLogicCondition logicCondition = logicConditionList[i];
                for (int paramIndex = 0; paramIndex < logicCondition.Parameters.Count; paramIndex++)
                {
                    float currentParam = logicCondition.Parameters[paramIndex];

                    SignpostCondition parameterCondition = null;

                    for (int conditionIndex = 0; conditionIndex < conditionList.Count && parameterCondition == null; conditionIndex++)
                    {
                        SignpostCondition currentCondition = conditionList[conditionIndex];
                        if (currentCondition.ConditionID == currentParam)
                        {
                            parameterCondition = currentCondition;
                        }
                    }
                    if (parameterCondition != null)
                    {
                        logicCondition.InternalConditions.Add(parameterCondition);

                    }
                }
            }
        }
        internal void ReadAllActionsFromDatabase(Database db)
        {

            m_actions = new List<SignpostAction>();
            SqlQuery query = new SqlQuery(db, "select * from signpost_actions where signpost_id = " + m_signpostID);
            if (query.HasRows)
            {

                while (query.Read())
                {
                    int actionID = query.GetInt32("signpost_action_id");
                    
                    string parameterString = query.GetString("params");
                    SignpostAction.ActionType actionType = (SignpostAction.ActionType)query.GetInt32("action_type_id");
                    string message = query.GetString("message"); 
                    List<float> paramFloats = new List<float>();
                    string[] paramStringList = parameterString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < paramStringList.Length; i++)
                    {
                        string currentString = paramStringList[i];
                        float floatVal = float.Parse(currentString);
                        paramFloats.Add(floatVal);

                    }
                    SignpostAction newCondition = new SignpostAction(m_signpostID,actionID, actionType, paramFloats,message);
                    m_actions.Add(newCondition);
                  

                }
            }
            query.Close();


        }
        internal SignpostContainer TestConditions(Character playerCharacter)
        {

            SignpostContainer resultContainer = new SignpostContainer(this);

            if (m_baseCondition != null)
            {
                SignpostCondition.PassType passType = m_baseCondition.PassesConditions(playerCharacter, resultContainer);
                resultContainer.PassLevel = passType;
            }

            return resultContainer;

        }
        internal void UndoActions(Character playerCharacter)
        {
             if (Debugging == true && playerCharacter != null)
            {
                Program.Display("Signpost closed : "+m_signpostID+ " for "+playerCharacter.GetIDString());
            }

            for (int actionIndex = 0; actionIndex < m_actions.Count; actionIndex++)
            {
                SignpostAction currentAction = m_actions[actionIndex];
                switch (currentAction.Type)
                {
                    case SignpostAction.ActionType.HelpItemTemplate:
                    case SignpostAction.ActionType.HelpMobTemplate:
                    case SignpostAction.ActionType.HelpPoint:
                    case SignpostAction.ActionType.HelpSpawnPoint:
                        {
                            
                            Program.processor.SendHidePlayerHelp(playerCharacter.m_player);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }
        internal void TakeAction(Character playerCharacter, PlayerSignpostManager signpostManager)
        {
            if (Debugging == true && playerCharacter != null)
            {
                Program.Display("Signpost activated : "+m_signpostID+ " for "+playerCharacter.GetIDString());
            }

            for (int actionIndex = 0; actionIndex < m_actions.Count; actionIndex++)
            {
                SignpostAction currentAction = m_actions[actionIndex];
                currentAction.TakeAction(playerCharacter);
            }
           
        }
    }

    class SignpostContainer
    {
        Signpost m_signpost =null;

        List<SignpostCondition> m_failedPositionConditions = new List<SignpostCondition>();
        List<SignpostCondition> m_passedPositionConditions = new List<SignpostCondition>();

        List<SignpostCondition> m_failedTimeConditions = new List<SignpostCondition>();
        List<SignpostCondition> m_passedTimeConditions = new List<SignpostCondition>();

        List<SignpostCondition> m_failedDeathConditions = new List<SignpostCondition>();
        List<SignpostCondition> m_passedDeathConditions = new List<SignpostCondition>();


        SignpostCondition.PassType m_passLevel = SignpostCondition.PassType.FailsCondition;
        internal SignpostCondition.PassType PassLevel
        {
            get { return m_passLevel; }
            set { m_passLevel = value; }
        }
        internal Signpost BaseSignpost
        {
            get { return m_signpost; }
        }
        internal SignpostContainer(Signpost signpost)
        {
            m_signpost = signpost;
        }
        internal void AddConditions(List<SignpostCondition> conditionslist,bool passed)
        {
            List<SignpostCondition> timeConditions = null;
            List<SignpostCondition> positionConditions = null;
            List<SignpostCondition> deathConditions = null;

            if (passed == true)
            {
                timeConditions = m_passedTimeConditions;
                positionConditions = m_passedPositionConditions;
                deathConditions = m_passedDeathConditions;
            }
            else
            {
                timeConditions = m_failedTimeConditions;
                positionConditions = m_failedPositionConditions;
                deathConditions = m_failedDeathConditions;
            }

            for (int i = 0; i < conditionslist.Count; i++)
            {
                SignpostCondition currentCondition = conditionslist[i];
                switch (currentCondition.Type)
                {
                    case SignpostCondition.ConditionType.InRangeOfPosition:
                        {
                            positionConditions.Add(currentCondition);
                            break;
                        }
                    case SignpostCondition.ConditionType.TimeLoggedIn:
                    case SignpostCondition.ConditionType.TimeSinceQuestStageComplete:
                    case SignpostCondition.ConditionType.TimeSinceQuestComplete:
                    case SignpostCondition.ConditionType.DateGreaterThan:
                        {
                            timeConditions.Add(currentCondition);
                            break;
                        }
                    case SignpostCondition.ConditionType.CharacterIsDead:
                    case SignpostCondition.ConditionType.CharacterDeaths:
                        {
                            deathConditions.Add(currentCondition);
                            break;
                        }


                }
            }

        }
        internal bool TimeUpdated(Character playerCharacter)
        {
            bool conditionsUpdated = false;

            SignpostContainer tempContainer = new SignpostContainer(m_signpost);
            for (int i = 0; i < m_failedTimeConditions.Count && conditionsUpdated == false; i++)
            {
                SignpostCondition currentCondition = m_failedTimeConditions[i];
                SignpostCondition.PassType passType = currentCondition.PassesConditions(playerCharacter, tempContainer);
                if (passType == SignpostCondition.PassType.CurrentPass || passType == SignpostCondition.PassType.PassesCondition || passType == SignpostCondition.PassType.CompletePass)
                {
                    conditionsUpdated = true;
                }
            }

            for (int i = 0; i < m_passedTimeConditions.Count && conditionsUpdated == false; i++)
            {
                SignpostCondition currentCondition = m_passedTimeConditions[i];
                SignpostCondition.PassType passType = currentCondition.PassesConditions(playerCharacter, tempContainer);
                if (passType == SignpostCondition.PassType.CurrentFail || passType == SignpostCondition.PassType.FailsCondition||passType == SignpostCondition.PassType.CompleteFail)
                {
                    conditionsUpdated = true;
                }
            }

                return conditionsUpdated;

        }
        internal bool PositionUpdated(Character playerCharacter)
        {
            bool conditionsUpdated = false;

            SignpostContainer tempContainer = new SignpostContainer(m_signpost);
            for (int i = 0; i < m_failedPositionConditions.Count && conditionsUpdated == false; i++)
            {
                SignpostCondition currentCondition = m_failedTimeConditions[i];
                SignpostCondition.PassType passType = currentCondition.PassesConditions(playerCharacter, tempContainer);
                if (passType == SignpostCondition.PassType.CurrentPass || passType == SignpostCondition.PassType.PassesCondition|| passType == SignpostCondition.PassType.CompletePass)
                {
                    conditionsUpdated = true;
                }
            }

            for (int i = 0; i < m_passedPositionConditions.Count && conditionsUpdated == false; i++)
            {
                SignpostCondition currentCondition = m_passedTimeConditions[i];
                SignpostCondition.PassType passType = currentCondition.PassesConditions(playerCharacter, tempContainer);
                if (passType == SignpostCondition.PassType.CurrentFail || passType == SignpostCondition.PassType.FailsCondition|| passType == SignpostCondition.PassType.CompleteFail)
                {
                    conditionsUpdated = true;
                }
            }

            return conditionsUpdated;

        }
        internal bool CharacterDied(Character playerCharacter)
        {
            bool conditionsUpdated = false;
            
            SignpostContainer tempContainer = new SignpostContainer(m_signpost);
            for (int i = 0; i < m_failedDeathConditions.Count && conditionsUpdated == false; i++)
            {
                SignpostCondition currentCondition = m_failedDeathConditions[i];
                SignpostCondition.PassType passType = currentCondition.PassesConditions(playerCharacter, tempContainer);
                if (passType == SignpostCondition.PassType.CurrentPass || passType == SignpostCondition.PassType.PassesCondition || passType == SignpostCondition.PassType.CompletePass)
                {
                    conditionsUpdated = true;
                }
            }

            for (int i = 0; i < m_passedDeathConditions.Count && conditionsUpdated == false; i++)
            {
                SignpostCondition currentCondition = m_passedDeathConditions[i];
                SignpostCondition.PassType passType = currentCondition.PassesConditions(playerCharacter, tempContainer);
                if (passType == SignpostCondition.PassType.CurrentFail || passType == SignpostCondition.PassType.FailsCondition || passType == SignpostCondition.PassType.CompleteFail)
                {
                    conditionsUpdated = true;
                }
            }

            return conditionsUpdated;
        }
    }

    static class SignpostManager
    {
		// #localisation
		static int textDBIndex = 0;

		static List<Signpost> m_signposts = new List<Signpost>();

        static internal List<Signpost> Signposts
        {
            get { return m_signposts; }
        }

        static internal void ReadSignpostsFromDatabase(Database db)
        {
            SqlQuery query = new SqlQuery(db, "select * from signposts");
            if (query.HasRows)
            {

                while (query.Read())
                {
                    int signpostID = query.GetInt32("signpost_id");
                    int baseConditionID = query.GetInt32("base_condition_id");
                    int priority = query.GetInt32("priority");
                    int quantity = query.GetInt32("quantity");
                    bool accountBased = query.GetBoolean("account_based");
                    Signpost newSignpost = new Signpost(signpostID, baseConditionID, priority,quantity,accountBased);
                    m_signposts.Add(newSignpost);

                }
            }
            query.Close();

            for (int i = 0; i < m_signposts.Count; i++)
            {
                Signpost currentSignpost = m_signposts[i];
                currentSignpost.ReadAllConditionsFromDatabase(db);
                currentSignpost.ReadAllActionsFromDatabase(db);
            }

			// Get textNameDB index.
			textDBIndex = Localiser.GetTextDBIndex("signpost_actions");
		}
        internal static Signpost GetSignpostForID(int signpostID)
        {
            Signpost signpost = null;

            for (int i = 0; i < m_signposts.Count && signpost==null; i++)
            {
                Signpost currentSignpost = m_signposts[i];
                if (currentSignpost.SignpostID== signpostID)
                {
                    signpost = currentSignpost;
                }
            }

                return signpost;

        }

		static internal string GetLocaliseSignPostActionMessage(Player player, int signPostID, int actionID)
		{
			int textID = Localiser.CombineData((ushort)signPostID, (ushort)actionID);
			return Localiser.GetString(textDBIndex, player, textID);
		}
	}
    

}
