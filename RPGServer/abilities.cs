using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Localise;

namespace MainServer
{
    public enum ABILITY_TYPE
    {
        NA = -1,
        SWORD = 0,
        BLUNT = 1,
        AXE = 2,
        STAFF = 3,
        DAGGER = 4,
        WAND = 5,
        BOW = 6,
        COMBAT_TACTICS = 7,
        RANGED_COMBAT = 8,
        PHYSICAL_FITNESS = 9,
        NATURE_MAGIC = 10,
        CUNNING = 11,
        ICE_MAGIC = 12,
        FIRE_MAGIC  = 13,
        FIRST_AID = 14,
        SPEAR = 15,
        TOTEM=16,
        NOVELTY_ITEM = 17,
        HAND_TO_HAND = 18,
        FORTITUDE = 19,
        WARDING = 20,
        EVASION = 21,
        VIGOUR = 22,
        WILLPOWER = 23,
		DOGTAMING = 24,
		RABBITTAMING = 25,
		FISHING = 26,
        BEARTAMING = 27,
        WISPTAMING = 28,
        CRITICAL_STRIKE = 29,
        CRITICAL_SKILL = 30,
        TREASURE_HUNTER = 31,
        SCHOLAR = 32,
        WOLF_TAMING = 33,
        SPIDER_TAMING = 34,
        BOAR_TAMING = 35,
        WOLF_RIDING = 36,
        BEAR_RIDING = 37,
        COOKING_PROFICIENCY = 38,
        COOKING_MASTERY = 39
    };

	/// <summary>
	/// Ability of playeraaa
	/// </summary>
    public class Ability
    {
		public Ability(ABILITY_TYPE ability_id, float baseUpgradeChance, float upgradeModifer,string ability_name,int class_restriction)
        {
            m_ability_id = ability_id;
            m_BaseUpgradeChance = baseUpgradeChance;
            m_UpgradeModifier = upgradeModifer;
            m_abilityName = ability_name;
            m_classRestriction = class_restriction;

        }
        public ABILITY_TYPE m_ability_id;
        public float m_BaseUpgradeChance;
        public float m_UpgradeModifier;
        public string m_abilityName;
        public int m_classRestriction;
    }
    public class CharacterAbility
    {
        public CharacterAbility(ABILITY_TYPE ability_id, int currentLevel)
        {
            m_ability_id = ability_id;
            m_currentLevel = currentLevel;
        }
        public ABILITY_TYPE m_ability_id;
        public int m_currentLevel;
    }
    static public class AbilityManager
    {
		// #localisation
		static int textDBIndex = 0;

		static List<Ability> m_abilities;
		
        static internal void Setup(Database db)
        {
            m_abilities = new List<Ability>();

            SqlQuery query = new SqlQuery(db, "select * from abilities order by ability_id");
            if (query.HasRows)
            {
                while (query.Read())
                {
                    ABILITY_TYPE ability_id = (ABILITY_TYPE)query.GetInt32("ability_id");
                    float baseChance = query.GetFloat("base_chance");
                    float levelModifier = query.GetFloat("level_modifier");
                    string abilityName = query.GetString("ability_name");
                    int classRestriction = query.GetInt32("class_restriction");
                    m_abilities.Add(new Ability(ability_id,baseChance,levelModifier,abilityName,classRestriction));
                }
            }

            query.Close();

			// Get textNameDB index.
			textDBIndex = Localiser.GetTextDBIndex("abilities");
		}
        static internal Ability getAbility(ABILITY_TYPE ability_id)
        {
            for (int i = 0; i < m_abilities.Count; i++)
            {
                if (m_abilities[i].m_ability_id == ability_id)
                {
                    return m_abilities[i];
                }
            }
            return null;
        }
        static internal bool isAvailable(ABILITY_TYPE ability_id,CLASS_TYPE classType)
        {
           Ability ability=getAbility(ability_id);
           if (ability == null)
               return false;
           int classAnd = (int)Math.Pow(2, (double)classType-1);
           if ((ability.m_classRestriction & classAnd) > 0)
               return true;
           return false;
                
        }

		static internal string GetLocaliseAbilityName(Player player, ABILITY_TYPE ability_id)
		{
			return Localiser.GetString(textDBIndex, player, (int)ability_id);
		}

		/// <summary>
		/// I think this checks if an ability can be upgraded without going out of range (normally ten times player level)
		/// </summary>
		/// <param name="ability_id">id to check with</param>
		/// <param name="currentLevel">current level of ability</param>
		/// <param name="playerLevel">relevant level to check against (main level,fish level etc)</param>
		/// <param name="abilityRateModifier">dunno</param>
		/// <returns>true if we can upgrade ability and still be within our level range</returns>
		public static bool testUpgrade(ABILITY_TYPE ability_id, int currentLevel, int playerLevel, float abilityRateModifier)
		{

			//abilities can't be more than ten times their relevant level
			if (currentLevel >= playerLevel * 10)
				return false;
			
			//do some calculation here?? I think mostly to allow some abilities to leverl up slower than others
			Ability ability = getAbility(ability_id);
			if (ability != null)
			{

				int chance = (int)((ability.m_BaseUpgradeChance + currentLevel * ability.m_UpgradeModifier) / abilityRateModifier);
				//   Program.Display("ability="+ability_id+", ability level="+currentLevel+", player level="+playerLevel+", ability rate modifier="+abilityRateModifier+", chance="+chance);
				if (Program.getRandomNumber(chance) == 0)
				{
					return true;
				}
			}
			return false;
		}

		
    }

}
