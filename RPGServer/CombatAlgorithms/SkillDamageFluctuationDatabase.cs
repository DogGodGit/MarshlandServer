using System.Collections.Generic;

namespace MainServer.CombatAlgorithms
{
    interface ISkillDamageFluctuationDatabase
    {
        IDictionary<CLASS_TYPE, float> SetUpSkillDamageFluctuations();
    }

    class SkillDamageFluctuationDatabase: ISkillDamageFluctuationDatabase
    {
        private readonly SqlQuery query;

        public SkillDamageFluctuationDatabase(SqlQuery query)
        {
            this.query = query;
        }

        public IDictionary<CLASS_TYPE, float> SetUpSkillDamageFluctuations()
        {
            var skillDamageFlucations = new Dictionary<CLASS_TYPE, float>();
            query.ExecuteCommand("select * from skill_damage_fluctuations order by class_id");

            while (query.Read())
            {
                int classId = query.GetInt32("class_id");
                float skillDamageFlucation = query.GetFloat("skill_damage_fluctuation");

                skillDamageFlucations.Add((CLASS_TYPE)classId, skillDamageFlucation);
            }

            query.CleanUpAfterExecute();

            return skillDamageFlucations;
        }
    }
}
