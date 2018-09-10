using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Analytics.Simple
{
    public class ClientDevice
    {
        public string eventName = "clientDevice";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_ClientDevice eventParams = new EventParams_ClientDevice();

        public ClientDevice()
        { }

        public ClientDevice(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_ClientDevice
    {
        //we collect model, generation, ios version and device name
        //public string deviceName = "";
        public string deviceType = "";//device model
        //public string hardwareVersion = "";
        public string manufacturer = "";
        public string operatingSystem = "";//OS Name
        public string operatingSystemVersion = "";
        //public string browserName = "";
        //public string browserVersion = "";

        public EventParams_ClientDevice()
        { }
    }
}
