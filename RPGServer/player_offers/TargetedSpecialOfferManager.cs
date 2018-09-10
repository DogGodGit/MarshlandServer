#region Includes

// Includes //
using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace MainServer.player_offers
{
    // TargetedSpecialOfferManager Class //

    #region TargetedSpecialOfferManager Details

    // ------------------------------------------------------------------------------------------------------------------------------- //
    // Designed to take in a single ';' and ',' seperated string and break it down into a list of conditions                           //
    // These statements results are found from the passed character object                                                             //
    // The design of the string is that each conditional statement is seperated by a ';'                                               //
    // Each of the conditions parameters are then seperated by a ','                                                                   //
    // ------------------------------------------------------------------------------------------------------------------------------- //
    // The following keywords for statements are supported:                                                                            //
    // - accountage,min,max         - min and max are in days from the current date - 'accountage,0,30' is accounts under 30 days old  //
    // - class,druid,mage           - anywhere from 1 to all 5 classes can be specified (by NAME only)                                 //
    // - gender,female              - can target male or female characters                                                             //
    // - level,min,max              - 'level,100,200' is where the logged in character is between level 100 and 200                    //
    // - lastlogin,min,max          - 'lastlogin,0,60' is where the account has been active within the last 60 days                    //
    // - platpurchased,min,max      - 'platpurchased,1000,1000000 is accounts who have purchased over 1k platinum (under a million...) //
    // - platrewarded,min,max       - 'platrewarded,100,1000 is accounts who have earned between 100 and 1000 platnium through offers  //
    // - playtime,timescale,min,max - 'playtime,timescale,120,<somenumber> - timescale converts min max for the correct range          //
    // - realm,balor                - anywhere from 1 to all 18 servers can be specified (by NAME only)                                //
    // ------------------------------------------------------------------------------------------------------------------------------- //
    // As many conditions can be combined as desired to allow for the targeting of special offers - seperated by a ';'                 //
    // Names are NOT case sensitive - all strings are set to .ToLower()                                                                //
    // An invalid string will cause a message to be displayed, and for the whole set of condtions to be condsidered false              //
    // Same goes for the parameters, min/max conditions MUST have 3 params, others require atleast 2 with defined upper limits         //
    // Currently nothing to stop silly numbers being used...                                                                           //
    // ***Hard-Coded Warning***                                                                                                        //                 
    // Gender Enum, Coniditons, Timescale & Platforms                                                                                  //
    // ------------------------------------------------------------------------------------------------------------------------------- //

    #endregion

    public class TargetedSpecialOfferManager
    {
        #region Const Variables

        // Range Variables //
        private const int MIN_MAX_PARAMS      = 3;  // string targetType, int minValue, int maxValue - always 3 params
        private const int PLAY_TIME_PARAMS    = 4;  // playtime, string timeScale, int min, int max  - always 4 params
        private const int MIN_CLASS_PARAMS    = 2;  // string class, string className                - least amount of params is 2
        private const int MAX_CLASS_PARAMS    = 6;  // class,druid,hunter,mage,rogue,warrior         - max is 6 (though having all 5 classes is pointless...)
        private const int MIN_GENDER_PARAMS   = 2;  // string gender, string genderType              - least amount of params is 2
        private const int MAX_GENDER_PARAMS   = 3;  // gender,female,male                            - max is 3
        private const int MIN_PLATFORM_PARAMS = 2;  // platform, string platformType                 - atleast 2 params  
        private const int MAX_PLATFORM_PARAMS = 4;  // platform, internal, ios, android              - max is 4
        private const int MIN_REALM_PARAMS    = 2;  // string realm, string realmName                - least amount of params is 2
        private const int MAX_REALM_PARAMS    = 19; // realm,iOS worlds, Android worlds              - max is 29 (14 iOs, 4 Android)
        
        // Condition Keywords //
        private const string ACCOUNT_AGE    = "accountage";
        private const string CLASS          = "class";
        private const string GENDER         = "gender";
        private const string LEVEL          = "level";
        private const string LAST_LOGIN     = "lastlogin";
        private const string PLAT_PURCHASED = "platpurchased";
        private const string PLAT_REWARDED  = "platrewarded";
        private const string PLAY_TIME      = "playtime";
        private const string REALM          = "realm";

        // Timespan Keywords //
        private const string SECONDS = "seconds";
        private const string MINUTES = "minutes";
        private const string HOURS   = "hours";
        private const string DAYS    = "days";

        // Gender Keywords //
        private const string MALE   = "male";
        private const string FEMALE = "female";

        // Platform Keywords //
        private const string INTERNAL = "both";
        private const string IOS      = "ios";
        private const string ANDROID  = "android";

        // Max Keyword //
        private const string MAX     = "max";
        private const int    INT_MAX = 2147483647;

        #endregion

        #region Variables

        // Private Variables //
        private List<bool> m_conditionResults = new List<bool>();  // list containing the result of each condition
        private int        m_worldId          = Program.m_worldID; // the world id is contained within the server
        private string     m_platform         = null;              // the platform this server is for
        private bool       m_badDataReceived  = false;             // flag if incorrect data is received
        private int[]      BAD_REALM_ID       = { -1 };            // bad realm int array

        // Sql Data Strings //
        private const string GET_REALM_PLATFORM_SQL = "select platform from worlds where world_id = {0}"; // sql query to set this servers platform string
        private const string GET_REALM_DATA_SQL     = "select world_id, world_name from worlds";          // sql query to populate the world data dictionary
        private const string GET_CLASS_DATA_SQL     = "select class_id, name from class";                 // sql query to populate the class data dictionary

        // Data Dictionaries //
        private Dictionary<string, int> m_realmData = new Dictionary<string, int>(); // container for this platforms world name strings and their world id ints
        private Dictionary<string, int> m_classData = new Dictionary<string, int>(); // container for the class name strings and the ints for their enumerated values

        #endregion

        #region Constructor

        // TargetedSpecialOfferManager                                   //
        // Get the platform, world list and class data from the database //
        public TargetedSpecialOfferManager(Database db)
        {
            // Get the platform string for this world using its world_id
            SqlQuery query = new SqlQuery(db, String.Format(GET_REALM_PLATFORM_SQL, m_worldId));

            if (query.HasRows)
            {
                while (query.Read())
                {
                    if (!query.isNull("platform"))
                    {
                        m_platform = query.GetString("platform").ToLower();
                    }
                }
            }

            query.Close();

            // Get this platforms worlds list (ids and names)
            query = new SqlQuery(db, GET_REALM_DATA_SQL);

            if (query.HasRows)
            {
                while (query.Read())
                {
                    int    world_id   = query.GetInt32("world_id");
                    string world_name = query.GetString("world_name");

                    m_realmData.Add(world_name.ToLower(), world_id);
                }
            }

            query.Close();

            // Get the class ids and names from the database
            query = new SqlQuery(db, GET_CLASS_DATA_SQL);

            if (query.HasRows)
            {
                while (query.Read())
                {
                    int    class_id   = query.GetInt32("class_id");
                    string class_name = query.GetString("name");

                    m_classData.Add(class_name.ToLower(), class_id);
                }
            }

            query.Close();

            // Done with the query
            query = null;
        }

        #endregion

        #region Internal Functions

        // AddTargetedSpecialOffer //

        /// <summary>
        /// Boolean function to be used to determine if a special offer has been targetted at the owner character
        /// </summary>
        /// <param name="owner"> The character in question - used to gain access to: accountID, characterID, class, level, gender, platinum purchased, account age, last login and play time </param>
        /// <param name="targetedSpecialOfferString"> The string contained within the new column added to special offers </param>
        /// <returns> True if all passed conditions are true - otherwise false </returns>
        internal bool AddTargetedSpecialOffer(Character owner, string targetedSpecialOfferString, int targetedSpecialOfferNumber)
        {
            // Check for bad params
            if (owner == null || targetedSpecialOfferString == null)
            {
                Program.Display("ERROR - TargetedSpecialOfferManager.cs received a null paramater!");
                return false;
            }

            // Split the incoming Sql column by semi-colons to get the individual target conditions
            string[] targetStrings = targetedSpecialOfferString.Split(';');

            // Check each condition
            foreach(string targetCondition in targetStrings)
            {
                // Get each condition substring
                string[] parameters = targetCondition.Split(',');

                // Remove any whitespace
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = parameters[i].Trim();
                }

                // Add the result of each statement to the list
                m_conditionResults.Add(CheckCondition(owner, parameters));
            }

            // Final bool
            bool targetedSpecialOffer = false;

            // If bad data was received - return false
            if (m_badDataReceived)
            {
                Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs a targeted special offer: {0} - has incorrect elements!", targetedSpecialOfferNumber));
                targetedSpecialOffer = false;
            }
            // Otherwise loop through the conditions
            else
            {
                // Check each condition
                foreach (bool condition in m_conditionResults)
                {
                    // Set as we go
                    targetedSpecialOffer = condition;

                    // If one has failed then break out
                    if (targetedSpecialOffer == false)
                    {
                        break;
                    }
                }
            }

            // Reset flags, clear lists and queries
            m_conditionResults.Clear();
            m_badDataReceived = false;

            // Return 
            return targetedSpecialOffer;
        }

        #endregion

        #region Main Logic

        // CheckCondition                                                                                     //
        // Checks each incoming condition and returns wether its requires have been met (true) or not (false) //
        private bool CheckCondition(Character owner, string[] parameters)
        {
            switch (parameters[0].ToLower())
            {
                case LEVEL:
                case PLAT_PURCHASED:
                case PLAT_REWARDED:
                {
                    // Check that we have the correct number of variables (3) - <querytype>,min,max
                    if (parameters.Length != MIN_MAX_PARAMS)
                    {
                        m_badDataReceived = true;
                        Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid number of arguments for condition string: {0} !", parameters[0].ToLower()));
                        return false;
                    }

                    // Get the number which is to be tested against the passed range
                    int number = GetValue(owner, parameters[0].ToLower());

                    // Try parse the min and max values
                    int min, max;
                    bool minParsed = Int32.TryParse(parameters[1], out min);
                    bool maxParsed = Int32.TryParse(parameters[2], out max);

                    // If the final param doesnt parse to an int, but is "max" - use the max value of an int
                    if (!maxParsed && parameters[2] == MAX)
                    {
                        max       = INT_MAX;
                        maxParsed = true;
                    }

                    // If either fails display an error message and set the condition to false (failing the whole offer)
                    if (!minParsed || !maxParsed)
                    {
                        m_badDataReceived = true;
                        Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid number within the conditon: {0},min,max !", parameters[0].ToLower()));
                        return false;
                    }

                    // If within range return true
                    if ((number >= min) && (number <= max))
                    {
                        return true;
                    }
                    // Else return false
                    else
                    {
                        return false;
                    }
                }
                case PLAY_TIME:
                {
                    // Check that we have the correct number of variables (4) - playtime,timescale,min,max
                    if (parameters.Length != PLAY_TIME_PARAMS)
                    {
                        m_badDataReceived = true;
                        Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid number of arguments for condition string: {0} !", parameters[0].ToLower()));
                        return false;
                    }

                    // Get the number which is to be tested against the passed range
                    int number = GetValue(owner, parameters[0].ToLower());

                    // Try parse the timescale and min and max values
                    int timescale, min, max;
                    timescale = GetTimeScale(parameters[1].ToLower());
                    bool minParsed = Int32.TryParse(parameters[2], out min);
                    bool maxParsed = Int32.TryParse(parameters[3], out max);

                    // If the final param doesnt parse to an int, but is "max" - use the max value of an int
                    if (!maxParsed && parameters[3] == MAX)
                    {
                        max       = INT_MAX;
                        maxParsed = true;
                    }

                    // If either fails or GetTimeScale returns -1 display an error message and set the condition to false (failing the whole offer)
                    if (!minParsed || !maxParsed || (timescale == -1))
                    {
                        m_badDataReceived = true;
                        Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid number within the conditon: {0},timescale,min,max !", parameters[0].ToLower()));
                        return false;
                    }
                    // Check for integer overflow 
                    else if (parameters[3] == MAX || TooBigForAnInt(parameters[1], max))
                    {
                        min *= timescale;
                        max  = INT_MAX;
                    }
                    // Otherwise convert the min and max values to seconds
                    else
                    {
                        min *= timescale;
                        max *= timescale;
                    }

                    // If within range return true
                    if ((number >= min) && (number <= max))
                    {
                        return true;
                    }
                    // Else return false
                    else
                    {
                        return false;
                    }
                }
                case CLASS:
                {
                    // Check that we have a correct number of variables between 2 and 6 - class,druid,hunter,mage,rogue,warrior (from 1 to 5 classes can be passed)
                    if (parameters.Length < MIN_CLASS_PARAMS || parameters.Length > MAX_CLASS_PARAMS)
                    {
                        m_badDataReceived = true;
                        Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid number of arguments for condition string: {0} !", parameters[0].ToLower()));
                        return false;
                    }

                    // Get the current characters class
                    int ownerClass = (int)owner.m_class.m_classType;
                    
                    // Loop through the passed classes and return true if they match
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        // Default to -1
                        int classID = -1;

                        // If the realm data dictonary has that world in it
                        if (m_classData.ContainsKey(parameters[i].ToLower()))
                        {
                            // Get its value world id
                            m_classData.TryGetValue(parameters[i].ToLower(), out classID);

                            // If their the same - this realm is targeted
                            if (ownerClass == classID)
                            {
                                return true;
                            }
                        }
                    }

                    // Otherwise with no matches return false
                    return false;
                }
                case ACCOUNT_AGE:
                case LAST_LOGIN:
                {
                    // Check that we have the correct number of variables (3) - <queryType>,min,max
                    if (parameters.Length != MIN_MAX_PARAMS)
                    {
                        m_badDataReceived = true;
                        Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid number of arguments for condition string: {0} !", parameters[0].ToLower()));
                        return false;
                    }

                    // Set the correct base DateTime value
                    DateTime dateTime = GetDateTime(owner, parameters[0].ToLower());

                    // Try parse the min and max values
                    int min, max;
                    bool minParsed = Int32.TryParse(parameters[1], out min);
                    bool maxParsed = Int32.TryParse(parameters[2], out max);

                    // If the final param doesnt parse to an int, but is "max" - use the max value of an int
                    if (!maxParsed && parameters[2] == MAX)
                    {
                        max       = INT_MAX;
                        maxParsed = true;
                    }

                    // If either fails display an error message and set the condition to false (failing the whole offer)
                    if (!minParsed || !maxParsed)
                    {
                        m_badDataReceived = true;
                        Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid number within the conditon: {0},min,max !",parameters[0].ToLower()));
                        return false;
                    }

                    // Get the time span between that date and now
                    TimeSpan timeSpan = DateTime.Now - dateTime; 

                    // If the timespan is within the specified range - return true
                    if ((timeSpan.TotalDays >= min) && (timeSpan.TotalDays <= max))
                    {
                        return true;
                    }
                    // Otherwise return false
                    else
                    {
                        return false;
                    }
                }
                case REALM:
                {
                    // Check that we have a correct number of variables between 2 and 29 - realm,balor.... etc (from 1 to 18 realms can be passed)
                    if (parameters.Length < MIN_REALM_PARAMS || parameters.Length > MAX_REALM_PARAMS)
                    {
                        m_badDataReceived = true;
                        Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid number of arguments for condition string: {0} !", parameters[0].ToLower()));
                        return false;
                    }

                    // Loop through the passed realms and return true if one matches
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        // Default to -1
                        int worldID = -1;

                        // If the realm data dictonary has that world in it
                        if (m_realmData.ContainsKey(parameters[i].ToLower()))
                        {
                            // Get its value world id
                            m_realmData.TryGetValue(parameters[i].ToLower(), out worldID);

                            // If their the same - this realm is targeted
                            if (m_worldId == worldID)
                            {
                                return true;
                            }
                        }
                    }

                    // Otherwise with no matches return false
                    return false;
                }
                case GENDER:
                {
                    // Check that we have a correct number of variables between 2 and 3 - gender,female,male (either or both can be passed)
                    if (parameters.Length < MIN_GENDER_PARAMS || parameters.Length > MAX_GENDER_PARAMS)
                    {
                        m_badDataReceived = true;
                        Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid number of arguments for condition string: {0} !", parameters[0].ToLower()));
                        return false;
                    }

                    // Get the current characters class
                    int ownerGender = (int)owner.m_gender;

                    // Loop through the passed gender and return true if they match
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        if (ownerGender == GenderToInt(parameters[i]))
                        {
                            return true;
                        }
                    }

                    // Otherwise with no matches return false
                    return false;
                }
                default:
                {
                    m_badDataReceived = true;
                    Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs received an invalid condition string: {0} !", parameters[0]));
                    return false;
                }
            }
        }

        #endregion

        #region Private Functions

        // TooBigForAnInt                                                     //
        // Checks the passed max value and timescale arent over 2,147,483,647 //
        private bool TooBigForAnInt(string timescale, int value)
        {
            switch(timescale.ToLower())
            {
                case MINUTES:
                {
                    return value > 35791394 ? true : false;
                }
                case HOURS:
                {
                    return value > 596523 ? true : false;
                }
                case DAYS:
                {
                    return value > 24855 ? true : false;
                }
                default:
                {
                    m_badDataReceived = true;
                    Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs TooBigForAnInt() received an unknown timescale: {0} !", timescale.ToLower()));
                    return true;
                }
            }
        }

        // String Conversion Functions                                                                                   //
        // These functions convert incoming substrings into real values - mostly integers representing enumerated values //
        // Every function has a default case which set the bad data flag and prints a message explaining where           //

        // GetTimeScale                                                             //
        // Returns the number required to multiply by based on the passed timescale //
        private int GetTimeScale(string timescale)
        {
            // Return the correct multiplier
            switch(timescale.ToLower())
            {
                case SECONDS:
                {
                    return 1;
                }
                case MINUTES:
                {
                    return 60;
                }
                case HOURS:
                {
                    return 3600;
                }
                case DAYS:
                {
                    return 86400;
                }
                default:
                {
                    m_badDataReceived = true;
                    Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs GetTimeScale() received an unknown timescale: {0} !", timescale.ToLower()));
                    return -1;
                }
            }
        }

        // GetValue                                                                             //
        // Returns the value to compared against the passed range based on the condition string //
        private int GetValue(Character owner, string condition)
        {
            // Get the number which is to be checked against the range
            switch (condition.ToLower())
            {
                case LEVEL:
                {
                    return owner.Level;
                }
                case PLAT_PURCHASED:
                {
                    return owner.m_player.m_plat_purchased; 
                }
                case PLAT_REWARDED:
                {
                    return owner.m_player.m_platRewarded;
                }
                case PLAY_TIME:
                {
                    return owner.m_player.m_playTime != null ? (int)owner.m_player.m_playTime : -1;
                }
                default:
                {
                    m_badDataReceived = true;
                    Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs GetValue() received an unknown string: {0} !", condition.ToLower()));
                    return -1;
                }
            }
        }

        // GetDateTime                                                 //
        // Returns the required DateTime form the passed Player object //
        private DateTime GetDateTime(Character owner, string condition)
        {
            switch (condition.ToLower())
            {
                case ACCOUNT_AGE:
                {
                    if (owner.m_player.m_accountAge != null)
                    {
                        return (DateTime)owner.m_player.m_accountAge;
                    }
                    else
                    {
                        m_badDataReceived = true;
                        Program.Display("ERROR - TargetedSpecialOfferManager.cs owners account age was NULL !");
                        return DateTime.Now;
                    }
                }
                case LAST_LOGIN:
                {
                    if (owner.m_player.m_accountAge != null)
                    {
                        return (DateTime)owner.m_player.m_lastLogin;
                    }
                    else
                    {
                        m_badDataReceived = true;
                        Program.Display("ERROR - TargetedSpecialOfferManager.cs owners last login was NULL !");
                        return DateTime.Now;
                    }
                }
                default:
                {
                    m_badDataReceived = true;
                    Program.Display("ERROR - TargetedSpecialOfferManager.cs GetDateTime() received an unknown case!");
                    return DateTime.Now;
                }
            }
        }

        // GenderToInt                                    //
        // Convert the passed gender to its integer value //
        private int GenderToInt(string genderName)
        {
            // Return the correct integer value
            switch (genderName.ToLower())
            {
                case MALE:
                {
                    return 1;
                }
                case FEMALE:
                {
                    return 2;
                }
                default:
                {
                    m_badDataReceived = true;
                    Program.Display(String.Format("ERROR - TargetedSpecialOfferManager.cs GenderToInt() received an unknown string: {0} !", genderName.ToLower()));
                    return -1;
                }
            }
        }

        #endregion
    }
}
