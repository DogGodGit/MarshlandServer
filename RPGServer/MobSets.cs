using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainServer
{
    static class MobSets
    {
        private static Dictionary<int, List<int>> MobSetStorage = new Dictionary<int, List<int>>();

        public static void Load(Database db)
        {
            MobSetStorage.ToList().Clear();

            // Load up free bounties
            SqlQuery query = new SqlQuery(db, "select mob_set_id, mob_id from mob_sets order by mob_set_id, mob_id");
            while (query.Read())
            {
                int key = query.GetInt32("mob_set_id");
                int value = query.GetInt32("mob_id");

                List<int> array;
                if (!MobSetStorage.TryGetValue(key, out array))
                    MobSetStorage[key] = new List<int>(8);

                MobSetStorage[key].Add(value);
            }
            query.Close();
        }

        public static bool QueryMobSet(int mob_set_id, int mob_id)
        {
            List<int> array;
            if (!MobSetStorage.TryGetValue(mob_set_id, out array))
                return false;
            return array.Contains(mob_id);
        }
    }
}
