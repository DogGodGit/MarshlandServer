using System;
using System.Collections.Generic;
using Lidgren.Network;
using XnaGeometry;
using MainServer.Localise;

namespace MainServer
{
    internal class QuestManager
    {
		// #localisation
		public class QuestManagerTextDB : TextEnumDB
		{
			public QuestManagerTextDB() : base(nameof(QuestManager), typeof(TextID)) { }

			public enum TextID
			{
				ALREADY_KNOW_SKILL,                //"You already know {skillName0}"
				CANNOT_TAKE_MORE_BOUNTIES,          //"Cannot take more bounties."
				QUEST_UNAVAILABLE,                  //"Quest not available"
				NOT_ON_QUEST,                       //"Not on Quest, Stage not completable {questId0},{stageId1}"
				STAGE_NOT_COMPLETABLE,              //"Stage not completable {questId0},{stageId1}"
				STAGE_ALREADY_COMPLETED,            //"Stage already Completed {questId0},{stageId1}"
				LOW,                                //"Low"
				MEDIUM,                             //"Medium"
				HIGH,                               //"High"
			}
		}
		public static QuestManagerTextDB textDB = new QuestManagerTextDB();

		Character m_character = null;
        public List<Quest> m_currentQuests = new List<Quest>();
        public List<int> m_completed_quests = new List<int>();
        public List<int> m_available_quests = new List<int>();
        public List<int> m_not_yet_available_quests = new List<int>();
        private static readonly List<int> QUEST_TRACKING_LIST = new List<int>();

        /// <summary>
        /// This list will be locked during its operations
        /// </summary>
        public List<QuestStub> m_questsAwaitingDeletion = new List<QuestStub>();

        Database m_db;

        QuestTemplateManager m_QuestTemplateManager;

        public QuestManager(Database db, Character character, uint characterID)
        {
            m_db = db;
            m_QuestTemplateManager = Program.processor.m_QuestTemplateManager;
            m_character = character;
            SqlQuery query = new SqlQuery(db, "select * from quest where character_id=" + characterID + " order by quest_id");
            if (query.HasRows)
            {
                while (query.Read())
                {
                    int quest_id = query.GetInt32("quest_id");

                    // QuestTracker - as database select pulls all columns, added this call to set the quests tracking state
                    Boolean tracked = query.GetBoolean("quest_tracked");

                    Boolean completed = query.GetBoolean("completed");
                    if (completed)
                    {
                        m_completed_quests.Add(quest_id);
                    }
                    else
                    {
                        QuestTemplate questTemplate = m_QuestTemplateManager.GetQuestTemplate(quest_id);

                        // QuestTracker - setting new variable within Quest class     ->       <-
                        Quest newQuest = new Quest(m_character, quest_id, questTemplate, tracked);
                        m_currentQuests.Add(newQuest);
                        SqlQuery stageQuery = new SqlQuery(m_db, "select * from quest_stage where character_id=" + characterID + " and quest_id=" + quest_id + " order by stage_id");
                        if (stageQuery.HasRows)
                        {
                            while (stageQuery.Read())
                            {
                                int stage_id = stageQuery.GetInt32("stage_id");
                                int stage_open_sum = stageQuery.GetInt32("stage_open_sum");
                                bool stage_completed = stageQuery.GetBoolean("completed");
                                int completion_sum = stageQuery.GetInt32("completion_sum");
                                newQuest.addStage(stage_id, stage_open_sum, completion_sum, stage_completed);
                            }
                        }
                        stageQuery.Close();
                        lockStages(newQuest);
                    }
                }
            }
            query.Close();
        }

        public static void InitializeQuestTrackingList()
        {
            SqlQuery questQuery = new SqlQuery(Program.processor.m_dataDB, "select quest_id from quest_tracking");
            while (questQuery.Read())
            {
                int questId = questQuery.GetInt32("quest_id");
                QUEST_TRACKING_LIST.Add(questId);
            }

            questQuery.Close();
        }

        internal void DeletePendingQuests()
        {
            bool questsChanged = false;
            lock (m_questsAwaitingDeletion)
            {
                if (m_questsAwaitingDeletion.Count > 0)
                {
                    //remove each quest from the list
                    for (int i = 0; i < m_questsAwaitingDeletion.Count; i++)
                    {
                        QuestStub currentStub = m_questsAwaitingDeletion[i];
                        DeleteQuest(currentStub.QuestID, false);
                        questsChanged = true;
                    }
                    //clear the quests that have been dealt with
                    m_questsAwaitingDeletion.Clear();
                }
            }
            //if the characters quests have changed then refresh them
            if (questsChanged)
            {
                SendQuestRefresh();
            }
        }
        /// <summary>
        /// Sets m_locked to false for all stages
        /// </summary>
        /// <param name="quest"></param>
        internal void unlockAllStages(Quest quest)
        {
            for (int i = 0; i < quest.m_QuestStages.Count; i++)
            {
                QuestStage stage = quest.m_QuestStages[i];
                stage.m_locked = false;
            }
        }
        internal static void lockStages(Quest quest)
        {
            for (int i = 0; i < quest.m_QuestStages.Count; i++)
            {
                QuestStage stage = quest.m_QuestStages[i];
                if (!stage.m_locked && stage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.LootItem)
                {
                    QuestStage currentStage = stage;
                    int templateID = stage.m_QuestStageTemplate.m_completionDetails[0].m_id;
                    while (true)
                    {
                        int nextStageID = currentStage.m_QuestStageTemplate.m_nextStage;
                        if (nextStageID < i || nextStageID >= quest.m_QuestStages.Count)
                            break;
                        QuestStage nextStage = quest.m_QuestStages[nextStageID];
                        if (nextStage.m_completed == false)
                            break;
                        if (!nextStage.IsAvailable())
                            break;
                        if (nextStage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.EndQuest)
                            break;
                        if (nextStage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.HandInItem || nextStage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.HandInItemList)
                        {
                            for (int j = 0; j < nextStage.m_QuestStageTemplate.m_completionDetails.Count; j++)
                            {
                                if (nextStage.m_QuestStageTemplate.m_completionDetails[j].m_id == templateID)
                                {
                                    stage.m_locked = true;
                                    break;
                                }
                            }
                        }
                        if (stage.m_locked)
                            break;
                        currentStage = nextStage;
                    }
                }

            }
        }
        internal void WriteQuestListToMessage(NetOutgoingMessage outmsg)
        {
            string quests = m_character.Name + " completed quests=";
            outmsg.WriteVariableInt32(m_completed_quests.Count);
            string completedQuestList = "";
            for (int i = 0; i < m_completed_quests.Count; i++)
            {
                outmsg.WriteVariableInt32(m_completed_quests[i]);
                completedQuestList += "," + m_completed_quests[i];
            }
            if (completedQuestList.Length > 0)
            {
                quests += completedQuestList.Substring(1);
            }
            else
            {
                quests += "none";
            }
            quests += " currentQuests= ";
            string currentQuestsList = "";
            outmsg.WriteVariableInt32(m_currentQuests.Count);
            for (int i = 0; i < m_currentQuests.Count; i++)
            {
                m_currentQuests[i].WriteQuestToMsg(outmsg);
                currentQuestsList += "," + m_currentQuests[i].m_quest_id;
            }
            if (currentQuestsList.Length > 0)
            {
                quests += currentQuestsList.Substring(1);
            }
            else
            {
                quests += "none";
            }
            if (Program.m_LogQuests)
                Program.Display(quests);
        }
        internal bool IsQuestComplete(int quest_id)
        {
            for (int i = 0; i < m_completed_quests.Count; i++)
            {
                if (m_completed_quests[i] == quest_id)
                    return true;
            }
            return false;
        }

        internal bool IsAvailable(int quest_id)
        {
            QuestTemplate template = m_QuestTemplateManager.GetQuestTemplate(quest_id);

            if (template == null)
                return false;

            // PDH - If a quest is marked as Bounty Quest, don't check it's level (this has already been done elsewhere)
            bool levelCheck = template.m_repeatable != QuestTemplate.Repeatability.bounty_board;

            if (!template.IsQuestAllowed(m_character, levelCheck))
                return false;
            if (IsQuestComplete(quest_id))
                return false;
            if (IsQuestStarted(quest_id))
                return false;

            return true;
        }

        internal bool IsNotYetAvailable(int quest_id)
        {

            if ((m_QuestTemplateManager.GetQuestTemplate(quest_id) != null) && m_QuestTemplateManager.GetQuestTemplate(quest_id).IsQuestAllowed(m_character, false) 
                && !IsQuestComplete(quest_id) && !IsQuestStarted(quest_id))
                return true;
            return false;
        }

        internal Quest GetCurrentQuest(int quest_id)
        {
            for (int i = 0; i < m_currentQuests.Count; i++)
            {
                if (m_currentQuests[i].m_quest_id == quest_id)
                    return m_currentQuests[i];
            }
            return null;
        }

        internal bool IsQuestStarted(int quest_id)
        {
            Quest quest = GetCurrentQuest(quest_id);
            if (quest == null)
                return false;
            else
                return true;
        }


        //check to see if we can start a quest
        public string StartQuest(int quest_id)
        {
            if (IsAvailable(quest_id))
            {
                if (!ServerBountyManager.QuestStarted(m_character, quest_id))
                {
					return Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.CANNOT_TAKE_MORE_BOUNTIES);
				}
                QuestTemplate questTemplate = m_QuestTemplateManager.GetQuestTemplate(quest_id);
                Quest newQuest = new Quest(m_character, quest_id, questTemplate, false);  // QuestTracker - tracking status defaults to false
                m_db.runCommandSync("insert into quest (character_id,quest_id,completed,started_date) values (" + m_character.m_character_id + "," + quest_id + ",0,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')");
                m_currentQuests.Add(newQuest);
                Program.Display(m_character.Name + " started quest " + newQuest.m_quest_id);
                string dbUpdateString = "";
                for (int i = 0; i < questTemplate.m_QuestStageTemplates.Count; i++)
                {
                    newQuest.addNewStage(m_db, m_character.m_character_id, quest_id, i);
                    dbUpdateString += ",(" + m_character.m_character_id + "," + quest_id + "," + i + ",0,0,0)";

                }
                if (dbUpdateString.Length > 0)
                {
                    m_db.runCommandSync("insert into quest_stage (character_id,quest_id,stage_id,stage_open_sum,completed,completion_sum) values " + dbUpdateString.Substring(1));
                }
                m_character.SignpostsNeedRechecked();


                return "";
            }
            else
            {
                Program.Display(m_character.Name + " failed to start quest " + quest_id + " as it's not available");
				return Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.QUEST_UNAVAILABLE);
			}
        }

        /// <summary>
        /// send a list of quests available in the current zone to the player
        /// </summary>
        /// <param name="outMsg">already created message</param>
        public void writeAvailableQuestsToMessage(NetOutgoingMessage outMsg)
        {
            m_available_quests.Clear();
            m_not_yet_available_quests.Clear();

            //iterate through all the quest templates
            foreach (var template in m_QuestTemplateManager.QuestTemplates.Values)
            {
                if (template.m_zone == m_character.m_zone.m_zone_id)
                {
                    if (IsAvailable(template.m_quest_id))
                    {
                        m_available_quests.Add(template.m_quest_id);
                    }
                    else if (IsNotYetAvailable(template.m_quest_id))
                    {
                        m_not_yet_available_quests.Add(template.m_quest_id);
                    }
                }
            }
            
            outMsg.WriteVariableInt32(m_available_quests.Count);
            string availableQuests = m_character.Name + " available quests = ";
            string availabileQuestsList = "";
            for (int i = 0; i < m_available_quests.Count; i++)
            {
                outMsg.WriteVariableInt32(m_available_quests[i]);
                availabileQuestsList += "," + m_available_quests[i];
            }
            if (availabileQuestsList.Length > 0)
            {
                availableQuests += availabileQuestsList.Substring(1);
            }
            else
            {
                availableQuests += "none";
            }
            outMsg.WriteVariableInt32(m_not_yet_available_quests.Count);
            availableQuests = availableQuests + ", not yet available quests = ";
            string notyetavailabileQuestsList = "";
            for (int i = 0; i < m_not_yet_available_quests.Count; i++)
            {
                outMsg.WriteVariableInt32(m_not_yet_available_quests[i]);
                notyetavailabileQuestsList += "," + m_not_yet_available_quests[i];
            }
            if (notyetavailabileQuestsList.Length > 0)
            {
                availableQuests += notyetavailabileQuestsList.Substring(1);
            }
            else
            {
                availableQuests += "none";
            }
            if (Program.m_LogQuests)
                Program.Display(availableQuests);
        }
        
        /// <summary>
        /// check if the mob killed was one of the ones required by a quest
        /// </summary>
        /// <param name="mobTemplateID"></param>
        internal void checkKillRequired(int mobTemplateID)
        {
            for (int i = 0; i < m_currentQuests.Count; i++)
            {
                Quest quest = m_currentQuests[i];
                for (int j = 0; j < quest.m_QuestStages.Count; j++)
                {
                    QuestStage stage = quest.m_QuestStages[j];
                    int requiredMob = stage.requiredMob(mobTemplateID);
                    if (requiredMob == 0)// completed stage
                    {
                        m_db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
                        completeStage(quest.m_quest_id, j);
                    }
                    else if (requiredMob > 0)// still some more to kill, tell the player
                    {
                        m_db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
                        NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                        outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
                        outmsg.Write((byte)1);//valid response
                        outmsg.Write((byte)0);//stage not yet complete
                        outmsg.WriteVariableInt32(quest.m_quest_id);
                        outmsg.WriteVariableInt32(j);
                        outmsg.WriteVariableInt32((int)stage.m_QuestStageTemplate.m_completion_type);
                        outmsg.WriteVariableInt32(mobTemplateID);
                        outmsg.WriteVariableInt32(requiredMob);
                        outmsg.WriteVariableInt32(stage.m_QuestStageTemplate.m_nextStage);
                        outmsg.Write((byte)0);//no inventory update
                        Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStageProgressReply);


                    }
                }
            }
        }

        
        /// <summary>
        /// check if quest_completion was one of the ones required by a quest
        /// </summary>
        /// <param name="completedQuestID"></param>
        internal void checkCompletedQuestRequired(int completedQuestID)
        {
            for (int i = 0; i < m_currentQuests.Count; i++)
            {
                Quest quest = m_currentQuests[i];
                for (int j = 0; j < quest.m_QuestStages.Count; j++)
                {
                    QuestStage stage = quest.m_QuestStages[j];
                    int requiredQuest = stage.requiredQuestComplete(completedQuestID);
                    if (requiredQuest == 0)// completed stage
                    {
                        m_db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
                        completeStage(quest.m_quest_id, j);
                    }
                    else if (requiredQuest > 0)// still some more to kill, tell the player
                    {
                        m_db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
                        NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                        outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
                        outmsg.Write((byte)1);//valid response
                        outmsg.Write((byte)0);//stage not yet complete
                        outmsg.WriteVariableInt32(quest.m_quest_id);
                        outmsg.WriteVariableInt32(j);
                        outmsg.WriteVariableInt32((int)stage.m_QuestStageTemplate.m_completion_type);
                        outmsg.WriteVariableInt32(stage.m_QuestStageTemplate.m_nextStage);
                        outmsg.Write((byte)0);//no inventory update
                        Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStageProgressReply);


                    }
                }
            }
        }
        
        /// <summary>
        /// check if the skill cast was one of the ones required by a quest
        /// </summary>
        /// <param name="skillID"></param>
        /// <param name="mobTemplateID"></param>
        internal void checkSkillRequired(int skillID, int mobTemplateID)
        {
            for (int i = 0; i < m_currentQuests.Count; i++)
            {
                Quest quest = m_currentQuests[i];
                for (int j = 0; j < quest.m_QuestStages.Count; j++)
                {
                    QuestStage stage = quest.m_QuestStages[j];
                    int requiredSkill = stage.requiredSkill(skillID, mobTemplateID);
                    if (requiredSkill == 0)// completed stage
                    {
                        m_db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
                        completeStage(quest.m_quest_id, j);
                    }
                    else if (requiredSkill > 0)// still some more to kill, tell the player
                    {
                        m_db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
                        NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                        outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
                        outmsg.Write((byte)1);//valid response
                        outmsg.Write((byte)0);//stage not yet complete
                        outmsg.WriteVariableInt32(quest.m_quest_id);
                        outmsg.WriteVariableInt32(j);
                        outmsg.WriteVariableInt32((int)stage.m_QuestStageTemplate.m_completion_type);
                        outmsg.WriteVariableInt32(skillID);
                        outmsg.WriteVariableInt32(requiredSkill);
                        outmsg.WriteVariableInt32(stage.m_QuestStageTemplate.m_nextStage);
                        outmsg.Write((byte)0);//no inventory update
                        Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStageProgressReply);


                    }
                }
            }
        }


        internal void checkIfItemAffectsStage(int itemTemplateID)
        {
            int itemCount = m_character.m_inventory.GetItemCount(itemTemplateID);
            for (int i = 0; i < m_currentQuests.Count; i++)
            {
                Quest quest = m_currentQuests[i];
                for (int j = 0; j < quest.m_QuestStages.Count; j++)
                {
                    QuestStage stage = quest.m_QuestStages[j];
                    int requiredItem = stage.requiredLootItem(itemTemplateID, itemCount);
                    if (requiredItem == 0)// completed stage
                    {
                        m_db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
                        completeStage(quest.m_quest_id, j);
                    }
                    else if (requiredItem > 0)// still some more to kill, tell the player
                    {
                        if (!stage.m_completed)
                        {
                            m_db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
                            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                            outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
                            outmsg.Write((byte)1);//valid response
                            outmsg.WriteVariableInt32(0);//stage not yet complete
                            outmsg.WriteVariableInt32(quest.m_quest_id);
                            outmsg.WriteVariableInt32(j);
                            outmsg.WriteVariableInt32((int)stage.m_QuestStageTemplate.m_completion_type);
                            outmsg.WriteVariableInt32(itemTemplateID);
                            outmsg.WriteVariableInt32(requiredItem);
                            outmsg.WriteVariableInt32(stage.m_QuestStageTemplate.m_nextStage);
                            outmsg.Write((byte)0);//no inventory update
                            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStageProgressReply);
                        }
                        else
                        {
                            unCompleteStage(quest, j);
                        }

                    }
                }
            }
        }

        private void unCompleteStage(Quest quest, int j)
        {
            QuestStage stage = quest.m_QuestStages[j];
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
            outmsg.Write((byte)1);//valid response
            outmsg.WriteVariableInt32(-1);//stage not yet complete
            outmsg.WriteVariableInt32(quest.m_quest_id);
            outmsg.WriteVariableInt32(j);
            outmsg.WriteVariableInt32((int)stage.m_QuestStageTemplate.m_completion_type);
            if ((int)stage.m_QuestStageTemplate.m_completion_type < 2)
            {
                outmsg.WriteVariableInt32(stage.m_QuestStageTemplate.m_completionDetails[0].m_id);
                outmsg.WriteVariableInt32(stage.m_QuestStageTemplate.m_completionDetails[0].m_total - stage.m_completion_sum);
            }
            outmsg.WriteVariableInt32(stage.m_QuestStageTemplate.m_nextStage);
            outmsg.Write((byte)0);//no inventory update
            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStageProgressReply);
            stage.m_completed = false;
            m_db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + ",completed=false,completed_date=null where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
            int nextStageID = stage.m_QuestStageTemplate.m_nextStage;
            QuestStage nextStage = quest.m_QuestStages[nextStageID];
            nextStage.m_stage_open_sum--;
            nextStage.m_completion_sum = 0;
            if (nextStage.m_stage_open_sum < 0)
            {
                Program.Display("error in uncompleteStage " + quest.m_quest_id + "," + j + " for character " + m_character.m_name + " [" + m_character.m_character_id + "]");
            }
            m_db.runCommandSync("update quest_stage set stage_open_sum=" + nextStage.m_stage_open_sum + ",completion_sum=" + nextStage.m_completion_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest.m_quest_id + " and stage_id=" + nextStageID);
            if (nextStage.m_completed)
            {
                unCompleteStage(quest, nextStageID);
            }

        }

        private static void unCompleteStageOffline(Quest quest, int j, int characterID)
        {
            QuestStage stage = quest.m_QuestStages[j];

            Database db = Program.processor.m_worldDB;
            stage.m_completed = false;
            db.runCommandSync("update quest_stage set completion_sum=" + stage.m_completion_sum + ",completed=false,completed_date=null where character_id=" + characterID + " and quest_id=" + quest.m_quest_id + " and stage_id=" + j);
            int nextStageID = stage.m_QuestStageTemplate.m_nextStage;
            QuestStage nextStage = quest.m_QuestStages[nextStageID];
            nextStage.m_stage_open_sum--;
            if (nextStage.m_stage_open_sum < 0)
            {
                Program.Display("error in unCompleteStageOffline " + quest.m_quest_id + "," + j + " for character  [" + characterID + "]");
            }
            db.runCommandSync("update quest_stage set stage_open_sum=" + nextStage.m_stage_open_sum + " where character_id=" + characterID + " and quest_id=" + quest.m_quest_id + " and stage_id=" + nextStageID);
            if (nextStage.m_completed)
            {
                unCompleteStageOffline(quest, nextStageID, characterID);
            }


        }

        internal void tryCompleteStage(int quest_id, int stage_id)
        {
            Quest quest = GetCurrentQuest(quest_id);
            if (quest != null && stage_id < quest.m_QuestStages.Count)
            {

                QuestStage stage = quest.m_QuestStages[stage_id];
                QuestStageTemplate.CompletionType completionType = stage.m_QuestStageTemplate.m_completion_type;
                if (completionType == QuestStageTemplate.CompletionType.ConversationOption
                    || completionType == QuestStageTemplate.CompletionType.HandInItem
                    || completionType == QuestStageTemplate.CompletionType.HandInItemList
                    || completionType == QuestStageTemplate.CompletionType.HandInGold)
                {
                    completeStage(quest_id, stage_id);
                }
            }
        }

        /// <summary>
        /// Used if the support tool has changed the quests in some way, to prevent corruption
        /// Send the complete quest list 
        /// sends the current zone specific quest info 
        /// </summary>
        internal void SendQuestRefresh()
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();

            outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestRefreshMessage);

            WriteQuestListToMessage(outmsg);
            writeAvailableQuestsToMessage(outmsg);


            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestRefreshMessage);


        }

        /// <summary>
        /// Build self contained message that updates quest id's for the zone the players is in
        /// </summary>
        internal void SendQuestsInZoneRefresh()
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestsInZoneRefresh);            
            writeAvailableQuestsToMessage(outmsg);           
            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestsInZoneRefresh);
        }

        internal void completeStage(int quest_id, int stage_id)
        {
            Quest quest = GetCurrentQuest(quest_id);
            if (quest == null)
            {
                NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.NOT_ON_QUEST);
				locText = string.Format(locText, quest_id, stage_id);
				outmsg.Write(locText);
				Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStageProgressReply);
                return;
            }

            QuestStage stage = quest.m_QuestStages[stage_id];

            if (!stage.m_completed && stage.IsAvailable())
            {
                bool completable = false;

                switch (stage.m_QuestStageTemplate.m_completion_type)
                {
                    // Easy case hand in item
                    case (QuestStageTemplate.CompletionType.HandInItem):
                        completable = (m_character.m_inventory.checkHasItems(stage.m_QuestStageTemplate.m_completionDetails[0].m_id)
                            >= stage.m_QuestStageTemplate.m_completionDetails[0].m_total);
                        break;
                    // Hand in item list
                    case (QuestStageTemplate.CompletionType.HandInItemList):
                        completable = true;
                        for (int i = 0; i < stage.m_QuestStageTemplate.m_completionDetails.Count; i++)
                        {
                            if (m_character.m_inventory.checkHasItems(stage.m_QuestStageTemplate.m_completionDetails[i].m_id) < stage.m_QuestStageTemplate.m_completionDetails[i].m_total)
                            {
                                completable = false;
                                break;
                            }

                        }
                        break;
                    // Hand in gold
                    case (QuestStageTemplate.CompletionType.HandInGold):
                        if (m_character.m_inventory.m_coins < stage.m_QuestStageTemplate.m_completionDetails[0].m_total)
                        {
                            completable = false;
                        }
                        break;
                    // Otherwise
                    default:
                        completable = true;
                        break;
                }

                if (completable)
                {
                    bool sendUpdateInventory = false;
                    // Must be complete to lock the stages
                    stage.m_completed = true;
                    if (stage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.HandInItem || stage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.HandInItemList)
                    {
                        for (int i = 0; i < stage.m_QuestStageTemplate.m_completionDetails.Count; i++)
                        {
                            m_character.m_inventory.removeQuestItems(quest_id, stage_id, stage.m_QuestStageTemplate.m_completionDetails[i].m_id, stage.m_QuestStageTemplate.m_completionDetails[i].m_total);
                        }
                        sendUpdateInventory = true;
                        lockStages(quest);
                    }
                    else if (stage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.HandInGold)
                    {
                        sendUpdateInventory = true;
                        lockStages(quest);
                        m_character.updateCoins(-stage.m_QuestStageTemplate.m_completionDetails[0].m_total);
                    }
                    //stage.m_completed = true; // commented out before Open Sum Fix
                    switch (quest_id)
                    {
                        case 77:
                            {
                                m_character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.SHALEMONT_EXPLORER, 1);
                                break;
                            }
                        case 78:
                            {
                                m_character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.STONEVALE_EXPLORER, 1);
                                break;
                            }
                        case 79:
                            {
                                m_character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.LORD_OF_SHALEMONT, 1);
                                break;
                            }
                        case 80:
                            {
                                m_character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.LORD_OF_STONEVALE, 1);
                                break;
                            }
                    }

                    m_db.runCommandSync("update quest_stage set completed=1,completed_date='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id + " and stage_id=" + stage_id);
                    int nextStageID = stage.m_QuestStageTemplate.m_nextStage;
                    QuestStage nextStage = quest.m_QuestStages[nextStageID];

                    // Open Sum Fix - possible recursion within IncreaseAvailabilityCounter
                    nextStage.IncreaseAvailabilityCounter(quest);
                    bool isNowAvailable = nextStage.IsAvailable();

                    // Open Sum Fix - if multiple stages are unlocked - update database correctly
                    if (quest.m_num_unlocked_stages > 1)
                    {
                        for (int i = 0; i < quest.m_num_unlocked_stages; i++)
                        {
                            m_db.runCommandSync("update quest_stage set stage_open_sum=" + quest.m_QuestStages[nextStageID + i].m_stage_open_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id + " and stage_id=" + (nextStageID + i));
                        }
                    }
                    else
                    {
                        m_db.runCommandSync("update quest_stage set stage_open_sum=" + nextStage.m_stage_open_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id + " and stage_id=" + nextStageID);
                    }

                    Program.Display(m_character.m_name + " has completed quest stage " + quest_id + "," + stage_id);
                    if (isNowAvailable)
                    {
                        if (nextStage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.EndQuest)
                        {
                            m_db.runCommandSync("update quest_stage set completed=1,completed_date='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id + " and stage_id=" + nextStageID);
                            completeQuest(quest_id);
                        }
                        else
                        {
                            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                            outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
                            outmsg.Write((byte)1); // valid response
                            outmsg.WriteVariableInt32(1); // stage is complete
                            outmsg.WriteVariableInt32(quest_id);
                            outmsg.WriteVariableInt32(stage_id);
                            outmsg.WriteVariableInt32(nextStageID);
                            outmsg.WriteVariableInt32(quest.m_num_unlocked_stages); // Open Sum Fix - passing the number of stages to unlock beginning with nextStageID above
                            if (sendUpdateInventory)
                            {
                                outmsg.Write((byte)1);
                                m_character.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);
                            }
                            else
                            {
                                outmsg.Write((byte)0);
                            }

                            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal,
                                NetworkCommandType.QuestStageProgressReply);

                            // Open Sum Fix - reset number of unlocked stages for this quest
                            quest.m_num_unlocked_stages = 0;

                            checkIfAlreadyHaveRequirements(quest_id);
                        }
                    }
                    // Send player message that stage completed
                    else
                    {
                        NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                        outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
                        outmsg.Write((byte)1); // valid response
                        outmsg.WriteVariableInt32(1); // stage is complete
                        outmsg.WriteVariableInt32(quest_id);
                        outmsg.WriteVariableInt32(stage_id);
                        outmsg.WriteVariableInt32(-1); // next stage not open yet
                        outmsg.WriteVariableInt32(quest.m_num_unlocked_stages); // Open Sum Fix - not required but client code for this and above message is the same 
                        if (sendUpdateInventory)
                        {
                            outmsg.Write((byte)1);
                            m_character.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);
                        }
                        else
                        {
                            outmsg.Write((byte)0);
                        }

                        Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal,
                            NetworkCommandType.QuestStageProgressReply);
                    }

                    // Open Sum Fix - reset number of unlocked stages for this quest
                    quest.m_num_unlocked_stages = 0;

                    if (stage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.HandInItem || stage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.HandInItemList)
                    {
                        for (int i = 0; i < stage.m_QuestStageTemplate.m_completionDetails.Count; i++)
                        {
                            checkIfItemAffectsStage(stage.m_QuestStageTemplate.m_completionDetails[i].m_id);
                        }
                    }
                }
                else
                {
                    NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                    outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
                    outmsg.Write((byte)0);
					string locText = Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.STAGE_NOT_COMPLETABLE);
					locText = string.Format(locText, quest_id, stage_id);
					outmsg.Write(locText);
					Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStageProgressReply);
                }

                //add the event to the character
                List<float> eventParams = new List<float>();
                eventParams.Add(quest_id);
                eventParams.Add(stage_id);
                Signposting.CharacterEvent newEvent = new MainServer.Signposting.CharacterEvent(Signposting.CharacterEvent.EventType.QuestStageComplete, eventParams);
                m_character.RecentEvents.Add(newEvent);
                m_character.SignpostsNeedRechecked();
            }
            else
            {
                NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.STAGE_ALREADY_COMPLETED);
				locText = string.Format(locText, quest_id, stage_id);
				outmsg.Write(locText);
				Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStageProgressReply);

            }

        }

        internal void SendQuestStageProgress(Quest quest, QuestStage stage, bool sendUpdateInventory, int nextStageID)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStageProgressReply);
            outmsg.Write((byte)1);//valid response
            outmsg.WriteVariableInt32(1);//stage is complete
            outmsg.WriteVariableInt32(quest.m_QuestTemplate.m_quest_id);
            outmsg.WriteVariableInt32(stage.m_QuestStageTemplate.m_stage_id);
            outmsg.WriteVariableInt32(nextStageID);
            if (sendUpdateInventory)
            {
                outmsg.Write((byte)1);
                m_character.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);
            }
            else
            {
                outmsg.Write((byte)0);
            }
            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStageProgressReply);

        }

        #region Quest Preview Methods

        /// <summary>
        /// Function for calculating average XP used in quest preview banding.
        /// </summary>
        /// <param name="quest_id"></param>
        /// <returns></returns>
        private int calculateAverageXp(int quest_id)
        {
            //Get quest template + data;
            QuestTemplate questTemplate = m_QuestTemplateManager.GetQuestTemplate(quest_id);
            Quest quest = new Quest(m_character, quest_id, questTemplate, false); // QuestTracker - defaults to false
            int quest_level = quest.Quest_Level;
            
            //Query the database for quests at player level & player level +/- 1;
            SqlQuery query = new SqlQuery(Program.processor.m_dataDB,
                "SELECT SUM(xp_reward) WHT_RBT FROM quest_templates WHERE quest_level IN(" + quest_level
                + "," + (quest_level - 1) + "," + (quest_level + 1) + ")"); //quest.Quest_Level);
            SqlQuery countQuery = new SqlQuery(Program.processor.m_dataDB,
                "SELECT COUNT(NULLIF(xp_reward,0)) count FROM quest_templates WHERE quest_level IN(" + quest_level + "," + (quest_level - 1) + "," + (quest_level + 1) + ")"); //quest.Quest_Level);
            
            int avgXP = 0;
            int total = 0;
            int count = 0;

            //Read total XP of selected quests.
            if (query.HasRows)
            {
                while (query.Read())
                {
                    total = query.GetInt32("WHT_RBT");
                }
            }
            query.Close();

            //Read number of quests at level.
            if (countQuery.HasRows)
            {
                while (countQuery.Read())
                {
                    count = countQuery.GetInt32("count");
                    
                }
            }
            countQuery.Close();

            //Calculate average XP.
            if (count != 0)
            {
                avgXP = total / count;
    
            }

            return avgXP;
        }

        /// <summary>
        /// Function used to determine XP banding string for quest preview
        /// </summary>
        /// <param name="quest"></param>
        /// <returns></returns>
        private string calculateXpBand(Quest quest)
        {
            //Let's get the average XP.
            int avgXP = calculateAverageXp(quest.m_quest_id);
            
            //Calculate the final XP reward after XP acceleration.
            int finalxpReward = calculateFinalXpReward(quest);

            //Return the band string based on some parameters-
            string band = "";
			if (m_character.GetRelevantLevel(quest.m_QuestTemplate) == 1 || m_character.GetRelevantLevel(quest.m_QuestTemplate) == 2)
            {
				band = Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.HIGH);
			}
            else
            {
                //XP band parameters/Names should be changed here.
                if (finalxpReward == 0)
                {
                    band = "";
                }
                else if (finalxpReward < (avgXP / 2))
                {
					band = Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.LOW);
				}
                else if ((avgXP / 2) < finalxpReward && finalxpReward < (avgXP + avgXP / 2))
                {
					band = Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.MEDIUM);
				}
                else if (finalxpReward > (avgXP + avgXP / 2))
                {
					band = Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.HIGH);
				}
            }
            return band;
        }

        /// <summary>
        /// Calculates the final XP reward from a quest based on XP accelerator and the difference between quest and player level.
        /// </summary>
        /// <param name="quest"></param>
        /// <returns></returns>
        private int calculateFinalXpReward(Quest quest)
        {
            int basexpReward = quest.XP_Reward;
			int diff = m_character.GetRelevantLevel(quest.m_QuestTemplate) - quest.Quest_Level;
            if (diff < 0) diff = 0;
            int finalxpReward = (int)(basexpReward * (Math.Pow(Character.EXPERIENCE_ACCELLERATOR, diff)));
            return finalxpReward;
        }
        
        /// <summary>
        /// Function to pack a message for client with quest reward data(XP, Gold, Items, Skills).
        /// </summary>
        /// <param name="quest_id"></param>
        /// <param name="from_log"></param>
        internal void previewQuestRewards(int quest_id, bool from_log)
        {
            //Get Quest template + data;
            QuestTemplate questTemplate = m_QuestTemplateManager.GetQuestTemplate(quest_id);
            Quest quest = new Quest(m_character, quest_id, questTemplate, false); // QuestTracker - defaults to false
            if (questTemplate == null)
            {
                Program.Display("Quest Template for quest id: " + quest_id + " does not exist!");
                return;
            }

            int finalxpReward = calculateFinalXpReward(quest);
            int coinsReward = quest.Coins_Reward;
            
            //Start packing message with data.
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.PreviewQuestRewards);
            outmsg.Write(from_log);
            outmsg.WriteVariableInt32(quest_id);
            
            outmsg.WriteVariableInt32(finalxpReward);
            outmsg.Write(calculateXpBand(quest));
            outmsg.WriteVariableInt32(coinsReward);
            outmsg.WriteVariableInt32(questTemplate.m_quest_type);
            
            int itemReward = 0;
            bool skillReward = false;
            bool lootReward;

            //Set boolean for whether quest uses loot table.
            if (questTemplate.m_uses_loot_table)
            {
                lootReward = true;
            }
            else
            {
                lootReward = false;
            }
            
            //Check whether quest rewards item, skill or loot table rewards.
            if (quest.getItemReward(m_character.m_class.m_classType) != 0)
            {
                itemReward = quest.getItemReward(m_character.m_class.m_classType);
                outmsg.WriteVariableInt32(itemReward);
            }
            else if (quest.getQuestReward(m_character.m_class.m_classType).Count > 0 && quest.m_QuestTemplate.m_reward_type!=3)
            {
                List<NewQuestReward> rewards = quest.getQuestReward(m_character.m_class.m_classType);
                outmsg.WriteVariableInt32(rewards[0].m_rewardID);
            }
            else if (quest.m_QuestTemplate.m_reward_type == 3)
            {
                List<NewQuestReward> rewards = quest.getQuestReward(m_character.m_class.m_classType);
	            bool somethingWritten = false;
				//in rare case where we award both item & skill, make sure we match up the skill id not the item id
	            foreach (NewQuestReward reward in rewards)
	            {
		            if (reward.m_rewardType == NewQuestReward.Reward_Type.Skill)
		            {
			            outmsg.WriteVariableInt32(reward.m_rewardID);
			            somethingWritten = true;
		            }
	            }
				//if we didn't get a matching skill, dump out some int into the message so I can be unpacked correctly.
				//the client will check for null
				if(somethingWritten == false)
					outmsg.WriteVariableInt32(-1);
                skillReward = true;
            }
            else
            {
                // else fill message with -1 as item reward
                outmsg.WriteVariableInt32(-1);
            }


            outmsg.Write(skillReward);
            outmsg.Write(lootReward);
            
            //Check the number of items rewarded by the quest.
            int itemCount = quest.Item_Count;
            outmsg.WriteVariableInt32(itemCount);

            // check on faction reward            
            if(quest.m_QuestTemplate.HasFactionReward())
            {
                outmsg.WriteVariableInt32(quest.m_QuestTemplate.FactionPointReward.Id);
                outmsg.WriteVariableInt32(quest.m_QuestTemplate.FactionPointReward.Points);
            }
            else
            {
                outmsg.WriteVariableInt32(-1);
                outmsg.WriteVariableInt32(0);
            }
            
            //Only send message if the quest rewards something.
            if (coinsReward > 0 || finalxpReward > 0 || itemCount > 0 && itemReward > 0 || quest.m_QuestTemplate.HasFactionReward())
            {
                Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PreviewQuestRewards);
            }
        }
        #endregion

        internal void completeQuest(int quest_id)
        {
           
            Quest quest = GetCurrentQuest(quest_id);
            
            // bail if we can't find this quest
            if (quest == null)
            {
                return;
            }

            // if repeatable delete record so it can be taken again
            if (quest.m_QuestTemplate.m_repeatable == QuestTemplate.Repeatability.instant_repeat)
            {
                m_db.runCommandSync("delete from quest where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id);
                m_db.runCommandSync("delete from quest_stage where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id);

            }
            else
            {
                Program.Display(m_character.Name + " completing quest " + quest_id);
                m_db.runCommandSync("update quest set completed=true ,completed_date='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id);
            }

            // check if this has quest is part of a stage in another questline
            checkCompletedQuestRequired(quest_id);
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestCompleteReply);
            outmsg.Write((byte)1);
            outmsg.WriteVariableInt32(quest_id);

            // calculate xp reward
            int basexpReward = quest.XP_Reward;
            int diff = m_character.GetRelevantLevel(quest.m_QuestTemplate) - quest.Quest_Level;
            if (diff < 0) diff = 0;

            int finalxpReward = (int)(basexpReward * (Math.Pow(Character.EXPERIENCE_ACCELLERATOR, diff)));
            
            // other rewards
            CombatEntity.LevelType questType = (CombatEntity.LevelType)quest.m_QuestTemplate.m_quest_type;
            int coinsReward = quest.Coins_Reward;
            finalxpReward = m_character.updateCoinsAndXP(coinsReward, finalxpReward,questType);
            outmsg.WriteVariableInt32(finalxpReward);
            outmsg.Write(m_character.getVisibleExperience(questType));


            outmsg.WriteVariableInt32(coinsReward);
            int itemReward = 0;
            outmsg.WriteVariableInt32(itemReward);
            int itemCount = 0;
            outmsg.WriteVariableInt32(itemCount);
            
            // loot rewards
            List<LootDetails> lootWon = new List<LootDetails>();
            List<NewQuestReward> questRewards = quest.getQuestReward(m_character.m_class.m_classType);
            for (int i = 0; i < questRewards.Count; i++)
            {
                NewQuestReward currentReward = questRewards[i];
                GiveReward(currentReward, quest, lootWon);
            }

            outmsg.WriteVariableInt32(lootWon.Count);
            for (int i = 0; i < lootWon.Count; i++)
            {
                outmsg.WriteVariableInt32(lootWon[i].m_templateID);
                outmsg.WriteVariableInt32(lootWon[i].m_quantity);
            }

            // check for faction reward
            if(quest.m_QuestTemplate.HasFactionReward())
            {
                this.m_character.FactionManager.AlterFactionPoints(quest.m_QuestTemplate.FactionPointReward.Id, quest.m_QuestTemplate.FactionPointReward.Points);
            }

            // Mobile App Tracking - track quests in dynamic quest tracking list where valid.
            Byte mobileAppTracking = QUEST_TRACKING_LIST.Contains(quest_id) ? (Byte)1 : (Byte)0;
            outmsg.Write(mobileAppTracking);

            // update players inventory with money
            m_character.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);

            // remove from current quests
            m_currentQuests.Remove(quest);
            if (quest.m_QuestTemplate.m_repeatable != QuestTemplate.Repeatability.instant_repeat)
            {
                m_completed_quests.Add(quest_id);
            }
            writeAvailableQuestsToMessage(outmsg);

            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestCompleteReply);

            // I don't think this is needed anymore...
            m_character.increaseRanking(RankingsManager.RANKING_TYPE.QUEST_COMPLETED, 1, false);
            Program.processor.CompetitionManager.UpdateCompetition(m_character, Competitions.CompetitionType.QUESTS_COMPLETED, quest.m_quest_id);
            m_character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.QUESTER, 1);
            m_character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.ADVENTURER, 1);

            if (Program.m_LogAnalytics)
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                logAnalytics.missionCompleted(m_character.m_player, quest.m_QuestTemplate.m_questName, quest_id.ToString(), lootWon, coinsReward, finalxpReward);
            }

            // Bounty Manager
            ServerBountyManager.QuestCompleted(m_character, quest_id);

            // add the event to the character
            List<float> eventParams = new List<float>();
            eventParams.Add(quest_id);
            Signposting.CharacterEvent newEvent = new MainServer.Signposting.CharacterEvent(Signposting.CharacterEvent.EventType.QuestComplete, eventParams);
            m_character.RecentEvents.Add(newEvent);
            m_character.SignpostsNeedRechecked();

        }


        internal void GiveReward(NewQuestReward theReward, Quest quest, List<LootDetails> totalLoot)
        {

            switch (theReward.m_rewardType)
            {
                case NewQuestReward.Reward_Type.Item:
                    {
                        int quantity = 0;
                        string lootListParams = theReward.m_paramVal;
                        string[] lootListSplit = lootListParams.Split(new char[] { ',' });
                        if (lootListSplit.Length > 0)
                        {
                            quantity = int.Parse(lootListSplit[0]);
                        }
                        int templateID = theReward.m_rewardID;
                        if (quantity > 0)
                        {
                            Item item = m_character.m_inventory.AddNewItemToCharacterInventory(templateID, quantity, false);
                            checkIfItemAffectsStage(item.m_template_id);
                            if (item == null)
                            {
                                Program.Display("error in item reward for quest " + quest.m_quest_id + " item " + theReward.m_rewardID + " quantity " + quantity + " was not given to " + m_character.GetIDString());

                            }
                            LootDetails lootData = new LootDetails(theReward.m_rewardID, quantity);
                            LootDetails.AddLootToCompiledList(lootData, totalLoot);
                        }
                        break;
                    }
                case NewQuestReward.Reward_Type.LootTable:
                    {
                        int quantity = 0;
                        string lootListParams = theReward.m_paramVal;
                        string[] lootListSplit = lootListParams.Split(new char[] { ',' });
                        if (lootListSplit.Length > 0)
                        {
                            quantity = int.Parse(lootListSplit[0]);
                        }

                        List<LootDetails> lootTableDrops = new List<LootDetails>();
                        for (int i = 0; i < quantity; i++)
                        {
                            LootDetails lootData = LootSetManager.getLootTable(theReward.m_rewardID).getLootItem();
                            lootTableDrops.Add(lootData);

                        }
                        //loot data is created specially when needed so can be combined
                        List<LootDetails> finalTableDrops = new List<LootDetails>();
                        for (int i = 0; i < lootTableDrops.Count; i++)
                        {
                            LootDetails currentDetails = lootTableDrops[i];
                            if (currentDetails == null) continue;
                            LootDetails.AddLootToCompiledList(currentDetails, finalTableDrops);
                        }
                        for (int i = 0; i < finalTableDrops.Count; i++)
                        {
                            LootDetails currentDetails = finalTableDrops[i];
                            Item item = m_character.m_inventory.AddNewItemToCharacterInventory(currentDetails.m_templateID, currentDetails.m_quantity, false);
                            if (item == null)
                            {
                                Program.Display("error in loot table reward for quest " + quest.m_quest_id + " item " + currentDetails.m_templateID + " quantity " + currentDetails.m_quantity + " was not given to " + m_character.GetIDString());

                            }
                            LootDetails.AddLootToCompiledList(currentDetails, totalLoot);
                        }
                        break;
                    }

                case NewQuestReward.Reward_Type.Skill:
                    {
                        ServerControlledEntity caster = null;

                        string skillParams = theReward.m_paramVal;
                        string[] skillSplit = skillParams.Split(new char[] { ',' });
                        int skillLevel = 0;
                        int casterTemplateID = -1;
                        if (skillSplit.Length > 0)
                        {
                            skillLevel = int.Parse(skillSplit[0]);
                        }
                        if (skillSplit.Length > 1)
                        {
                            casterTemplateID = int.Parse(skillSplit[1]);
                        }
                        if (casterTemplateID >= 0 && m_character.CurrentPartition != null)
                        {
                            float maxMobRange = 10;
                            List<ServerControlledEntity> closeMobs = new List<ServerControlledEntity>();
                            m_character.CurrentPartition.AddLocalMobsInRangeToList(m_character, m_character.CurrentPosition.m_position, maxMobRange, closeMobs, partitioning.ZonePartition.ENTITY_TYPE.ET_MOB, null);
                            for (int i = 0; i < closeMobs.Count && caster == null; i++)
                            {
                                ServerControlledEntity currentEnt = closeMobs[i];
                                if (currentEnt != null && currentEnt.Template != null && currentEnt.Template.m_templateID == casterTemplateID)
                                {
                                    caster = currentEnt;
                                }
                            }
                        }
                        SkillTemplate skillTemp = SkillTemplateManager.GetItemForID((SKILL_TYPE)theReward.m_rewardID);
                        if (skillTemp != null)
                        {
                            EntitySkill entitySkill = new EntitySkill(skillTemp);
                            entitySkill.SkillLevel = skillLevel;
                            CombatEntity skillCaster = caster;
                            if (skillCaster == null)
                            {
                                skillCaster = m_character;
                            }
                            if ((int)entitySkill.SkillID >= SkillTemplate.LEARN_SKILL_START_ID && (int)entitySkill.SkillID < SkillTemplate.LEARN_SKILL_END_ID)
                            {
                                SKILL_TYPE skillToLearn = (SKILL_TYPE)((int)entitySkill.SkillID - 1000);
                                bool skillGained = m_character.AddSkill(skillToLearn, false, true);

                                if (skillGained == true)
                                {
                                    m_character.SendBuySkillResponse((int)entitySkill.SkillID);
                                }
                                else
                                {
                                    string locText = Localiser.GetString(textDB, m_character.m_player, (int)QuestManagerTextDB.TextID.ALREADY_KNOW_SKILL);
                                    string skillName = SkillTemplateManager.GetLocaliseSkillName(m_character.m_player, entitySkill.Template.SkillID);
                                    locText = string.Format(locText, skillName);
                                    Program.processor.sendSystemMessage(locText, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
                                }
                            }
                            else if (entitySkill.Template.LearnRecipeID != 0)
                            {
                                bool recipeLearned = m_character.AddRecipe(entitySkill.Template.LearnRecipeID);

                                if (recipeLearned)
                                {
                                    m_character.SendLearnRecipeResponse(entitySkill.Template.LearnRecipeID);
                                }
                                else
                                {
                                    string recipeKnown = "You already know " + entitySkill.Template.SkillName;
                                    Program.processor.sendSystemMessage(recipeKnown, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
                                }
                            }
                            else if (skillCaster != null && m_character.TheCombatManager != null)
                            {
                                double oldTimeActionWillComplete = skillCaster.TimeActionWillComplete;
                                bool skillSucceeded = m_character.TheCombatManager.CastSkill(skillCaster, m_character, entitySkill, null);

                                skillCaster.TimeActionWillComplete = oldTimeActionWillComplete;
                                if (skillSucceeded == false)
                                {
                                    Program.Display("error in skill reward fo quest " + quest.m_quest_id + " skill " + theReward.m_rewardID + " failed to cast by:" + skillCaster.GetIDString() + " for " + m_character.GetIDString());
                                }

                            }
                            else
                            {
                                Program.Display("error in skill reward for quest " + quest.m_quest_id + " skill " + theReward.m_rewardID + " failed to cast as no caster was found" + " for " + m_character.GetIDString());
                            }
                        }
                        else
                        {
                            Program.Display("error in skill reward for quest " + quest.m_quest_id + " skill " + theReward.m_rewardID + " does not exist" + " for " + m_character.GetIDString());
                        }

                        break;
                    }
            }
        }

        internal void checkIfAlreadyHaveRequirements(int quest_id)
        {
            Quest quest = GetCurrentQuest(quest_id);
            for (int i = 0; i < quest.m_QuestStages.Count; i++)
            {
                QuestStage stage = quest.m_QuestStages[i];
                if (stage.IsAvailable() && stage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.LootItem)
                {
                    int itemcount = m_character.m_inventory.checkHasItems(stage.m_QuestStageTemplate.m_completionDetails[0].m_id);
                    if (itemcount > 0)
                    {
                        checkIfItemAffectsStage(stage.m_QuestStageTemplate.m_completionDetails[0].m_id);
                    }
                }
            }
        }

        internal void checkPosition()
        {
            for (int i = 0; i < m_currentQuests.Count; i++)
            {
                Quest quest = m_currentQuests[i];
                for (int j = 0; j < quest.m_QuestStages.Count; j++)
                {
                    QuestStage stage = quest.m_QuestStages[j];
                    if (stage.IsAvailable()
                        && stage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.Position
                        && stage.m_QuestStageTemplate.m_zone_id == m_character.m_zone.m_zone_id
                        )
                    {
                        Vector3 diff = stage.m_QuestStageTemplate.m_position - m_character.m_CharacterPosition.m_position;
                        if (diff.LengthSquared() < stage.m_QuestStageTemplate.m_radius * stage.m_QuestStageTemplate.m_radius)
                        {
                            completeStage(quest.m_quest_id, j);

                        }
                    }

                }
            }
        }

        // TrackQuest
        // Update passed quest id's m_tracked and database entry bools
        internal bool TrackQuest(int quest_id, bool track)
        {
            Quest quest = GetCurrentQuest(quest_id);
            bool questTracked = false;

            if (quest != null)
            {
                if (track)
                {
                    quest.m_tracked = true;
                    m_db.runCommandSync("update quest set quest_tracked=1 where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id);
                    questTracked = true;
                }
                else
                {
                    quest.m_tracked = false;
                    m_db.runCommandSync("update quest set quest_tracked=0 where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id);
                }
            }

            return questTracked;
        }

        // TrackAllQuests
        // Update the tracked status of all current quests here in QuestHolder and database
        internal void TrackAllQuests(bool track_all)
        {
            for (int i = 0; i < m_currentQuests.Count; i++)
            {
                if (m_currentQuests[i] != null)
                {
                    if (track_all)
                    {
                        m_currentQuests[i].m_tracked = true;
                        m_db.runCommandSync("update quest set quest_tracked=1 where character_id=" + m_character.m_character_id + " and quest_id=" + m_currentQuests[i].m_quest_id);
                    }
                    else
                    {
                        m_currentQuests[i].m_tracked = false;
                        m_db.runCommandSync("update quest set quest_tracked=0 where character_id=" + m_character.m_character_id + " and quest_id=" + m_currentQuests[i].m_quest_id);
                    }
                }
            }
        }

        #region support online
        /// <summary>
        /// Used by the support Tool to restart a corrupted quest
        /// deletes the quest and then restarts the quest
        /// </summary>
        /// <param name="quest_id">The ID of the quest to be restarted</param>
        /// <returns>The quest that has been restarted, if null the function has failed</returns>
        internal Quest RestartQuest(int quest_id)
        {

            DeleteQuest(quest_id, true);

            StartQuest(quest_id);

            //find the quest
            Quest quest = GetCurrentQuest(quest_id);
            //if the quest doesn't exist then the start quest has failed
            return quest;
        }
        /// <summary>
        /// Used by the support Tool to restart a quest stage
        /// Only for the current Quest Stage
        /// rolls back to stage
        /// </summary>
        /// <param name="quest_id">The ID of the quest</param>
        /// <param name="stageID">The Stage to be restarted</param>
        /// <returns>The quest that has been restarted, if null the function has failed</returns>
        internal Quest RestartQuestStage(int quest_id, int stageID)
        {
            //find the quest
            Quest quest = GetCurrentQuest(quest_id);
            //if the quest doesn't exist then fail
            if (quest == null)
            {
                return quest;
            }

            //get the stage
            QuestStage stage = null;
            if (quest.m_QuestStages.Count > stageID)
            {
                stage = quest.m_QuestStages[stageID];
            }

            //if the stage does not exist then fail
            if (stage == null)
            {
                return quest;
            }
            //unlo9ck all stages so they can be undone if required
            unlockAllStages(quest);

            stage.m_completion_sum = 0;
            //this will do the recursion required online
            unCompleteStage(quest, stageID);

            //relock any stages that should be
            lockStages(quest);

            return quest;
        }
        internal Quest CompleteQuestStageSupportBase(int quest_id, int stageID)
        {
            //holds a list of all of the quest stages that need to be saved out because they have changed
            List<QuestStage> alteredStages = new List<QuestStage>();
            //find the quest
            Quest quest = GetCurrentQuest(quest_id);
            //if the quest doesn't exist then fail
            if (quest == null)
            {
                return null;
            }

            //get the stage
            QuestStage stage = null;
            if (quest.m_QuestStages.Count > stageID)
            {
                stage = quest.m_QuestStages[stageID];
            }

            //if the stage does not exist then fail
            if (stage == null)
            {
                return null;
            }

            if (stage.m_completed == true)
            {
                return null;
            }
            CompleteQuestStageRecursive(quest, stage, stageID, ref alteredStages);
            int nextStageIndex = stage.m_QuestStageTemplate.m_nextStage;
            //update the info on the next stage

            if (nextStageIndex < quest.m_QuestStages.Count)
            {
                QuestStage nextStage = quest.m_QuestStages[nextStageIndex];
                //if it's the end of the quest complete the quest
                if (nextStage.m_QuestStageTemplate.m_completion_type == QuestStageTemplate.CompletionType.EndQuest)
                {
                    //this will send a message to tell the player
                    completeQuest(quest_id);
                }
                else
                {
                    //if it's not theend of the quest send progress
                    SendQuestStageProgress(quest, stage, false, nextStageIndex);
                }
            }
            //otherwise tell it that the stage was completed


            lockStages(quest);
            //save out info on all altered stages
            /*for (int i=0; i < alteredStages.Count; i++)
            {
                QuestStage currentStage = alteredStages[i];

                if (currentStage.m_completed == true)
                {
                    m_db.runCommandSync("update quest_stage set completed=1,completed_date='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', stage_open_sum =" +currentStage.m_stage_open_sum+ " where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id + " and stage_id=" + currentStage.m_QuestStageTemplate.m_stage_id);
                       
                }
                else
                {
                    //it's just the open sum that has changed
                    m_db.runCommandSync("update quest_stage set stage_open_sum =" + currentStage.m_stage_open_sum + " where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id + " and stage_id=" + currentStage.m_QuestStageTemplate.m_stage_id);
                   
                }
            }*/
            SaveAlteredStages(alteredStages, quest_id, m_character.m_character_id);
            return quest;
        }
        static void SaveAlteredStages(List<QuestStage> alteredStages, int questID, uint characterID)
        {
            Database db = Program.processor.m_worldDB;
            //save out info on all altered stages
            for (int i = 0; i < alteredStages.Count; i++)
            {
                QuestStage currentStage = alteredStages[i];

                if (currentStage.m_completed == true)
                {
                    db.runCommandSync("update quest_stage set completed=1,completed_date='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', stage_open_sum =" + currentStage.m_stage_open_sum + " where character_id=" + characterID + " and quest_id=" + questID + " and stage_id=" + currentStage.m_QuestStageTemplate.m_stage_id);

                }
                else
                {
                    //it's just the open sum that has changed
                    db.runCommandSync("update quest_stage set stage_open_sum =" + currentStage.m_stage_open_sum + " where character_id=" + characterID + " and quest_id=" + questID + " and stage_id=" + currentStage.m_QuestStageTemplate.m_stage_id);

                }
            }
        }
        static Quest CompleteQuestStageRecursive(Quest quest, QuestStage stage, int stageID, ref List<QuestStage> alteredStages)
        {

            QuestStageTemplate completingTemplate = stage.m_QuestStageTemplate;



            if (completingTemplate == null || stage.m_completed == true)
            {
                return quest;
            }
            //get the template
            QuestTemplate questTemplate = quest.m_QuestTemplate;
            //get the stages that will need to be completed
            List<QuestStage> requiredStages = new List<QuestStage>();
            for (int i = 0; i < quest.m_QuestTemplate.m_QuestStageTemplates.Count && i < quest.m_QuestStages.Count; i++)
            {
                QuestStageTemplate currentTemplate = quest.m_QuestTemplate.m_QuestStageTemplates[i];
                QuestStage currentStage = quest.m_QuestStages[i];
                if (currentTemplate.m_nextStage == stageID)
                {
                    requiredStages.Add(currentStage);
                }

            }
            //go through each required stage and complete it if not yet completed
            for (int i = 0; i < requiredStages.Count; i++)
            {
                QuestStage currentStage = requiredStages[i];
                QuestStageTemplate currentTemplate = currentStage.m_QuestStageTemplate;
                if (currentStage.m_completed == false)
                {
                    CompleteQuestStageRecursive(quest, currentStage, currentTemplate.m_stage_id, ref alteredStages);
                }
            }

            //set all the variables that need to be set to complete this stage
            switch (completingTemplate.m_completion_type)
            {
                case QuestStageTemplate.CompletionType.CastSkill:
                case QuestStageTemplate.CompletionType.LootItem:
                case QuestStageTemplate.CompletionType.MobKill:
                    {
                        stage.m_completion_sum = completingTemplate.m_completionDetails[0].m_total;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            stage.m_completed = true;


            if (alteredStages.Contains(stage) == false)
            {
                alteredStages.Add(stage);
            }
            int nextStageIndex = completingTemplate.m_nextStage;
            //update the info on the next stage
            if (nextStageIndex < quest.m_QuestStages.Count)
            {
                QuestStage nextStage = quest.m_QuestStages[nextStageIndex];
                nextStage.m_stage_open_sum++;
                if (alteredStages.Contains(nextStage) == false)
                {
                    alteredStages.Add(nextStage);
                }
            }

            return quest;

        }
        /// <summary>
        /// Used by the support Tool to delete the quest
        /// </summary>
        /// <param name="quest_id"></param>
        /// <returns></returns>
        internal bool DeleteQuest(int quest_id, bool updateDatabase)
        {
            bool questDeleted = false;

            //has the quest been found in completed or current quests
            bool questFound = false;
            //find the quest
            //check the current quests
            Quest quest = GetCurrentQuest(quest_id);
            //if the quest is a current quest remove it from current quests
            if (quest != null)
            {
                m_currentQuests.Remove(quest);
                questFound = true;
            }
            //is the quest in the completed list
            bool isComplete = m_completed_quests.Contains(quest_id);
            //if the quest is a completed quest remove it from completed quests
            if (isComplete == true)
            {
                m_completed_quests.Remove(quest_id);
                questFound = true;
            }


            if (questFound == true)
            {
                if (updateDatabase == true)
                {
                    m_db.runCommandSync("delete from quest where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id);
                    m_db.runCommandSync("delete from quest_stage where character_id=" + m_character.m_character_id + " and quest_id=" + quest_id);
                }

                questDeleted = true;
            }

            return questDeleted;
        }

        #endregion //support online

        #region support offline

        internal static bool RestartQuestOffline(int quest_id, int characterID)
        {

            DeleteQuestOffline(quest_id, characterID);
            StartQuestOffline(quest_id, characterID);

            return true;
        }
        internal static bool DeleteQuestOffline(int quest_id, int characterID)
        {
            bool questDeleted = false;

            Program.processor.m_worldDB.runCommandSync("delete from quest where character_id=" + characterID + " and quest_id=" + quest_id);
            Program.processor.m_worldDB.runCommandSync("delete from quest_stage where character_id=" + characterID + " and quest_id=" + quest_id);

            questDeleted = true;


            return questDeleted;
        }
        internal static bool StartQuestOffline(int quest_id, int characterID)
        {
            bool questStarted = false;
            QuestTemplateManager questTemplateManager = Program.processor.m_QuestTemplateManager;
            QuestTemplate questTemplate = questTemplateManager.GetQuestTemplate(quest_id);
            Quest newQuest = new Quest(null, quest_id, questTemplate, false);
            Database db = Program.processor.m_worldDB;
            db.runCommandSync("insert into quest (character_id,quest_id,completed,started_date) values (" + characterID + "," + quest_id + ",0,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')");


            string dbUpdateString = "";
            for (int i = 0; i < questTemplate.m_QuestStageTemplates.Count; i++)
            {
                newQuest.addNewStage(db, (uint)characterID, quest_id, i);
                dbUpdateString += ",(" + (uint)characterID + "," + quest_id + "," + i + ",0,0,0)";

            }
            if (dbUpdateString.Length > 0)
            {
                db.runCommandSync("insert into quest_stage (character_id,quest_id,stage_id,stage_open_sum,completed,completion_sum) values " + dbUpdateString.Substring(1));
            }


            return questStarted;
        }
        static Quest LoadQuestFromOffline(int quest_id, int characterID)
        {
            Quest newQuest = null;
            Database db = Program.processor.m_worldDB;
            SqlQuery query = new SqlQuery(db, "select * from quest where character_id=" + characterID + " and quest_id = " + quest_id);
            if (query.HasRows)
            {
                while (query.Read())
                {


                    Boolean completed = query.GetBoolean("completed");
                    if (completed)
                    {

                    }
                    else
                    {
                        QuestTemplateManager questTemplateManager = Program.processor.m_QuestTemplateManager;
                        QuestTemplate questTemplate = questTemplateManager.GetQuestTemplate(quest_id);
                        newQuest = new Quest(null, quest_id, questTemplate, false);

                        SqlQuery stageQuery = new SqlQuery(db, "select * from quest_stage where character_id=" + characterID + " and quest_id=" + quest_id + " order by stage_id");
                        if (stageQuery.HasRows)
                        {
                            while (stageQuery.Read())
                            {
                                int stage_id = stageQuery.GetInt32("stage_id");
                                int stage_open_sum = stageQuery.GetInt32("stage_open_sum");
                                bool stage_completed = stageQuery.GetBoolean("completed");
                                int completion_sum = stageQuery.GetInt32("completion_sum");
                                newQuest.addStage(stage_id, stage_open_sum, completion_sum, stage_completed);
                            }
                        }
                        stageQuery.Close();

                    }
                }
            }
            query.Close();
            return newQuest;
        }
        internal static bool RestartQuestStageOffline(int quest_id, int stageID, int characterID)
        {

            //the quest needs to be read in

            Quest quest = 
                LoadQuestFromOffline(quest_id, characterID);
            if (quest == null)
            {
                return false;
            }
            //get the stage
            QuestStage stage = null;
            if (quest.m_QuestStages.Count > stageID)
            {
                stage = quest.m_QuestStages[stageID];
            }

            //if the stage does not exist then fail
            if (stage == null)
            {
                return false;
            }
            //unlock all stages so they can be undone if required


            stage.m_completion_sum = 0;
            //this will do the recursion required online
            unCompleteStageOffline(quest, stageID, characterID);

            return true;
        }
        #endregion
    }
}
