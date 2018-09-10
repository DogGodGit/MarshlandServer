using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace MainServer
{
    class Achievement
    {
        public double m_value;
        public bool m_confirmed;
        public DateTime m_updateSent=DateTime.MinValue;
        public AchievementTemplate m_template;
        public bool m_updated;
        public Achievement(AchievementTemplate template, double value, bool confirmed)
        {
            m_template = template;
            m_value = value;
            m_confirmed = confirmed;
            m_updated = false;
        }

        

    }
    class AchievementTemplate
    {
        public AchievementsManager.ACHIEVEMENT_TYPE m_achievement_type;
        public string m_code;
        public string m_description;
        public double m_target;
        public bool m_rare;
        public AchievementTemplate(AchievementsManager.ACHIEVEMENT_TYPE achievement_type, string code, string description, double target,bool rare)
        {
            m_achievement_type = achievement_type;
            m_code = code;
            m_description = description;
            m_target = target;
            m_rare = rare;
        }
    }
    class AchievementsManager
    {

        const int NUM_ACHIEVEMENTS=1;
        public enum ACHIEVEMENT_MANAGER_TYPE
        {
            CHARACTER_ACHIEVEMENTS=0,
            ACCOUNT_ACHIEVEMENTS=1
        }
        public enum ACHIEVEMENT_TYPE
        {
            CLANSMAN=0,
            CHIEFTAIN=1,
            SOCIALISER=2,
            TRADER=3,
            EXPLORER=4,
            QUESTER=5,
            ADVENTURER=6,
            HEALER=7,
            SLAYER=8,
            WOLF_BANE=9,
            GOBLIN_BANE=10,
            SKELETON_BANE=11,
            PUPPY_SLAYER=12,
            GOLDEN=13,
            LORD_OF_DUSTWITHER=14,
            LORD_OF_CROOKBACK=15,
            HIGH_ROLLER=16,
            COLLECTOR=17,
            BIRDMAN=18,
            BACK_FROM_THE_DEAD=19,
            MASTER_OF_LIRS=20,
            MASTER_OF_CROOKBACK=21,
            MASTER_OF_DUSTWITHER=22,
            MASTER_OF_SHALEMONT=23,
            MASTER_OF_STONEVALE=24,
            STONEVALE_EXPLORER=25,
            SHALEMONT_EXPLORER=26,
            LORD_OF_SHALEMONT=27,
            LORD_OF_STONEVALE=28,
            MUSHROOM_PICKER = 29,

            MASTER_OF_OTHERWORLD=30,//*
            MASTER_OF_FINGALS=31,//*
            MASTER_OF_CARROWMORE=32,//*
            MASTER_OF_SEWERS=33,//*

            DRAGON_SLAYER=34,//*

            PVP_SOLDIER=35,//*
            PVP_SERGEANT=36,//*
            PVP_LIEUTENANT=37,//*
            PVP_MARSHAL=38,//*
            PVP_WARDEN=39,//*
            PVP_LORD_COMMANDER=40,//*

            PVP_COMPETANT=41,//*
            PVP_SKILLFUL=42,//*
            PVP_POWERFUL=43,//*
            PVP_FORMIDABLE=44,//*
            PVP_MIGHTY=45,//*
            PVP_DEADLY=46,//*
            PVP_DEVASTATING=47,//*
            PVP_INVINCIBLE=48,//*

        }
        List<Achievement> m_achievements = new List<Achievement>();
        Database m_db;
        string m_tablename;
        string m_keyfield;
        int m_keyvalue;
        ACHIEVEMENT_MANAGER_TYPE m_managerType;
        public AchievementsManager(Database db,ACHIEVEMENT_MANAGER_TYPE managerType,string tablename,string keyfield,int keyvalue)
        {
            m_db=db;
            m_tablename=tablename;
            m_keyfield = keyfield;
            m_keyvalue = keyvalue;
            m_managerType = managerType;
            SqlQuery query = new SqlQuery(db,"select * from " + tablename + " where " + keyfield+"="+keyvalue);
            while (query.Read())
            {
                int achievement_id=query.GetInt32("achievement_id");
                double achievement_value=query.GetDouble("achievement_value");
                bool confirmed=query.GetBoolean("confirmed");
                m_achievements.Add(new Achievement(getTemplate((ACHIEVEMENT_TYPE )achievement_id),achievement_value,confirmed));
            }
        }
        public double increaseStat(ACHIEVEMENT_TYPE achievementType, double increment)
        {
            Achievement achievement = getAchievement(achievementType);
            if (achievement.m_value < achievement.m_template.m_target)
            {
                achievement.m_value += increment;

                if (achievement.m_value > achievement.m_template.m_target)
                {
                    achievement.m_value = achievement.m_template.m_target;
                }
                writeToDatabase(achievementType);
            }
            return achievement.m_value;
        }


        public double getStat(ACHIEVEMENT_TYPE achievementType)
        {
            return getAchievement(achievementType).m_value;
        }

        public bool setStat(ACHIEVEMENT_TYPE achievementType, double value)
        {
            Achievement achievement = getAchievement(achievementType);
            if (achievement.m_value< value)
            {
                achievement.m_value = value;
                writeToDatabase(achievementType);
                return true;
            }
            return false;
        }

        void writeToDatabase(ACHIEVEMENT_TYPE achievementType)
        {
            string completed = "0";
            string confirmed = "1";
            Achievement achievement = getAchievement(achievementType);
           
            achievement.m_confirmed = true;
            if (achievement.m_value == achievement.m_template.m_target)
            {
                completed = "1";
            }
            if (m_managerType == ACHIEVEMENT_MANAGER_TYPE.ACCOUNT_ACHIEVEMENTS)
            {
                achievement.m_updateSent = DateTime.Now;
                achievement.m_confirmed = false;
                confirmed = "0";
            }
            if (achievement.m_template.m_rare)
            {
                m_db.runCommandSync("replace into " + m_tablename + " (" + m_keyfield + ",achievement_id,achievement_value,completed,confirmed) values (" + m_keyvalue + "," + ((int)achievementType).ToString() + "," + achievement.m_value + "," + completed + "," + confirmed + ")");
            }
            else
            {
                achievement.m_updated = true;
            }
        }
        public void confirmed(ACHIEVEMENT_TYPE achievementType)
        {
            Achievement achievement = getAchievement(achievementType);
            achievement.m_confirmed = true;
            if (achievement.m_template.m_rare)
            {
                m_db.runCommandSync("update " + m_tablename + " set confirmed=1 where " + m_keyfield + " = " + m_keyvalue + " and achievement_id=" + (int)achievementType);
            }
            else
            {
                achievement.m_updated = true;
            }
        }

        public void sendAchievementUpdate(Character character, ACHIEVEMENT_TYPE achievementType)
        {
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            Achievement achievement = getAchievement(achievementType);
            msg.WriteVariableUInt32((uint)NetworkCommandType.AchievementUpdate);
            msg.WriteVariableInt32((int)character.m_character_id);
            msg.WriteVariableInt32((int)achievementType);
            msg.Write(achievement.m_template.m_code);
            msg.Write(achievement.m_template.m_description);
            msg.Write((float)((100 * achievement.m_value) / achievement.m_template.m_target));
            Program.processor.SendMessage(msg, character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AchievementUpdate);
        }
        public Achievement getAchievement(ACHIEVEMENT_TYPE achievementType)
        {
            Achievement achievement=null;
            for (int i = 0; i < m_achievements.Count; i++)
            {
                if (m_achievements[i].m_template.m_achievement_type == achievementType)
                {
                    achievement = m_achievements[i];
                    break;
                }
            }
            if (achievement == null)
            {
                achievement = new Achievement(getTemplate(achievementType), 0, true);
                m_achievements.Add(achievement);
            }
            return achievement;
        }
        public AchievementTemplate getTemplate(ACHIEVEMENT_TYPE achievementType)
        {
            AchievementTemplate template = null;
            for (int i = 0; i < Program.processor.m_achievement_templates.Count; i++)
            {
                if (Program.processor.m_achievement_templates[i].m_achievement_type == achievementType)
                {
                    template = Program.processor.m_achievement_templates[i];
                    break;
                }
            }
            return template;
        }
        public void update(Character character)
        {
            for (int i = 0; i < m_achievements.Count; i++)
            {
                Achievement achievement = m_achievements[i];
                if(achievement.m_confirmed==false && (DateTime.Now-achievement.m_updateSent).TotalMinutes>60)
                {
                    writeToDatabase(achievement.m_template.m_achievement_type);
                    sendAchievementUpdate(character, achievement.m_template.m_achievement_type);
                }
            }

        }

        public void saveAchievements()
        {
            string updateString="";
            for (int i = 0; i < m_achievements.Count(); i++)
            {
                Achievement achievement = m_achievements[i];
                if (achievement.m_updated == true)
                {
                    string completed = "0";
                    string confirmed = "0";
                    if (achievement.m_value == achievement.m_template.m_target)
                    {
                        completed = "1";
                    }
                    if (achievement.m_confirmed==true)
                    {
                        confirmed = "1";
                    }
                    updateString=updateString+",(" + m_keyvalue + "," + ((int)achievement.m_template.m_achievement_type).ToString() + "," + achievement.m_value + "," + completed + "," + confirmed + ")";
                    achievement.m_updated = false;
                }
            }
            if (updateString.Length > 0)
            {
                m_db.runCommandSync("replace into " + m_tablename + " (" + m_keyfield + ",achievement_id,achievement_value,completed,confirmed) values " + updateString.Substring(1));
              //  Program.Display(m_tablename + "achievements updated " + updateString);

            }
        }
    }
}
