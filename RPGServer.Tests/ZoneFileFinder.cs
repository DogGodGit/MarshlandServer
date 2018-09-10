using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RPGServer.Tests
{
    class ZoneFileFinder
    {
      

        public static void Validate(string fileName)
        {
            string path = ConfigurationManager.AppSettings["collisionmappath"];
            Assert.True(File.Exists(Path.Combine(path, fileName)), "File missing: " + fileName);
            
        }
    }
}
