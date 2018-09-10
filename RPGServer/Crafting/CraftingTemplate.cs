using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainServer.Crafting
{
    class CraftingTemplate
    {
         //Recipe fields
        public string recipeName { get; set; }
        public int successMinChance { get; set; }
        public int successMaxChance { get; set; }
        public int criticalMinChance { get; set; }
        public int criticalMaxChance { get; set; }
        public int masterMinChance { get; set; }
        public int masterMaxChance { get; set; }
        public int minAbility { get; set; }
        public int Difficulty { get; set; }
        public int recipeLevel { get; set; }
        public int recipeXP { get; set; }
        public int failureItemReward { get; set; }
        public int successItemReward { get; set; }
        public int criticalItemReward { get; set; }
        public int masterItemReward { get; set; }
        public int craftingTime { get; set; }
        public int recipeID { get; set; }
        public int costID { get; set; }
        public int optionalItemID { get; set; }
        

        public CraftingTemplate(string recipe_name,int success_min_chance, int success_max_chance, int critical_min_chance, int critical_max_chance,int master_min_chance, int master_max_chance
            ,int min_ability,int difficulty, int recipe_level, int recipe_xp,int failure_item_reward,int success_item_reward, int critical_item_reward,int master_item_reward,int crafting_time
            ,int recipe_id, int cost_id, int optional_item_id)
        {
            this.recipeName = recipe_name;
            this.successMinChance = success_min_chance;
            this.successMaxChance = success_max_chance;
            this.criticalMinChance = critical_min_chance;
            this.criticalMaxChance = critical_max_chance;
            this.masterMinChance = master_min_chance;
            this.masterMaxChance = master_max_chance;
            this.minAbility = min_ability;
            this.Difficulty = difficulty;
            this.recipeLevel = recipe_level;
            this.recipeXP = recipe_xp;
            this.failureItemReward = failure_item_reward;
            this.successItemReward = success_item_reward;
            this.criticalItemReward = critical_item_reward;
            this.masterItemReward = master_item_reward;
            this.craftingTime = crafting_time;
            this.recipeID = recipe_id;
            this.costID = cost_id;
            this.optionalItemID = optional_item_id;
            
        }
    }
}
