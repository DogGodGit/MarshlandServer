using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace MainServer.Localise
{
	public class LocalisedTextDBLoader
	{
		private string[] lineSeparator = new string[] { "\r\n" };
		private char[] fieldSeparator = new char[] { ',' };

		// for use in string.Format()
		// {variableName + number} => {number}
		// ex. {playerName0} => {0}
		private string pattern = @"(\{)([a-zA-Z]+)(\d+)(\})";
		private string replacement = "{$3}";
		private Regex rgx;

		// index in input CSV file
		private Dictionary<string, int> csvLanguageIndex;
		// index in output CS file
		private Dictionary<string, int> dbLanguageIndex;

		private class TextEntry
		{
			public string className;
			public string textName;
			public string[] texts;
			public string[] formattedTexts;
		}

		// use nested list instead of dictionary to preserved order in CSV
		private List<List<TextEntry>> classList;

		public static bool LoadHardCodeLocalisedTextDB(string filename, out string[] languageIndex, out Dictionary<string, int> csvLanguageIndex, out List<LocalisedTextDB> textDBList)
		{
			textDBList = null;
			languageIndex = null;
			csvLanguageIndex = null;

			LocalisedTextDBLoader loader = new LocalisedTextDBLoader();
			if (!loader.ParseFile(filename))
			{
				return false;
			}

			languageIndex = loader.dbLanguageIndex.Keys.ToArray();
			csvLanguageIndex = loader.csvLanguageIndex;

			textDBList = new List<LocalisedTextDB>();

			List<List<TextEntry>> classList = loader.classList;
			for (int i = 0; i < classList.Count; ++i)
			{
				List<TextEntry> teList = classList[i];

				if (teList.Count == 0)
				{
					// empty DB?
					continue;
				}

				LocalisedTextDB tdb = new LocalisedTextDB();
				tdb.index = i;
				tdb.className = teList[0].className;

				for (int j = 0; j < teList.Count; ++j)
				{
					TextEntry te = teList[j];
					tdb.textNameDB.Add(te.textName, te.formattedTexts);
				}

				textDBList.Add(tdb);
			}

			return true;
		}

		public static bool LoadLocalisedTextDB(string filename, Dictionary<string, int> csvLanguageIndex, out List<LocalisedTextDB> textDBList)
		{
			textDBList = null;

			LocalisedTextDBLoader loader = new LocalisedTextDBLoader();
			if (!loader.ParseFile(filename))
			{
				return false;
			}

			// check csvLanguageIndex
			bool hasError = false;
			foreach (var csvIdx in csvLanguageIndex)
			{
				if (!loader.csvLanguageIndex.ContainsKey(csvIdx.Key))
				{
					// dosen't has key
					hasError = true;
					break;
				}
				else
				{
					if (loader.csvLanguageIndex[csvIdx.Key] != csvIdx.Value)
					{
						// has key but value not match
						hasError = true;
						break;
					}
				}
			}

			if (hasError)
			{
				return false;
			}

			textDBList = new List<LocalisedTextDB>();

			List<List<TextEntry>> classList = loader.classList;
			for (int i = 0; i < classList.Count; ++i)
			{
				List<TextEntry> teList = classList[i];

				if (teList.Count == 0)
				{
					// empty DB?
					continue;
				}

				LocalisedTextDB tdb = new LocalisedTextDB();
				tdb.index = i;
				tdb.className = teList[0].className;

				for (int j = 0; j < teList.Count; ++j)
				{
					TextEntry te = teList[j];
					tdb.textNameDB.Add(te.textName, te.formattedTexts);
					tdb.textIDDB.Add(Convert.ToInt32(te.textName), te.formattedTexts);
				}

				textDBList.Add(tdb);
			}

			return true;
		}

		public LocalisedTextDBLoader()
		{
			rgx = new Regex(pattern);
		}

		private string[] GetLineContents(TextFieldParser parser)
		{
			while (!parser.EndOfData)
			{
				string[] fields = parser.ReadFields();

				if (fields == null)
					continue;

				if (fields.Length == 0)
					continue;

				bool isEmptyLine = true;
				for (int j = 0; j < fields.Length; ++j)
				{
					if (fields[j] != "")
					{
						// some fields is not null or whitespace so this line isn't empty
						isEmptyLine = false;
					}
				}

				if (isEmptyLine)
					continue;

				return fields;
			}

			return null;
		}

		private bool ParseHeader(string[] fields)
		{
			// contents less than expected (default header + at lease 1 language)
			if (fields.Length < 4)
				return false;

			if (fields[0] != "ClassName" && fields[0] != "TableName")
				return false;

			if (fields[1] != "TextID" && fields[1] != "ID")
				return false;

			csvLanguageIndex = new Dictionary<string, int>();
			dbLanguageIndex = new Dictionary<string, int>();

			// ClassName, TextID, ..., ..., ..., Comment
			for (int i = 2; i < fields.Length; ++i)
			{
				if (fields[i] == "Comment")
				{
					// parse until find comment field
					continue;
				}

				if (csvLanguageIndex.ContainsKey(fields[i]))
				{
					Console.WriteLine("Error parsing header, duplicated language header.");
					return false;
				}

				csvLanguageIndex.Add(fields[i], i);
				dbLanguageIndex.Add(fields[i], dbLanguageIndex.Count);
			}

			return true;
		}

		private TextEntry ParseTextEntry(string[] fields, string currentClassName)
		{
			TextEntry te = new TextEntry();
			te.className = currentClassName;
			te.textName = fields[1];

			// read each language
			te.texts = new string[dbLanguageIndex.Count];
			te.formattedTexts = new string[dbLanguageIndex.Count];

			int langIdx = 0;
			foreach (var langs in csvLanguageIndex)
			{
				langIdx = dbLanguageIndex[langs.Key];
				te.texts[langIdx] = fields[langs.Value];
				te.formattedTexts[langIdx] = rgx.Replace(fields[langs.Value], replacement);
				te.formattedTexts[langIdx] = te.formattedTexts[langIdx].Replace("\\n", "\n");
			}

			return te;
		}

		private bool ParseFile(string filename)
		{
			if (!File.Exists(filename))
			{
				Console.WriteLine("Error : File not found + [" + filename + "]");
				return false;
			}

			using (TextFieldParser parser = new TextFieldParser(filename))
			{
				parser.TextFieldType = FieldType.Delimited;
				parser.SetDelimiters(",");

				string[] fields = GetLineContents(parser);

				// read header, assume that header will be on the first line that has contents
				if (fields == null)
				{
					// no header to parse :(
					return false;
				}

				if (!ParseHeader(fields))
					return false;

				classList = new List<List<TextEntry>>();

				List<TextEntry> currentList = null;

				string currentClassName = "";

				fields = GetLineContents(parser);
				while (fields != null)
				{
					if (fields[0] != "")
					{
						currentClassName = fields[0];
						currentList = new List<TextEntry>();
						classList.Add(currentList);
					}

					if (currentClassName == "")
					{
						// still no ClassName :(
						return false;
					}

					TextEntry te = ParseTextEntry(fields, currentClassName);
					if (te == null)
						return false;

					currentList.Add(te);

					fields = GetLineContents(parser);
				}

				if (classList.Count == 0)
					return false;
			}

			return true;
		}
	}
}
