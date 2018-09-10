using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analytics.Simple;
using Lidgren.Network;
using XnaGeometry;
using Analytics.Gameplay;
using MainServer.Localise;

namespace MainServer.Crafting
{
    class CraftingManager
    {
        public class knownRecipe
        {
            public int recipeID;
            public bool isFavourited;
        }

        Character Character { get; set; }
        Database worldDB { get; set; }
        public List<knownRecipe> recipeList { get; set; }

        private const int FAILURE_MODIFIER  = 0;
        private const int   STANDARD_MODIFIER = 1;
        private const int   CRITICAL_MODIFIER = 2;
        private const int   MASTER_MODIFIER   = 4;

        private CraftingTemplate queuedCraftingTemplate;
        private int OptionalID = -1;
        private double craftingTimer;
        public bool isCrafting;        

        public CraftingManager(Database db, Character character)
       { 
            this.Character = character;
            this.worldDB = db;
            recipeList = new List<knownRecipe>();
            LoadKnownRecipes();
        }

        public void Update()
        {
            if (isCrafting && queuedCraftingTemplate != null)
            {

                //upate the timer
                if (NetTime.Now - craftingTimer > getCraftingTime(queuedCraftingTemplate, this.Character.m_player.m_activeCharacter.LevelCooking))
                {
                    //yes we finishd crafting
                    //send respomse
                    isCrafting = false;
                    int cookingXpGained;
                    int craftedRecipeID = formulateCraftingResponse(queuedCraftingTemplate.recipeID, out cookingXpGained);
                    OptionalID = -1; // reset optional id
                    Program.processor.CraftingNetworkHandler.SendCraftingResponse(this.Character.m_player, craftedRecipeID, cookingXpGained);
                }

            }
        }

        //We need to check if the player is professional or not
        private int getCraftingTime(CraftingTemplate recipe, int craftingLevel)
        {
            if (craftingLevel > recipe.recipeLevel + 20)
            {
                return recipe.craftingTime - 5;
            }
            else
            {
                return recipe.craftingTime;
            }
        }

        public void LoadKnownRecipes()
        {
           SqlQuery recipeQuery = new SqlQuery(worldDB);
           recipeQuery.ExecuteCommand("SELECT * FROM character_recipes WHERE character_id="+Character.m_character_id);

            while (recipeQuery.Read())
            {
                knownRecipe currentRecipe = new knownRecipe();
                currentRecipe.recipeID = recipeQuery.GetInt32("recipe_id");
                currentRecipe.isFavourited = recipeQuery.GetBoolean("is_favourited");

                recipeList.Add(currentRecipe); 
            }
        }

        /// <summary>
        /// Interrupte current crafting and reset crafting states
        /// </summary>
        /// <param name="interruptState"></param>
        public void interruptCraft(bool interruptState)
        {                        
            //if we actually have crafting to interrupt
            if(interruptState && isCrafting)
            {
                //reset crafting
                isCrafting = false;
                craftingTimer = NetTime.Now;
                queuedCraftingTemplate = null;
                string locText = Localiser.GetString(TokenVendors.TokenVendorManager.textDB, Character.m_player, (int)TokenVendors.TokenVendorManager.TokenVendorManagerTextDB.TextID.COOKING_INTERRUPTED);
                this.Character.SendSimpleMessageToPlayer(locText);               
            }                        

        }

        /// <summary>
        /// Check that we have the items for this recipe, and roll some dice on the success of this cooking
        /// </summary>
        /// <param name="recipeID"></param>
        /// <returns></returns>
        public int formulateCraftingResponse(int recipeID, out int xpGained)
        {
            // check for null
            if (Character == null)
            {
                xpGained = 0;
                return 0;
            }

            // check we have the required abilities
            CharacterAbility cookingProficiency = Character.getAbilityById(ABILITY_TYPE.COOKING_PROFICIENCY);
            CharacterAbility cookingMastery = Character.getAbilityById(ABILITY_TYPE.COOKING_MASTERY);
            if (cookingProficiency == null || cookingMastery == null)
            {
                Program.Display(String.Format("CharacterID.{2} Cooking Proficiency.{0} \\ Cooking Mastery.{1}", cookingProficiency, cookingMastery, Character.m_character_id));
                xpGained = 0;
                return 0;
            }

            // get the template for this recipe
            CraftingTemplate craftingTemplate = getCraftingTemplateForID(recipeID);
            
            // if we want to use an optional item deal with this now
            if (OptionalID >= 0)
            {                
                Item optionalItem = Character.m_inventory.m_bagItems.Find(x => x.m_template_id == OptionalID);
                if (optionalItem != null)
                {
                    Character.m_inventory.DeleteItem(optionalItem.m_template_id, optionalItem.m_inventory_id, 1);
                }
                else
                {
                    // no optional item found though requested, reject craft
                    xpGained = 0;
                    return 0;
                }
            }

            // roll the dice
            double rand = Program.m_rand.NextDouble();
            rand = rand * 100;

            float successChance = calculateChance(cookingProficiency.m_currentLevel, craftingTemplate.minAbility,
                craftingTemplate.successMinChance, craftingTemplate.successMaxChance, craftingTemplate.Difficulty);
            float criticalChance = calculateChance(cookingMastery.m_currentLevel, craftingTemplate.minAbility,
                craftingTemplate.criticalMinChance, craftingTemplate.criticalMaxChance, craftingTemplate.Difficulty);
            float masterChance = calculateChance(cookingMastery.m_currentLevel, craftingTemplate.minAbility,
                craftingTemplate.masterMinChance, craftingTemplate.masterMaxChance, craftingTemplate.Difficulty);

            // alter roll if we added optional item
            if (OptionalID >= 0)
            {
                successChance = 100;
                criticalChance = criticalChance*2;
                masterChance = masterChance*2;
            }

            xpGained = GetModifiedXP(Character.LevelCooking, craftingTemplate);
            int craftedItemReward = -1;
            CraftingOutcome outcome = CraftingOutcome.NULL;

            if (rand <= masterChance)
            {
                // master
                xpGained = xpGained * MASTER_MODIFIER;                
                Character.updateCoinsAndXP(0,xpGained,CombatEntity.LevelType.cook);
                Character.testAbilityUpgrade(cookingMastery);
                craftedItemReward = craftingTemplate.masterItemReward;
                outcome = CraftingOutcome.Master;
            }
            else if (rand <= criticalChance)
            {
                // critical
                xpGained = xpGained * CRITICAL_MODIFIER;                
                Character.updateCoinsAndXP(0, xpGained, CombatEntity.LevelType.cook);
                Character.testAbilityUpgrade(cookingMastery);
                craftedItemReward = craftingTemplate.criticalItemReward;
                outcome = CraftingOutcome.Critical;
            }
            else if (rand <= successChance)
            {
                // success
                xpGained = xpGained * STANDARD_MODIFIER;                
                Character.updateCoinsAndXP(0, xpGained, CombatEntity.LevelType.cook);
                Character.testAbilityUpgrade(cookingProficiency);
                craftedItemReward = craftingTemplate.successItemReward;   
                outcome = CraftingOutcome.Success;
            }
            else
            {
                // failure
                xpGained = xpGained * FAILURE_MODIFIER;
                Character.updateCoinsAndXP(0, xpGained, CombatEntity.LevelType.cook);
                craftedItemReward = craftingTemplate.failureItemReward;
                outcome = CraftingOutcome.Failure;
            }

            // log this
           LogCrafting(this.Character.m_character_id, craftingTemplate, OptionalID, craftedItemReward, xpGained, Character.LevelCooking);
           if (Program.m_LogAnalytics)
           {
               //Analytics Insertion
               AnalyticsMain logAnalytics = new AnalyticsMain(false);
               logAnalytics.CraftingEvent(Character.m_player, Program.m_worldID.ToString(), CraftingType.Cooking, craftingTemplate.recipeName, 1, outcome);
           }
            //return id of crafted item
            return craftedItemReward;
        }


        /// <summary>
        /// Log this crafting request in case we need to verify it through support
        /// </summary>
        /// <param name="character_id"></param>
        /// <param name="template"></param>
        /// <param name="optional_item_id"></param>
        /// <param name="crafted_id"></param>
        /// <param name="xp">xp gained for this craft</param>
        /// <param name="cooking_level">current cooking level after crafting</param>
        private void LogCrafting(uint character_id, CraftingTemplate template, int optional_item_id, int crafted_id, int xp, int cooking_level)
        {            
            string insert = String.Format("INSERT into cooking_history (character_id, recipe_id, recipe_name, transaction_date, optional_id, crafted_id, level_and_xp) VALUES " +
                "({0},{1},'{2}','{3}',{4},{5},'{6}')", Character.m_character_id, template.recipeID, template.recipeName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 
                optional_item_id,crafted_id, String.Format("Lvl.{0} +{1}xp",cooking_level,xp));

            Character.m_db.runCommand(insert);            
        }


        public float calculateChance(int current_ability, int min_ability,int min_chance, int max_chance,int difficulty)
        {
            float adjusted_ability = Math.Max(current_ability - min_ability,0);
            float chance_range = Math.Max(max_chance - min_chance, 0);
            float chance = min_chance + ((adjusted_ability/(adjusted_ability + difficulty))*chance_range);
            return chance;
        }

        public CraftingTemplate getCraftingTemplateForID(int recipeID)
        {
            CraftingTemplate craftingTemplate;

            if (Program.processor.CraftingTemplateManager.CraftingTemplates.TryGetValue(recipeID, out craftingTemplate))
            {
                return craftingTemplate;
            }
            return null;
        }

        /// <summary>
        /// GetModifiedXP
        /// Gets the experience value modified by the players cooking level vs the recipes level
        /// Combination of XP functionality from:
        /// ServerControlledEntity.cs getExperienceValue()
        /// Zone.cs SendWonFishCombat() Line: 1466
        /// </summary>
        /// <param name="LevelCooking"> The players cooking level </param>
        /// <param name="craftingTemplate"> The template for which the level and xp amount will be used </param>
        /// <returns></returns>
        internal int GetModifiedXP(int LevelCooking, CraftingTemplate craftingTemplate)
        {
            int levelDiff = LevelCooking - craftingTemplate.recipeLevel;
            if (levelDiff < 0)
            {
                levelDiff = 0;
            }
            int modifiedXP = (int)Math.Round(Math.Pow(Character.EXPERIENCE_ACCELLERATOR, levelDiff) * craftingTemplate.recipeXP);
            modifiedXP     = (int)(modifiedXP * Program.processor.GlobalEXPMod);
            modifiedXP     = (int)(modifiedXP * Character.ExpRate);
            return modifiedXP;
        }

        internal void QueueCraftin(int recipeid, int optionalID)
        {
            //queue up ou craft
            isCrafting = true;
            queuedCraftingTemplate = getCraftingTemplateForID(recipeid);
            OptionalID = optionalID;
            //note the start time
            craftingTimer = NetTime.Now;
        }
    }
}
 