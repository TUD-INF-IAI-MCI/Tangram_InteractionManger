using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace tud.mci.tangram.TangramLector.Control
{
    public class BrailleKeyboard
    {
        CtiFileLoader fl = new CtiFileLoader();
        private Dictionary<String, String> charToDotsList = new Dictionary<String, String>();
        private Dictionary<String, String> dotsToCharList = new Dictionary<String, String>();

        public BrailleKeyboard(String tablePath)
        {
            try
            {
                // load the basic Unicode braille chars
                string baseDir = GetCurrentDllDirectory();
                bool baseSuccess = fl.LoadFile(baseDir + @"\config\tables\unicode.dis");

                Logger.Instance.Log(LogPriority.MIDDLE, this, "Try to load braille keyboard config from " + (baseDir + @"\config\tables\unicode.dis") + ". " + (baseSuccess ? "" : "NOT ") + "successful");

                bool sucess = fl.LoadFile(tablePath);
                if (sucess)
                {
                    charToDotsList = fl.CharToDotsList;
                    dotsToCharList = fl.DotsToCharList;
                }
            }
            catch (ArgumentException ex)
            {
                Logger.Instance.Log(LogPriority.MIDDLE, this, "[FATAL ERROR] Can't load braille table", ex);
            }
        }

        public static string GetCurrentDllPath()
        {
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return path;
        }

        public static string GetCurrentDllDirectory()
        {
            string path = GetCurrentDllPath();
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Gets the Braille dots for char.
        /// </summary>
        /// <param name="c">The char to translate e.g. 'g'.</param>
        /// <returns>the dot pattren as a sorted string of raised dot-positions e.g. '1245'</returns>
        public String GetDotsForChar(String c)
        {
            return charToDotsList.ContainsKey(c) ? charToDotsList[c] : null;
        }

        /// <summary>
        /// Gets the Braille dots for char.
        /// </summary>
        /// <param name="c">The char to translate e.g. 'g'.</param>
        /// <returns>the dot pattern as a sorted string of raised dot-positions e.g. '1245'</returns>
        public String GetDotsForChar(char c)
        {
            return GetDotsForChar(c.ToString());
        }

        /// <summary>
        /// Gets the corresponding char for a defined eight dot pattern.
        /// </summary>
        /// <param name="dots">The eight dot pattern as a sorted string e.g. '12345678'.</param>
        /// <returns>the corresponding char from the loaded table</returns>
        public String GetCharFromDots(string dots)
        {
            return dotsToCharList.ContainsKey(dots) ? dotsToCharList[dots] : null;
        }
    }

    /// <summary>
    /// Class for loading an interpreting braille translation tables 
    /// based on the definitions of the 'liblouis' project [https://github.com/liblouis]. 
    /// </summary>
    class CtiFileLoader
    {
        private Dictionary<String, String> charToDotsList = new Dictionary<String, String>();
        private Dictionary<String, String> dotsToCharList = new Dictionary<String, String>();

        /// <summary>
        /// Gets the char to dots list. A dictionary which contains a mapping from chars to a 
        /// dot pattern as a sorted string of raised Braille dots e.g. '1245'.
        /// The key is the char to translate e.g. 'g', the value is the corresponding Braille dot pattern e.g. '1245'.
        /// </summary>
        /// <value>The char to dots list.</value>
        public Dictionary<String, String> CharToDotsList { get { return charToDotsList; } }
        /// <summary>
        /// Gets the dots to char list. A dictionary which contains a mapping from dot pattern 
        /// as a sorted string of raised Braille dots e.g. '1245' to a character
        /// The key is the  Braille dot pattern e.g. '1245' and the value is the corresponding character e.g. 'g'.
        /// </summary>
        /// <value>The dots to char list.</value>
        public Dictionary<String, String> DotsToCharList { get { return dotsToCharList; } }

        private String parentPath = "";

        /// <summary>
        /// Loads a Braille translation table file. 
        /// Based on the translation definitions of the 'liblouis' project [https://github.com/liblouis]
        /// You can load as much files as you want. 
        /// Double mappings of dot pattern will be overwritten by the last loaded definition.
        /// </summary>
        /// <param name="path">The path to the translation table file to load.</param>
        /// <returns><c>true</c> if the file could be loaded and translated into mapping dictionaries.</returns>
        public bool LoadFile(String path)
        {
            if (File.Exists(path))
            {
                FileInfo fi = new FileInfo(path);
                parentPath = fi.Directory.FullName;

                string line;

                // Read the file and display it line by line.
                using (System.IO.StreamReader file = new System.IO.StreamReader(path))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        if (String.IsNullOrWhiteSpace(line)) continue;
                        line = line.TrimStart();
                        if (line.StartsWith("#")) continue;

                        splitLine(line);
                    }
                    file.Close();
                }
            }
            else throw new ArgumentException("Table file '" + path + "' does not exist!");
            return true;
        }

        private void splitLine(String line)
        {
            if (!String.IsNullOrWhiteSpace(line))
            {
                string pattern = @"\s+";            // Split on hyphens
                string[] parts = Regex.Split(line, pattern);
                //System.Diagnostics.Debug.WriteLine("\tparts: " + string.Join(" | ", parts));
                if (parts.Length > 1)
                {
                    SignType sType;
                    try
                    {
                        sType = (SignType)Enum.Parse(typeof(SignType), parts[0]);
                        if (!(Enum.IsDefined(typeof(SignType), sType) | sType.ToString().Contains(",")))
                            sType = SignType.none;
                    }
                    catch (ArgumentException)
                    {
                        sType = SignType.none;
                    }

                    if (sType == SignType.none || sType == SignType.space) return;
                    if (sType == SignType.include)
                    {
                        LoadFile(parentPath + "\\" + parts[1]);
                    }
                    else
                    {
                        if (parts.Length > 2)
                        {
                            if (parts[1].StartsWith(@"\") && parts[1].Length > 2) // get Unicode Hex definitions but leave the backslash as a char
                            {
                                parts[1] = GetCharFromUnicodeHex(parts[1]);
                            }

                            try
                            {
                                if (CharToDotsList.ContainsKey(parts[1])) { CharToDotsList[parts[1]] = parts[2]; }
                                else { charToDotsList.Add(parts[1], parts[2]); }

                                if (DotsToCharList.ContainsKey(parts[2])) { dotsToCharList[parts[2]] = parts[1]; }
                                else { dotsToCharList.Add(parts[2], parts[1]); }
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the char from Unicode hexadecimal string.
        /// </summary>
        /// <param name="characterCode">The character code e.g. '\x2800'.</param>
        /// <returns>the current available Unicode character if available e.g. ' '</returns>
        public static string GetCharFromUnicodeHex(String characterCode)
        {
            if (!String.IsNullOrEmpty(characterCode))
            {
                if (characterCode.StartsWith(@"\"))
                {
                    characterCode = characterCode.Substring(1);
                }
                if (characterCode.StartsWith("x"))
                {
                    characterCode = characterCode.Substring(1);
                }

                int number;
                bool success = Int32.TryParse(characterCode, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out number);

                if (success)
                {
                    return GetCharFromUnicodeInt(number);
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// try to parse a char from Unicode int.
        /// </summary>
        /// <param name="number">The number code e.g. 10241.</param>
        /// <returns>the char of the given value e.g. ' '</returns>
        public static string GetCharFromUnicodeInt(int number)
        {
            try
            {
                char c2 = (char)number;
                return c2.ToString();
            }
            catch { }
            return String.Empty;
        }

        enum SignType
        {
            none,
            space,
            punctuation,
            sign,
            math,
            include,
            uppercase,
            lowercase,
            digit,
            display
        }
    }
}