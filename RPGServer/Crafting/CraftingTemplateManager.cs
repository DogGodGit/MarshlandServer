using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainServer.Crafting
{
    class CraftingTemplateManager
    {
        public Dictionary<int, CraftingTemplate> CraftingTemplates { get; private set; }


        public CraftingTemplateManager()
        {
            CraftingTemplates = new Dictionary<int, CraftingTemplate>();

            Initialize();



        }

        private void Initialize()
        {
            SqlQuery craftingQuery = new SqlQuery(Program.processor.m_dataDB, "select * from recipes");
            while (craftingQuery.Read())
            {
                string recipe_name = craftingQuery.GetString("recipe_name");
                int success_min_chance = craftingQuery.GetInt32("success_min_chance");
                int success_max_chance = craftingQuery.GetInt32("success_max_chance");
                int critical_min_chance = craftingQuery.GetInt32("critical_min_chance");
                int critical_max_chance = craftingQuery.GetInt32("critical_max_chance");
                int master_min_chance = craftingQuery.GetInt32("master_min_chance");
                int master_max_chance = craftingQuery.GetInt32("master_max_chance");
                int min_ability = craftingQuery.GetInt32("min_ability");
                int difficulty = craftingQuery.GetInt32("difficulty");
                int recipe_level = craftingQuery.GetInt32("recipe_level");
                int recipe_xp = craftingQuery.GetInt32("recipe_xp");
                int failure_item_reward = craftingQuery.GetInt32("failure_item_reward");
                int success_item_reward = craftingQuery.GetInt32("success_item_reward");
                int critical_item_reward = craftingQuery.GetInt32("critical_item_reward");
                int master_item_reward = craftingQuery.GetInt32("master_item_reward");
                int crafting_time = craftingQuery.GetInt32("crafting_time");
                int recipe_id = craftingQuery.GetInt32("recipe_id");
                int cost_id = craftingQuery.GetInt32("cost_id");
                int optional_item_id = craftingQuery.GetInt32("optional_item_id");
                

                CraftingTemplate faction = new CraftingTemplate(recipe_name,success_min_chance,  success_max_chance,  critical_min_chance,  critical_max_chance, master_min_chance,  master_max_chance
            , min_ability, difficulty,  recipe_level,  recipe_xp, failure_item_reward, success_item_reward,  critical_item_reward, master_item_reward, crafting_time
            , recipe_id,  cost_id,  optional_item_id);
                CraftingTemplates.Add(recipe_id, faction);
            }
        }

    }
}
