using System.Configuration;
using System.IO;
using MainServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPGServer.Tests
{
    [TestFixture]
    public class ServerFilePresenceCheck
    {
        private Database unityDataDB;
        private ZoneListFetcher zoneListFetcher;

        [SetUp]
        public void SetUp()
        {
            unityDataDB = new Database(ConfigurationManager.ConnectionStrings["unitydatadb"].ConnectionString);
            zoneListFetcher = new ZoneListFetcher(unityDataDB);
        }


        [Test]
        public void CheckZoneFiles()
        {
            var zonesToCheckFor = zoneListFetcher.Zones;


            foreach (Zone zone in zonesToCheckFor)
            {
                string aiMapFile = "z" + zone.ZoneID + "_aimap.aimap";
                string collisionFile = "z" + zone.ZoneID + "_collisions.txt";
             
                ZoneFileFinder.Validate(aiMapFile);
                ZoneFileFinder.Validate(collisionFile); 
            }
        }

    }


}
