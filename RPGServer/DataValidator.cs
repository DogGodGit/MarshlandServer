using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MainServer
{
	class DataValidator
	{
		public static readonly string NON_SYMBOL_FILTER = "[^a-zA-Z0-9 ]";
		public static readonly string NON_SYMBOL_NON_SPACE_FILTER = "[^a-zA-Z0-9]";
		public static readonly string USERNAME_FILTER = "[^a-zA-Z0-9-@._]";

		public static readonly int MIN_USERNAME_LENGTH = 3;
		public static readonly int MIN_PASSWORD_LENGTH = 5;


		public static readonly int CHARACTER_NAME_LIMIT = 15;
		public static readonly string CHARACTER_NAME_FILTER = "[^a-zA-Z0-9ก-๛ ]";

		public static readonly float CHARACTER_SIZE_MIN = 0.9f;
		public static readonly float CHARACTER_SIZE_MAX = 1.1f;

		

		



		public static bool CheckNonSymbolString(ref string str)
		{
			string filteredStr = Regex.Replace(str, NON_SYMBOL_FILTER, String.Empty);

			if (str != filteredStr)
			{
				Program.DisplayDelayed("[X]CheckNonSymbolString : " + str);
				str = filteredStr;
				return false;
			}

			return true;
		}

		public static bool JustCheckUserName(string name)
		{
			string filteredName = Regex.Replace(name, USERNAME_FILTER, String.Empty);

			if (name != filteredName)
			{
				Program.DisplayDelayed("[X]JustCheckUserName : " + name);
				return false;
			}

			return true;
		}

		// same check as username, but separated in case that it needs more check
		// using MD5
		public static bool CheckHashPassword(ref string pass)
		{
			string filteredName = Regex.Replace(pass, NON_SYMBOL_NON_SPACE_FILTER, String.Empty);

			if (pass != filteredName)
			{
				Program.DisplayDelayed("[X]CheckHashPassword : " + pass);
				pass = filteredName;
				return false;
			}

			return true;
		}


		public static bool JustCheckCharacterName(string name)
		{
			bool changed = false;
			string outName = name;

			outName.Trim();
			if (outName != name)
			{
				changed = true;
			}

			if (outName.Length == 0)
			{
				changed = true;

				// early out
				return !changed;
			}

			// check length
			if (outName.Length > CHARACTER_NAME_LIMIT)
			{
				outName = outName.Substring(0, CHARACTER_NAME_LIMIT);
				changed = true;
			}

			// filter character
			string filteredName = Regex.Replace(outName, CHARACTER_NAME_FILTER, String.Empty);
			if (filteredName != outName)
			{
				outName = filteredName;
				changed = true;
			}

			// check spaces, character name allow 1 space only
			// preserve only 1st space
			string[] splitted = outName.Split(' ');
			if (splitted.Length > 2)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(splitted[0]);
				sb.Append(" ");

				for (int i = 1; i < splitted.Length; ++i)
				{
					sb.Append(splitted[i]);
				}

				outName = sb.ToString();
				changed = true;
			}

			if (changed)
			{
				Program.DisplayDelayed("[X]CheckCharacterName : " + name);
			}

			return !changed;
		}

		public static bool CheckRace_Id(ref int race_id)
		{
			if (race_id != 1)
			{
				race_id = 1;
				return false;
			}

			return true;
		}

		public static bool CheckModel_Scale(ref float model_scale)
		{
			if (model_scale < CHARACTER_SIZE_MIN)
			{
				model_scale = CHARACTER_SIZE_MIN;
				return false;
			}

			if (model_scale > CHARACTER_SIZE_MAX)
			{
				model_scale = CHARACTER_SIZE_MAX;
				return false;
			}

			return true;
		}

		public static bool CheckGender(ref GENDER gender)
		{
			if (gender < GENDER.MALE)
			{
				gender = GENDER.MALE;
				return false;
			}

			if (gender > GENDER.FEMALE)
			{
				gender = GENDER.FEMALE;
				return false;
			}

			return true;
		}

		public static bool CheckClass_Id(ref int class_id)
		{
			if (class_id < (int)CLASS_TYPE.WARRIOR)
			{
				class_id = (int)CLASS_TYPE.WARRIOR;
				return false;
			}

			if (class_id > (int)CLASS_TYPE.ROGUE)
			{
				class_id = (int)CLASS_TYPE.ROGUE;
				return false;
			}

			return true;
		}














	}
}
