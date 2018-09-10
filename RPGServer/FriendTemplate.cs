using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace MainServer
{
    class FriendTemplate
    {
        int m_characterID=-1;
        uint m_accountID = 0;
        string m_characterName="";
        int m_zone=-1;
        int m_level=-1;
        int m_class = 0;
        int m_race = 0;
        bool m_online=false;
        Character m_character = null;

        internal int CharacterID
        {
            set { m_characterID = value; }
            get { return m_characterID; }
        }
        internal string CharacterName
        {
            set { m_characterName = value; }
            get { return m_characterName; }
        }
        internal int Zone
        {
            set { m_zone = value; }
            get { return m_zone; }
        }
        internal int Level
        {
            set { m_level = value; }
            get { return m_level; }
        }
        internal int Class
        {
            get { return m_class; }
            set { m_class = value; }
        }
        internal int Race
        {
            get { return m_race; }
            set { m_race = value; }
        }
        internal bool Online  
        {
            set { m_online = value; }
            get { return m_online; }
        }
        internal uint AccountID
        {
            set { m_accountID = value; }
            get { return m_accountID; }
        }
        internal FriendTemplate(Character baseCharacter)
        {
            m_characterID = (int)baseCharacter.m_character_id;
            m_characterName = baseCharacter.m_name;
            m_zone = baseCharacter.m_zone.m_zone_id;
            m_level = baseCharacter.Level;
            m_class = (int)baseCharacter.m_class.m_classType;
            m_online = true;
            m_race = (int)baseCharacter.m_race.m_raceType;
            m_character = baseCharacter;
            

        }
        internal void UpdateWithDetails(Character baseCharacter)
        {
            m_characterID = (int)baseCharacter.m_character_id;
            m_characterName = baseCharacter.m_name;
            m_zone = baseCharacter.m_zone.m_zone_id;
            m_level = baseCharacter.Level;
            m_class = (int)baseCharacter.m_class.m_classType;
            m_race = (int)baseCharacter.m_race.m_raceType;
            m_character = baseCharacter;
            
        }
        internal Character Character
        {
            get { return m_character; }
            set { m_character = value; }
        }
        internal FriendTemplate()
        {
            m_race = 0;
        }


        static internal FriendTemplate ContainsTemplateForID(List<FriendTemplate> theList, int theTemplateID)
        {
            FriendTemplate theTemplate = null;
            for (int i = 0; i < theList.Count; i++)
            {
                FriendTemplate currentTemplate = theList[i];
                if (currentTemplate != null)
                {
                    if (currentTemplate.CharacterID == theTemplateID)
                    {
                        theTemplate = currentTemplate;
                        return theTemplate;
                    }
                }
            }
            return theTemplate;
        }
                
        internal  void WriteSelfToMessage(NetOutgoingMessage msg)
        {
            msg.Write(CharacterName);
            msg.WriteVariableInt32(CharacterID);
            //online
            if (Online == true)
            {
                msg.Write((byte)1);
            }
            else
            {
                msg.Write((byte)0);
            }
            //location
            msg.WriteVariableInt32(Zone);
            //level
            msg.WriteVariableInt32(Level);
            //class
            msg.WriteVariableInt32(Class);
            //race
            msg.WriteVariableInt32(Race);
        }
        internal string GetIDString()
        {
            string idString = "(" + m_characterID + "," + m_characterName + ")";

            return idString;
        }
    }
}
