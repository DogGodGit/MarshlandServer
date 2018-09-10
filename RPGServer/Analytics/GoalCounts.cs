using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer;

namespace Analytics.Global
{
    public class GoalCounts
    {
        public int userLevel = -1;
        public Int64 experience = -1;
        public int health = -1;
        public int energy = -1;
        public int strength = -1;
        public int dexterity = -1;
        public int focus = -1;
        public int vitality = -1;
        public int attack = -1;
        public int defence = -1;
        public int damage = -1;
        public int armour = -1;
        public int gold = -1;
        public int platinum = -1;


        public GoalCounts()
        { }

        internal void setValues(Player player)
        {
            if (player != null)
            {
                platinum = player.m_platinum;

                if (player.m_activeCharacter != null)
                {
                    userLevel = player.m_activeCharacter.Level;
                    experience = player.m_activeCharacter.m_experience;
                    health = player.m_activeCharacter.CurrentHealth;
                    energy = player.m_activeCharacter.CurrentEnergy;
                    strength = player.m_activeCharacter.Strength;
                    dexterity = player.m_activeCharacter.Dexterity;
                    focus = player.m_activeCharacter.Focus;
                    vitality = player.m_activeCharacter.Vitality;
                    attack = player.m_activeCharacter.Attack;
                    defence = player.m_activeCharacter.Defence;
                    damage = player.m_activeCharacter.TotalWeaponDamage;
                    armour = player.m_activeCharacter.ArmourValue;
                    gold = player.m_activeCharacter.m_inventory.m_coins;
                }
            }
        }

        public void setValues(int i_userLevel, int i_XP, int i_health, int i_energy, int i_strength, int i_dexterity, int i_focus,
                            int i_vitality, int i_attack, int i_defence, int i_damage, int i_armour, int i_gold, int i_platinum)
        {
            userLevel = i_userLevel;    experience = i_XP;
            health = i_health;          energy = i_energy;
            strength = i_strength;      dexterity = i_dexterity;
            focus = i_focus;            vitality = i_vitality;
            attack = i_attack;          defence = i_defence;
            damage = i_damage;          armour = i_armour;
            gold = i_gold;              platinum = i_platinum;
        }
    }
}
