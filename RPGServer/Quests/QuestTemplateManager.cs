using System;
using System.Collections.Generic;
using MainServer.Localise;

namespace MainServer
{
    class QuestTemplateManager
    {
        Database m_db;

		// #localisation
		static int textDBIndex = 0;

		/// <summary>
		/// All quest templates loaded at startup
		/// </summary>
		public Dictionary<int, QuestTemplate> QuestTemplates { get; private set; }

        /// <summary>
        /// Load all templates and stages from the datadb
        /// </summary>
        /// <param name="db"></param>
        public QuestTemplateManager(Database db)
        {
            m_db = db;
            LoadQuestTemplates();
        }

        public void LoadQuestTemplates()
        {
            
            //create our dictionary
            QuestTemplates = new Dictionary<int, QuestTemplate>();

            //read in all quest templates
            SqlQuery query = new SqlQuery(m_db, "select * from quest_templates order by quest_id");
            if (query.HasRows)
            {
                while (query.Read())
                {

                    int quest_id = query.GetInt32("quest_id");
                    QuestTemplate newTemplate = new QuestTemplate(query);
                    QuestTemplates.Add(quest_id, newTemplate);
                }

            }
            query.Close();

            //now read in all quest stages and attach to the templates            
            SqlQuery stageQuery = new SqlQuery(m_db, "select * from quest_stage_templates order by quest_id,stage_id");
            while (stageQuery.Read())
            {
                int quest_id = stageQuery.GetInt32("quest_id");
                int stage_id = stageQuery.GetInt32("stage_id");
                QuestStageTemplate.CompletionType completion_type = (QuestStageTemplate.CompletionType)stageQuery.GetInt32("completion_type");
                string completion_details = stageQuery.GetString("completion_details");
                int next_stage = stageQuery.GetInt32("next_stage");
                int stage_open_sum = stageQuery.GetInt32("stage_open_sum");

                //find matching template for this this stage
                QuestTemplate template = GetQuestTemplate(quest_id);
                if (template == null)
                {
                    //no template found, display an error
                    Program.Display("QuestTemplateManager.bad quest stage template. stageId." + stage_id + " questId(NotFound)." + quest_id);
                    continue;
                }

                //we found a matching template so create and add this stage to it. 
                QuestStageTemplate newStage = new QuestStageTemplate(template, stage_id, completion_type, completion_details, next_stage, stage_open_sum);
                template.m_QuestStageTemplates.Add(newStage);

            }

            stageQuery.Close();

			// Get textNameDB index.
			textDBIndex = Localiser.GetTextDBIndex("quest_templates - quest_name");
		}

        /// <summary>
        /// Return matching template for this quest_id, or null if not found
        /// </summary>
        /// <param name="quest_id"></param>
        /// <returns></returns>
        public QuestTemplate GetQuestTemplate(int quest_id)
        {
            if (QuestTemplates == null)
                return null;

            QuestTemplate value;

            if (QuestTemplates.TryGetValue(quest_id, out value) == false)
                return null;

            return value;
        }

        /// <summary>
        /// Return list of all templates that match this repeat type.
        /// </summary>
        /// <param name="repeatType"></param>
        /// <returns></returns>
        public List<QuestTemplate> GetAllQuestsOfRepeatType(QuestTemplate.Repeatability repeatType)
        {
            //create new list
            List<QuestTemplate> filteredTemplateList = new List<QuestTemplate>();

            foreach (var template in QuestTemplates.Values)
            {
                if (template.m_repeatable == repeatType)
                {
                    filteredTemplateList.Add(template);
                }
            }

            return filteredTemplateList;
        }

        /// <summary>
        /// Takes in a list of quest templates and create a comma seperated list of their ids
        /// to store in the database.
        /// </summary>
        /// <param name="questList">List of quest templates</param>
        /// <returns>Comma seperate string of ids e.g. 123,321,111</returns>
        static string GetIDStringsForQuests(List<QuestTemplate> questList)
        {
            string questString = "";
            for (int i = 0; i < questList.Count; i++)
            {
                QuestTemplate currentQuest = questList[i];
                questString += currentQuest.m_quest_id.ToString();
                if (i != (questList.Count - 1))
                {
                    questString += ",";
                }
            }
            return questString;
        }

        /// <summary>
        /// Find all quests of a given repeatability and delete these from the characters quests & stages. 
        /// (Used to reset daily repeatable quests)
        /// </summary>
        /// <param name="repeatType"></param>
        /// <param name="db"></param>
        internal void DeleteCompleteRepeatableQuests(QuestTemplate.Repeatability repeatType, Database db)
        {
            //store a list of affected players who are online
            List<Player> affectedPlayers = new List<Player>();
            List<QuestStub> questsToDelete = new List<QuestStub>();

            //find a list of all quests that have this completion type
            List<QuestTemplate> questList = GetAllQuestsOfRepeatType(repeatType);

            //create a string with all of the quest id's
            string questIDString = GetIDStringsForQuests(questList);
            //find all complete quest enties with these ID's
            if (questIDString != "" && questList.Count > 0)
            {
                SqlQuery completedQuery = new SqlQuery(db, "select character_id,quest_id from quest where quest_id in (" + questIDString + ") and completed =1");
                if (completedQuery.HasRows)
                {
                    while ((completedQuery.Read()))
                    {
                        //read these into a list of character ID, quest ID
                        int characterID = completedQuery.GetInt32("character_id");
                        int quest_id = completedQuery.GetInt32("quest_id");
                        QuestStub newStub = new QuestStub(quest_id, characterID);
                        questsToDelete.Add(newStub);
                        //if the character is online add to the list of affected players
                        Player onlinePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
                        if (onlinePlayer != null && affectedPlayers.Contains(onlinePlayer) == false)
                        {
                            affectedPlayers.Add(onlinePlayer);
                            Character onlineCharacter = onlinePlayer.m_activeCharacter;
                            if (onlineCharacter != null)
                            {
                                lock (onlineCharacter.m_QuestManager.m_questsAwaitingDeletion)
                                {
                                    onlineCharacter.m_QuestManager.m_questsAwaitingDeletion.Add(newStub);
                                }
                            }
                        }
                    }
                }
                completedQuery.Close();
            }

            //create the delete statements
            string stubString = "";
            //create s string with all the stub data
            for (int i = 0; i < questsToDelete.Count; i++)
            {
                QuestStub currentStub = questsToDelete[i];
                stubString += currentStub.ToString();
                if (i < (questsToDelete.Count - 1))
                {
                    stubString += " or ";
                }
            }
            //delete from stages
            if (questsToDelete.Count > 0 && stubString != "")
            {
                string deleteStagesString = "delete from quest_stage where " + stubString;
                //delete from quests
                string deleteQuestsString = "delete from quest where " + stubString;

                //carry out the transaction
                List<string> transactionStrings = new List<string>();
                transactionStrings.Add(deleteQuestsString);
                transactionStrings.Add(deleteStagesString);
                bool success = db.runCommandsInTransaction(transactionStrings);
            }

        }

		static internal string GetLocaliseQuestName(Player player, int quest_id)
		{
			return Localiser.GetString(textDBIndex, player, quest_id);
		}
	}
}