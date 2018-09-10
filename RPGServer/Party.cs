using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using XnaGeometry;
using MainServer.Localise;

namespace MainServer
{
    class PreviousPartyMember
    {
        public PreviousPartyMember(uint character_id)
        {
            m_character_id = character_id;
            m_leaveTime = DateTime.Now;
        }
        public uint m_character_id;
        public DateTime m_leaveTime;
    }
    class Party : ITargetOwner
    {
		// #localisation
		public class PartyTextDB : TextEnumDB
		{
			public PartyTextDB() : base(nameof(Party), typeof(TextID)) { }

			public enum TextID
			{
				OTHER_PARTY_TOO_LARGE,          //"{name0}'s party is too large"
				PLAYER_PARTY_TOO_LARGE,         //"Your party is too large"
				COMBINED_PARTY_TOO_LARGE,       //"The combined party would be too large"
			}
		}
		public static PartyTextDB textDB = new PartyTextDB();

		public static float EXPERIANCE_BOOST_PER_PLAYER =0.3f;
        public static float GOLD_BOOST_PER_PLAYER = 0.3f;
        static int MAX_CHARACTERS_IN_PARTY = 8;
        List<Character> m_characterList=new List<Character>();
        List<PreviousPartyMember> m_previousMembers = new List<PreviousPartyMember>(); 
        int m_highestLevel = 0;
        List<CombatEntity> m_lockedList = new List<CombatEntity>();

        internal int Size
        {
            get { return m_characterList.Count; }
        }
        internal List<Character> CharacterList
        {
            get { return m_characterList; }
        }
        internal int HighestLevel
        {
            get { return m_highestLevel; }
        }
        internal bool AddPlayer(Character newCharacter)
        {
            bool characterAdded = false;
            for (int i = 0; i < m_characterList.Count; i++)
            {
                if (newCharacter.ServerID == m_characterList[i].ServerID)
                {
                    return true;
                }
            }
            if (m_characterList.Count < MAX_CHARACTERS_IN_PARTY)
            {
                OwnerMerge(newCharacter);
        
                m_characterList.Add(newCharacter);
                newCharacter.CharacterParty = this;
                characterAdded = true;
                newCharacter.PVPListNeedsChecked();
            }
            //send info to the other players
            RecalculateMaxLevel();
            return characterAdded;
        }
        void OwnerMerge(ITargetOwner mergingGroup)
        {

            List<NetConnection> connections = new List<NetConnection>();
            List<Character> characters = mergingGroup.GetCharacters;
            for (int charIndex = 0; charIndex < characters.Count; charIndex++)
            {
                Character currentCharacter = characters[charIndex];
                if (currentCharacter != null && currentCharacter.m_player != null && currentCharacter.m_player.connection != null)
                {
                    connections.Add(currentCharacter.m_player.connection);
                }
            }
            if (connections.Count > 0)
            {
                for (int i = 0; i < m_lockedList.Count; i++)
                {
                    CombatEntity currentTarget = m_lockedList[i];
                    currentTarget.SendLockMessage(connections, CombatEntity.TargetLockType.Owned);

                }
            }
            //make a fresh copy so it will not be affected by items being removed
            List<CombatEntity> newLocks = new List<CombatEntity>( mergingGroup.GetCurrentLocks);
            for (int lockIndex = 0; lockIndex < newLocks.Count; lockIndex++)
            {
                CombatEntity currentNewLock = newLocks[lockIndex];
                if (currentNewLock != null)
                {
                    TakeOwnership(currentNewLock);
                }
            }

        }
        internal void TransferOwnershipTo(ITargetOwner mergingGroup)
        {
            List<CombatEntity> newLocks = new List<CombatEntity>(GetCurrentLocks);
            for (int lockIndex = 0; lockIndex < newLocks.Count; lockIndex++)
            {
                CombatEntity currentNewLock = newLocks[lockIndex];
                if (currentNewLock != null)
                {
                    mergingGroup.TakeOwnership(currentNewLock);
                }
            }
        }
        internal bool RemovePlayer(Character characterToRemove,bool voluntary)
        {
            bool characterRemoved = false;

            if (m_characterList.Contains(characterToRemove) == true)
            {
                characterRemoved = true;
                List<NetConnection> connections = new List<NetConnection>();
                if (characterToRemove != null && characterToRemove.m_player != null && characterToRemove.m_player.connection != null)
                {
                    connections.Add(characterToRemove.m_player.connection);
                }
                if (connections.Count > 0)
                {
                    for (int i = 0; i < m_lockedList.Count; i++)
                    {
                        CombatEntity currentTarget = m_lockedList[i];
                        currentTarget.SendLockMessage(connections, CombatEntity.TargetLockType.Locked);
                    }
                }
                characterToRemove.PVPListNeedsChecked();
                m_characterList.Remove(characterToRemove);
                if (!voluntary)
                {
                    m_previousMembers.Add(new PreviousPartyMember(characterToRemove.m_character_id));
                }
                characterToRemove.CharacterParty = null;
            }
            SendNewPartyConfiguration();
            //destroy the party
            if (m_characterList.Count == 1 && voluntary)
            {
                TransferOwnershipTo(m_characterList[0]);
                m_characterList[0].CharacterParty = null;
                m_characterList.Clear();
            }
            else
            {
                RecalculateMaxLevel();
            }
            //send info To the other Players

            return characterRemoved;
        }
        internal void SendNewPartyConfiguration()
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.PartyMessage);
            outmsg.WriteVariableInt32((int) PartyMessageType.NewPartyConfiguration);

            WritePartyToMessage(outmsg);

            List<NetConnection> connections = new List<NetConnection>();
            for (int i = 0; i < m_characterList.Count; i++)
            {
                Character currentCharacter = m_characterList[i];
                if (currentCharacter != null && currentCharacter.m_player != null&&currentCharacter.m_player.connection!=null)
                {
                    connections.Add(currentCharacter.m_player.connection);
                }
            }
            Program.processor.SendMessage(outmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PartyMessage);

        }
        void WritePartyToMessage(NetOutgoingMessage msg)
        {
            int numberOfPlayers = m_characterList.Count;

            msg.WriteVariableInt32(numberOfPlayers);

            for (int i = 0; i < m_characterList.Count; i++)
            {
                Character currentCharacter = m_characterList[i];
                msg.Write(currentCharacter.m_name);
                msg.WriteVariableInt32((int)currentCharacter.m_character_id);
                //location
                msg.WriteVariableInt32(currentCharacter.m_zone.m_zone_id);
                //level
                msg.WriteVariableInt32(currentCharacter.Level);
                //class
                msg.WriteVariableInt32((int)currentCharacter.m_class.m_classType);

               /* msg.WriteVariableInt32(currentCharacter.MaxHealth);
                msg.WriteVariableInt32(currentCharacter.MaxEnergy);
                msg.WriteVariableInt32(currentCharacter.CurrentHealth);
                msg.WriteVariableInt32(currentCharacter.CurrentEnergy);*/

            }

        }

        void AbsorbOtherParty(Party otherParty)
        {
            List<Character> otherCharacterList = otherParty.CharacterList;
            OwnerMerge(otherParty);
            for(int i=0; i<otherCharacterList.Count; i++){
                Character currentCharacter = otherCharacterList[i];

                bool added = AddPlayer(currentCharacter);
                if (added == false)
                {
                    Program.Display("Party Error - " + currentCharacter.m_name + " was not added to the party");
                    return;
                }
                
            }
            for (int i = 0; i < otherParty.m_previousMembers.Count; i++)
            {
                m_previousMembers.Add(new PreviousPartyMember(otherParty.m_previousMembers[i].m_character_id));
            }
            otherCharacterList.Clear();
            otherParty.CharacterList.Clear();

        }
        public static bool CanFormParty(Character player1, Character player2, bool sendFeedback)
        {
            if (player1 == null || player1.Destroyed || player2 == null || player2.Destroyed)
            {
                return false;
            }
            if ((player1.CharacterParty == null) && (player2.CharacterParty == null))
            {
                return true;
            }
            else if (player1.CharacterParty == player2.CharacterParty)
            {
                return false;
            }
            else if (player1.CharacterParty == null)
            {
                if (player2.CharacterParty.Size < MAX_CHARACTERS_IN_PARTY)
                {

                    return true;
                }

				string locText = Localiser.GetString(textDB, player1.m_player, (int)PartyTextDB.TextID.OTHER_PARTY_TOO_LARGE);
				locText = string.Format(locText, player2.m_name);
				Program.processor.sendSystemMessage(locText, player1.m_player, false, SYSTEM_MESSAGE_TYPE.PARTY);

			}
            else if (player2.CharacterParty == null)
            {
                if (player1.CharacterParty.Size < MAX_CHARACTERS_IN_PARTY)
                {

                    return true;
                }
				string locText = Localiser.GetString(textDB, player1.m_player, (int)PartyTextDB.TextID.PLAYER_PARTY_TOO_LARGE);
				Program.processor.sendSystemMessage(locText, player1.m_player, false, SYSTEM_MESSAGE_TYPE.PARTY);
			}
            else if ((player1.CharacterParty.Size + player2.CharacterParty.Size) <= MAX_CHARACTERS_IN_PARTY)
            {
                if ((player1.CharacterParty.CharacterList.Contains(player2) == true) || (player2.CharacterParty.CharacterList.Contains(player1) == true))
                {
                    //Program.processor.sendSystemMessage("The combined party would be too large", player1.m_player.connection, SYSTEM_MESSAGE_TYPE.PARTY);
                    return false;
                }
                return true;
            }
            else
            {
				string locText = Localiser.GetString(textDB, player1.m_player, (int)PartyTextDB.TextID.COMBINED_PARTY_TOO_LARGE);
				Program.processor.sendSystemMessage(locText, player1.m_player, false, SYSTEM_MESSAGE_TYPE.PARTY);
			}
            return false;            
        }
        public static Party CombineParties(Character player1, Character player2)
        {
            Party resultingParty = null;
            if ((player1.CharacterParty == null) && (player2.CharacterParty == null))
            {
                resultingParty = new Party();
                resultingParty.AddPlayer(player1);
                resultingParty.AddPlayer(player2);
            }
            else if (player1.CharacterParty == null)
            {
                resultingParty = player2.CharacterParty;
                resultingParty.AddPlayer(player1);
            }
            else if (player2.CharacterParty == null)
            {
                resultingParty = player1.CharacterParty;
                resultingParty.AddPlayer(player2);
            }
            else if ((player1.CharacterParty.Size + player2.CharacterParty.Size) <= MAX_CHARACTERS_IN_PARTY)
            {
                resultingParty = player1.CharacterParty;
                resultingParty.AbsorbOtherParty(player2.CharacterParty);
            }

            return resultingParty; 
        }
        internal void RecalculateMaxLevel()
        {
            if (m_characterList.Count>0)
            {
                m_highestLevel = m_characterList[0].Level;
            }
            for (int i = 0; i < m_characterList.Count; i++)
            {
                Character currentCharacter = m_characterList[i];
                if (m_highestLevel < currentCharacter.Level)
                {
                    m_highestLevel = currentCharacter.Level;
                }
            }
        }
        internal void SendPartyChatMessage(string chatMessage, Player sendingPlayer)
        {
            List<NetConnection> connections = new List<NetConnection>();
            int sendersID = (int)sendingPlayer.m_activeCharacter.m_character_id;
            for (int i = 0; i < m_characterList.Count; i++)
            {
                Character currentCharacter = m_characterList[i];
                if (currentCharacter != null && currentCharacter.m_player != null && currentCharacter.m_player.connection != null)
                {
                    if (currentCharacter.HasBlockedCharacter(sendersID) == false)
                    {
                        connections.Add(currentCharacter.m_player.connection);
                    }
                }
            }
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();

            outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
            outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_TEAM);
            outmsg.Write(sendingPlayer.m_activeCharacter.m_name);
            outmsg.Write(chatMessage);
            outmsg.WriteVariableInt32((int)sendingPlayer.m_activeCharacter.m_character_id);
            Program.Display("got party chat message from " + sendingPlayer.m_activeCharacter.m_name + " : " + chatMessage);
            Program.processor.SendMessage(outmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
        }

        internal void SendPartySystemMessage(string message, Character characterToExclude,bool important,SYSTEM_MESSAGE_TYPE type, bool isBattle)
        {
             List<NetConnection> connections = new List<NetConnection>();
             int excludeID = -1;
             if (characterToExclude != null)
             {
                 excludeID = (int)characterToExclude.m_character_id;
             }

            for (int i = 0; i < m_characterList.Count; i++)
            {
                if ((int)m_characterList[i].m_character_id != excludeID && m_characterList[i].m_player != null && m_characterList[i].m_player.connection!=null)
                {
                    connections.Add(m_characterList[i].m_player.connection);
                }
            }
            if (isBattle)
            {
                Program.processor.SendBattleMessage(message, connections, important, type);
            }
            else
            {
                 Program.processor.sendSystemMessage(message, connections, important,type);
            }
           
        }

        internal bool update()
        {
            for (int i = m_previousMembers.Count-1; i >-1; i--)
            {
                if ((DateTime.Now - m_previousMembers[i].m_leaveTime).TotalMinutes > 5)//0.01) //15)
                {
                    m_previousMembers.RemoveAt(i);
                }
            }
            if (m_previousMembers.Count == 0 && m_characterList.Count == 1)
            {
                m_characterList[0].CharacterParty = null;
                m_characterList.Clear();
            }
            if (m_characterList.Count > 0)
                return true;
            return false;
        }
        internal bool checkPreviousMembers(Character member)
        {
           
            if(Size<MAX_CHARACTERS_IN_PARTY)
            {
                try
                {
                    for (int i = 0; i < m_previousMembers.Count; i++)
                    {
                        if (m_previousMembers[i].m_character_id == member.m_character_id)
                        {
                            bool found=false;
                            for (int j = 0; j < m_characterList.Count; j++)
                            {
                                if (CharacterList[j].m_character_id == member.m_character_id)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                AddPlayer(member);
                                m_previousMembers.RemoveAt(i);
                                return true;
                            }
                        }
                    }
            
                }
                catch(Exception)
                {
                }
            
            }
            return false;
        }


        #region ITargetOwner Members

        public bool ResignOwnership(CombatEntity theEntity)
        {
            bool ownershipResigned = false;
            if (m_lockedList.Contains(theEntity) == true)
            {
                m_lockedList.Remove(theEntity);
                if (theEntity.LockOwner == this)
                {
                    theEntity.LockOwner = null;
                }
            }
            return ownershipResigned;
        }

        public bool TakeOwnership(CombatEntity theEntity)
        {
            bool ownershipTaken = false;
            if (m_lockedList.Contains(theEntity) == false)
            {
                if (theEntity.LockOwner != null)
                {
                    theEntity.LockOwner.ResignOwnership(theEntity);
                }
                m_lockedList.Add(theEntity);
                theEntity.LockOwner = this;
                ownershipTaken = true;
            }
            return ownershipTaken;
        }

        public bool HasOwnership(CombatEntity theEntity)
        {
            bool targetFound = false;
            targetFound = m_lockedList.Contains(theEntity);
            return targetFound;
        }

        public List<Character> GetCharacters
        {
            get { return m_characterList; }
        }
        public List<CombatEntity> GetCurrentLocks
        {
            get { return m_lockedList; }
        }
        /// <summary>
        /// Send Any client notification required when a lock is made
        /// </summary>
        /// <param name="theEntity">the entity that this has just become the owner for</param>
        public void NotifyOwnershipTaken(CombatEntity theEntity)
        {
            float notificationRangeSQR = 30*30;
            Vector3 mobPos = theEntity.CurrentPosition.m_position;
            for (int i = 0; i < m_characterList.Count; i++)
            {
                Character currentChar = m_characterList[i];
                Vector3 charPos = currentChar.CurrentPosition.m_position;
                double distSQR = Utilities.Difference2DSquared(mobPos, charPos);
                if (notificationRangeSQR > distSQR && currentChar.m_player!=null)
                {
                  //  Program.processor.SendPlaySound2D(currentChar.m_player, "player_hit_by_missile");
                }
            }
        }
        #endregion
    }
}
