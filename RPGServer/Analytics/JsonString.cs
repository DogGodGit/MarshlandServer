using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;


namespace Analytics.Global
{
    public class JsonString
    {
        string serialised_str;
        
        public JsonString()
        {
            serialised_str = "";
        }

        public string getJSONString(object obj)
        {
            serialised_str = JsonConvert.SerializeObject(obj);

            if (serialised_str == "{}" || serialised_str == null)
            {
                //MessageBox.Show("the object: " + obj.GetType().ToString() + "was not seralised");
            }
            return serialised_str;
        }

    }
}
