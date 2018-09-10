using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Localise;

namespace MainServer
{
    public class StatusEffectClass
    {
        public StatusEffectClass(int class_id)
        {
            m_class_id = class_id;
        }

        public int m_class_id;
    }

    static class StatusEffectTemplateManager
    {
        static List<StatusEffectTemplate>     m_statusEffectTemplates;
        static List<StatusEffectClass>        m_statusEffectClasses;
        static Dictionary<int, AuraSubEffect> m_auraSubEffects;


        static Dictionary<int, List<int>> m_whitelistedMobAuras = new Dictionary<int, List<int>>(8);
        static Dictionary<int, List<int>> m_immunitiesAurasOnMob = new Dictionary<int, List<int>>(8);

		// #localisation
		static int textDBIndex = 0;

		static StatusEffectTemplateManager() {}

        static public void FillTemplate(Database db)
        {
            // Create the array of status effects
            m_statusEffectTemplates = new List<StatusEffectTemplate>();
            m_statusEffectClasses   = new List<StatusEffectClass>();
            m_auraSubEffects        = new Dictionary<int, AuraSubEffect>();

            // Get classes
            SqlQuery query = new SqlQuery(db, "select * from status_effect_class");
            while (query.Read())
            {
                StatusEffectClass newClass = new StatusEffectClass(query.GetInt32("status_effect_class_id"));
                m_statusEffectClasses.Add(newClass);
            }
            query.Close();

            // Get templates
            query = new SqlQuery(db, "select * from status_effect_templates");
            while (query.Read())
            {
                StatusEffectTemplate newTemplate = new StatusEffectTemplate (db,query);
                m_statusEffectTemplates.Add(newTemplate);
            }
            query.Close();

			// Get textNameDB index.
			textDBIndex = Localiser.GetTextDBIndex("status_effect_templates");

			// Get auras
			List<int> auraSubEffectIDs = new List<int>();
            query = new SqlQuery(db, "select * from aura_sub_effects");
            while (query.Read())
            {
                AuraSubEffect newAuraSubEffect = new AuraSubEffect(query);
                m_auraSubEffects.Add(newAuraSubEffect.SubEffectID, newAuraSubEffect);
                auraSubEffectIDs.Add(newAuraSubEffect.CharacterEffectID);
            }
            query.Close();

            // read mob immunities to certain auras
            query = new SqlQuery(db, "select aura_id,mob_id from aura_blacklists");
            
            while (query.Read())
            {
                int auraID = query.GetInt32("aura_id");

                if (m_immunitiesAurasOnMob.ContainsKey(auraID) == false)
                    m_immunitiesAurasOnMob[auraID] = new List<int>(2);

                m_immunitiesAurasOnMob[auraID].Add(query.GetInt32("mob_id"));
            }

            // read whitelist auras, aura entries here will disable effects for all unspecified mobs
            query = new SqlQuery(db, "select aura_id,mob_id from aura_whitelists");

            while (query.Read())
            {
                int auraID = query.GetInt32("aura_id");

                if (m_whitelistedMobAuras.ContainsKey(auraID) == false)
                    m_whitelistedMobAuras[auraID] = new List<int>(2);

                m_whitelistedMobAuras[auraID].Add(query.GetInt32("mob_id"));
            }


            // Assign auras
            foreach (StatusEffectTemplate template in m_statusEffectTemplates)
            {
                if (template.AuraSubEffectID > 0)
                {
                    template.AuraEffect = GetAuraSubEffectForID(template.AuraSubEffectID);

                    if (template.AuraEffect == null)
                    {
                        Program.Display(String.Format("StatusEffectTemplateManager.cs - Failed to assign AuraEffect to status effect {0}!", template.StatusEffectID));
                    }
                }

                // Flag AuraSubEffects
                if (auraSubEffectIDs.Contains((int)template.StatusEffectID))
                {
                    template.IsAuraSubEffect = true;
                }
            }
        }

        internal static bool MobIsImmuneToAura(int in_mobID, int in_auraID)
        {
            if (m_immunitiesAurasOnMob.ContainsKey(in_auraID) == false)
                return false;

            return m_immunitiesAurasOnMob[in_auraID].Contains(in_mobID);
        }

        internal static bool AuraHasWhitelist(int in_auraID)
        {
            return m_whitelistedMobAuras.ContainsKey(in_auraID);
        }

        internal static bool AuraWhitelistContainsMob(int in_auraID, int in_mobID)
        {
            if (AuraHasWhitelist(in_auraID) == false)
                return false;

            return m_whitelistedMobAuras[in_auraID].Contains(in_mobID);
        }

        static public StatusEffectTemplate GetStatusEffectTemplateForID(EFFECT_ID ID)
        {
            if (m_statusEffectTemplates == null)
            {
                return null;
            }
            for (int currentTemplate = 0; currentTemplate < m_statusEffectTemplates.Count; currentTemplate++)
            {
                if (m_statusEffectTemplates[currentTemplate].StatusEffectID == ID)
                {
                    return m_statusEffectTemplates[currentTemplate];
                }
            }
            return null;
        }

        static public StatusEffectClass GetStatusEffectClassForID(int ID)
        {
            if (m_statusEffectTemplates == null)
            {
                return null;
            }
            for (int i = 0; i < m_statusEffectClasses.Count; i++)
            {
                if (m_statusEffectClasses[i].m_class_id == ID)
                {
                    return m_statusEffectClasses[i];
                }
            }
            return null;
        }

        static public AuraSubEffect GetAuraSubEffectForID(int ID)
        {
            return m_auraSubEffects.ContainsKey(ID) ? m_auraSubEffects[ID] : null;
        }

		static internal string GetLocaliseStatusEffectName(Player player, int statusEffectID)
		{
			return Localiser.GetString(textDBIndex, player, statusEffectID);
		}
	}
}
