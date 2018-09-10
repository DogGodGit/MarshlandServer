using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MainServer
{
    static class ConfigurationVerifier
    {

        internal static bool Verify(Database m_dataDB)
        {
            return VerifyAppConfig(m_dataDB);
        }

        private static bool VerifyAppConfig(Database m_dataDB)
        {
            string configPlatform = "";
            try
            {
                configPlatform = ConfigurationManager.AppSettings["platform"];
            }
            catch
            {
                //platform app key not present. App.config needs updated.
                return false;
            }

            SqlQuery worldPlatform = new SqlQuery(m_dataDB,
                    "select platform from worlds where world_id=" + Program.m_worldID);
            if (worldPlatform.Read())
            {
                string worldPlatformString = worldPlatform.GetString("platform");

                if (worldPlatformString == "Both")
                    return true;

                if (configPlatform != worldPlatformString)
                {
                    return false;
                }
            }

            return true;
        }

    }

}

