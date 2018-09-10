using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MainServer.Localise;

namespace MainServer
{

    class ProfanityHolder
    {
        internal ProfanityHolder(string profanity,string replacement)
        {
            profanityString =profanity;
            replacementString=replacement;                  
        }
        /// <summary>
        /// what word to look for
        /// </summary>
        internal string profanityString = "";
        /// <summary>
        /// if the word is encountered what should replace it
        /// </summary>
        internal string replacementString = "";
    };

    static class ProfanityFilter
    {

        static List<ProfanityHolder> m_profanityList = new List<ProfanityHolder>();
        static List<ProfanityHolder> m_cleanList = new List<ProfanityHolder>();
        static internal void ReadProfanityList(Database dataDB)
        {

           /* m_profanityList.Add(new ProfanityHolder("lol"));
            m_profanityList.Add(new ProfanityHolder("Duck"));
            m_profanityList.Add(new ProfanityHolder("rabbit"));*/
            SqlQuery query = new SqlQuery(dataDB, "select * from profanity_list order by bad_word desc,profanity_string desc");
            if (query.HasRows)
            {
                
                while ((query.Read()))
                {
                    
                    string listedString = query.GetString("profanity_string");
                    if (query.GetBoolean("bad_word"))
                    {
                        ProfanityHolder newEntry = new ProfanityHolder(listedString, "<ps>" + listedString + "<pe>");
                        m_profanityList.Add(newEntry);
                    }
                    else
                    {
                        ProfanityHolder newEntry = new ProfanityHolder(replaceOffendingStrings(listedString),listedString);
                        m_cleanList.Add(newEntry);
                    }

                }

            }
            query.Close();
        }

        static internal bool isAllowed(string stringToCheck)
        {
            string replacementString = replaceOffendingStrings(stringToCheck);

            if (replacementString == stringToCheck)
            {
                return true;
            }

            return false;
        }
        static internal string GetStarredOutOffendingStrings(string stringToCheck)
        {
            string finalString = "";
            string filteredString = replaceOffendingStrings(stringToCheck);

            string[] profanityStart = {"<ps>"};
            string[] profanityEnd = {"<pe>"};
            string[] profanitySplitStrings = filteredString.Split(profanityStart, StringSplitOptions.None);
            //while (searchCompleted == false && safetyCatch < stringToCheck.Length)
            if (profanitySplitStrings.Length > 0)
            {
                finalString += profanitySplitStrings[0];
            }
            for (int i = 1; i < profanitySplitStrings.Length;i++ )
            {
                string currentSection = profanitySplitStrings[i];

                string[] profanityEndStrings = currentSection.Split(profanityEnd, StringSplitOptions.RemoveEmptyEntries);
                if (profanityEndStrings.Length > 1)
                {
                    finalString += "***"+ profanityEndStrings[1];
                }

               

            }


            return finalString;
        }
        static internal string replaceOffendingStrings(string stringToCheck)
        {
           // Program.Display("string to check="+stringToCheck);
            string replacementString = stringToCheck;
            while (replacementString.IndexOf("  ") > -1)
            {
                replacementString = replacementString.Replace("  ", " ");
            }
            replacementString.Replace("<ps>", "");
            replacementString.Replace("<pe>", "");
          //  Program.Display("after clean 1="+replacementString);
            
            string azstring = Regex.Replace(replacementString, Localiser.TextFilter, "").ToLower();
          //  Program.Display("after azstring="+azstring);
            bool found=false;
            for (int i = 0; i < m_profanityList.Count; i++)
            {
                ProfanityHolder currentProfanity = m_profanityList[i];
                if (azstring.IndexOf(currentProfanity.profanityString) > -1)
                {
                    azstring = azstring.Replace(currentProfanity.profanityString, currentProfanity.replacementString);
                    found = true;
                }
            }
            if(!found) 
            {
                return replacementString;
            }
           // Program.Display("after filter="+azstring);
            string newstring="";
            int azpointer=0;
            int profanityLevel=0;
            for (int i = 0; i < replacementString.Length; i++)
            {
                string substring=replacementString.Substring(i,1);
                if(Regex.IsMatch(substring, Localiser.TextFilter))
                {
                    newstring+=substring;
                }
                else
                {
                    while (true)
                    {
                        if (azstring.Length >= azpointer + 4)
                        {
                            string tag = azstring.Substring(azpointer, 4);
                            if (tag.Equals("<ps>"))
                            {
                                azpointer += 4;
                                profanityLevel++;
                                if (profanityLevel == 1)
                                {
                                    newstring += "<ps>";
                                }
                            }
                            else if (tag.Equals("<pe>"))
                            {
                                azpointer += 4;
                                profanityLevel--;
                                if (profanityLevel == 0)
                                {
                                    newstring += "<pe>";
                                }
                            }
                            else
                            {
                                break;
                            }

                        }
                        else
                        {
                            break;
                        }
                    }
                    newstring += substring;
                    azpointer++;
                }
                if (azstring.Length >= azpointer + 4)
                {
                    string tag = azstring.Substring(azpointer, 4);                   
                    if (tag.Equals("<pe>"))
                    {
                        azpointer += 4;
                        profanityLevel--;
                        if (profanityLevel == 0)
                        {
                            newstring += "<pe>";
                        }
                    }
                }
            }

			// we have open but not close tag
			if (profanityLevel > 0)
			{
				newstring += "<pe>";
			}

           // Program.Display("after filter 2=" + newstring);
            for (int i = 0; i < m_cleanList.Count; i++)
            {
                ProfanityHolder currentProfanity = m_cleanList[i];

                newstring = newstring.Replace(currentProfanity.profanityString, currentProfanity.replacementString);

            }
           // Program.Display("after filter 3=" + newstring);
            return newstring;
        }
    }
}
