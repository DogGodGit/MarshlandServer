using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using XnaGeometry;
using System.Net;

namespace MainServer
{
    static class Utilities
    {
        public static double Difference2DSquared(Vector3 vec1, Vector3 vec2)
        {
            Vector3 vec3 = vec1 - vec2;
            return vec3.X * vec3.X + vec3.Z * vec3.Z;
        }
        public static float Difference2D(Vector3 vec1, Vector3 vec2)
        {
            return (float)Math.Sqrt(Difference2DSquared(vec1,vec2));
        }

        public static string baToHex(byte[] ba)
        {
            StringBuilder sb = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }

        public static string hashString(string s)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            s = "S6D34dFb!" + s;
            string outstr = baToHex(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s))).ToUpper();
            return outstr;
        }
        
        static internal Vector3 GetDirectionFromYAngle(float yAngle)
        {
        
            float dirx = (float)Math.Sin(((yAngle) / 180.0f) * Math.PI);
            float dirz = (float)Math.Cos(((yAngle) / 180.0f )* Math.PI);
            Vector3 direction = new Vector3(dirx, 0, dirz);
            //emergency correction if direction cannot be normalised
            if (direction.LengthSquared() == 0)
            {
                direction = new Vector3(0, 0, -1);
            }
            else
            {
                direction.Normalize();
            }

            return direction;

        }

        static internal float GetYAngleFromDirection(Vector3 direction)
        {
            float yAngle = 0;
            
            if (direction.Z == 0)
            {
                //if directing x&z ==0 then use the 0 as default value
                if (direction.X > 0)
                {
                    yAngle = 270;
                }
                else if(direction.X>0)
                {
                    yAngle = 90;
                }
            }
            else
            {
                //the value in radians 
                float radValue = (float)Math.Atan2(-direction.X , -direction.Z);//(float)Math.Atan(-direction.X / -direction.Z);

                yAngle = (float)((radValue/Math.PI)*180);
            }
           
            return yAngle;
        }

        public static string GetFormatedDateTimeString(DateTime dateTime)
        {
			// Change DateTime format for localize.
			return dateTime.ToString("dd/MM/yyyy HH:mm:ss");
			/*
            int day = dateTime.Day;

            int tens = day / 10;    // Gets the number of tens in the date.
            int unit = day % 10;    // Gets the number of units in the date.

            string formattedDay = String.Empty;

            if (tens == 1)
            {
                formattedDay = day.ToString() + "th";
            }
            else
            {
                switch (unit)
                {
                    case 1:
                        formattedDay = day.ToString() + "st";
                        break;

                    case 2:
                        formattedDay = day.ToString() + "nd";
                        break;

                    case 3:
                        formattedDay = day.ToString() + "rd";
                        break;

                    default:
                        formattedDay = day.ToString() + "th";
                        break;
                }
            }

            return dateTime.ToString("H : mm") + " GMT, on the " + formattedDay + " of " + dateTime.ToString("MMMM, yyyy");
			*/
		}

		internal static HttpWebRequest CreatePatchHttpRequest(string in_urlStr)
		{
			string patchAddress = System.Configuration.ConfigurationManager.AppSettings["PatchserverAddress"];
			
			string patchUrl = string.Format("{0}{1}{2}", (patchAddress.StartsWith("http://") ? "" : "http://"), patchAddress, in_urlStr);

			Program.DisplayDelayed("Patch request: " + patchUrl);

			var request = (HttpWebRequest)WebRequest.Create(patchUrl);
			request.Timeout = 5000;
			request.ReadWriteTimeout = 5000;
			return request;
		}
    }
}
