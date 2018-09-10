using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytics.Engagment
{
    class NotificationServices
    {
        public string eventName = "notificationServices";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_NotificationServices eventParams;

        public NotificationServices()
        { }

        public NotificationServices(long i_userID, uint i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID.ToString();
            sessionID = i_sessionID.ToString();
            eventTimestamp = i_eventTimestamp;
        }
    }

    abstract class EventParams_NotificationServices
    {
        //push string for android
        public string platform; 
      //push string for IOS
        //public string sdkVersion;
    }

    class EventParams_NotificationServicesIOS : EventParams_NotificationServices
    {
        public EventParams_NotificationServicesIOS(string i_pushNotificationToken)
        {
            pushNotificationToken = i_pushNotificationToken;
        }
        public string pushNotificationToken; 
    }

    class EventParams_NotificationServicesAndroid : EventParams_NotificationServices
    {
        public EventParams_NotificationServicesAndroid(string i_androidRegistrationID)
        {
            androidRegistrationID = i_androidRegistrationID;
        }
        public string androidRegistrationID;
    }
}
 