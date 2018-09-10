using System.Collections.Generic;

namespace MainServer.CombatAlgorithms
{
    interface IEvasionFactorDatabase
    {
        IDictionary<CLASS_TYPE, float> SetUpEvasionFactors();
    }

    class EvasionFactorDatabase : IEvasionFactorDatabase
    {
        private readonly SqlQuery query;

        public EvasionFactorDatabase(SqlQuery query)
        {
            this.query = query;
        }

        public IDictionary<CLASS_TYPE, float> SetUpEvasionFactors()
        {
            var evasionFactors = new Dictionary<CLASS_TYPE, float>();
            query.ExecuteCommand("select * from evasion_factors order by class_id");

            while (query.Read())
            {
                int classId = query.GetInt32("class_id");
                float evasionFactor = query.GetFloat("evasion_factor");

                evasionFactors.Add((CLASS_TYPE)classId, evasionFactor);
            }
            query.CleanUpAfterExecute();

            return evasionFactors;
        }
    }
}
