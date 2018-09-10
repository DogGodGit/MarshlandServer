using System.Collections.Generic;
using MainServer;
using NUnit.Framework;

namespace RPGServer.Tests
{
    class Zone
    {
        public int ZoneID { get; set; }
        public string Name { get; set; }
    }

    class ZoneListFetcher
    {
        public List<Zone> Zones { get; private set; }

        public ZoneListFetcher(Database database)
        {
            Zones = new List<Zone>();
            SqlQuery zoneQuery = new SqlQuery(database, "select * from zones where zone_id>0");

            if (zoneQuery.HasRows)
            {
                while (zoneQuery.Read())
                {
                    Zone z = new Zone();
                    z.Name = zoneQuery.GetString("zone_name");
                    z.ZoneID = zoneQuery.GetInt32("zone_id");
                    Zones.Add(z);
                }
            }
        }


    }
}
