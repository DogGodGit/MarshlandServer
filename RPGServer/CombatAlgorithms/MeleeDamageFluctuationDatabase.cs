using System.Collections.Generic;

namespace MainServer.CombatAlgorithms
{
    interface IMeleeDamageFluctuationDatabase
    {
        IDictionary<CLASS_TYPE, float> SetUpMeleeDamageFluctuation();
    }

    class MeleeDamageFluctuationDatabase : IMeleeDamageFluctuationDatabase
    {
        private readonly SqlQuery query;

        public MeleeDamageFluctuationDatabase(SqlQuery query)
        {
            this.query = query;
        }

        public IDictionary<CLASS_TYPE, float> SetUpMeleeDamageFluctuation()
        {
            var meleeDamageFlucations = new Dictionary<CLASS_TYPE, float>();
            query.ExecuteCommand("select * from melee_damage_fluctuations order by class_id");

            while (query.Read())
            {
                int classId = query.GetInt32("class_id");
                float meleeDamageFlucation = query.GetFloat("melee_damage_fluctuation");

                meleeDamageFlucations.Add((CLASS_TYPE)classId, meleeDamageFlucation);
            }

            query.CleanUpAfterExecute();

            return meleeDamageFlucations;
        }
    }
}
