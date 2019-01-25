using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace tud.mci.tangram.TangramLector.Button2FunctionMapping
{
    /// <summary>
    /// Interprets an set of device related button/key-combination to function-name mappings 
    /// as XML and return them as a searchable Dictionary.
    /// </summary>
    public class ButtonMappingLoader
    {
        #region members

        static readonly IComparer<string> buttonComparer = new ButtonSortingComparer();

        #endregion 

        #region Schema

        static XmlSchema _mySchema;
        static XmlSchema getValidtaionSchema()
        {

            if (_mySchema == null)
            {
                System.Text.Encoding encode = System.Text.Encoding.UTF8;
                MemoryStream ms = new MemoryStream(encode.GetBytes(Properties.Resources.FunctionMappingXSD));
                _mySchema = XmlSchema.Read(ms, DTDValidation);
            }
            return _mySchema;
        }

        static readonly System.Xml.Schema.ValidationEventHandler DTDValidation = new System.Xml.Schema.ValidationEventHandler(booksSettingsValidationEventHandler);
        static void booksSettingsValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e)
        {
            if (e.Severity == System.Xml.Schema.XmlSeverityType.Warning)
            {
                Console.Write("WARNING: ");
                Console.WriteLine(e.Message);
            }
            else if (e.Severity == System.Xml.Schema.XmlSeverityType.Error)
            {
                Console.Write("ERROR: ");
                Console.WriteLine(e.Message);
                throw new ArgumentException("XML validation Exception:\r\n" + e.Message);
            }
        }

        #endregion

        public ButtonMappingLoader()
        {

        }

        #region Mapping Loading

        /// <summary>Interprets the mapping MXL definitions and returns it as a device-related dictionary
        /// of button-combinations to function.</summary>
        /// <param name="mappingXML">the content string of the mapping XML (file)</param>
        /// <param name="funcDevOverrideList">A list of functions that has overrides for special adapters or devices.
        /// (Dictionary[key=functionName, value=list of adapters/devices overriding the default definition])
        /// </param>
        /// <returns>Dictionary of mapping dictionaries
        /// (Dictionary[key=AdapterTypeName,
        /// value=Dictionary[key=buttonCombinationString, value=list of function names])</returns>
        public Dictionary<string, Dictionary<string, List<string>>> LoadMapping(
            string mappingXML, out Dictionary<string, List<string>> funcDevOverrideList)
        {
            funcDevOverrideList = new Dictionary<string, List<string>>();
            if (string.IsNullOrWhiteSpace(mappingXML)) return null;

            try
            {
                // build an XML structure of the mapping file content string
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(mappingXML);

                // validation
                try
                {
                    doc.Schemas.Add(getValidtaionSchema());
                    doc.Schemas.Compile();
                    doc.Validate(DTDValidation);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error in function mapping definition XML file validation:\r\n" + ex);
                }

                // build the mappings
                // dictionary containing the different mapping dictionaries for the different devices
                Dictionary<string, Dictionary<string, List<string>>> mappings = new Dictionary<string, Dictionary<string, List<string>>>
                {
                    // add the default mapping dictionary (key = "")
                    { string.Empty, new Dictionary<string, List<string>>() }
                };

                // load, build and store the different mappings
                if (doc != null && doc.HasChildNodes && doc.DocumentElement != null)
                {
                    foreach (XmlNode functionNode in doc.DocumentElement)
                    {
                        if (functionNode != null && functionNode.Name.Equals("function"))
                        {
                            var functionNameAttr = functionNode.Attributes["name"];
                            string name = functionNameAttr == null ? String.Empty : functionNameAttr.Value;
                            // check if an additional help exists
                            // NOTICE: not used yet
                            var functionDescAttr = functionNode.Attributes["description"];
                            string descr = functionDescAttr == null ? String.Empty : functionDescAttr.Value;

                            // get mappings for this function for the different devices
                            if (functionNode.HasChildNodes)
                            {
                                foreach (XmlNode mappingNode in functionNode.ChildNodes)
                                {
                                    try
                                    {
                                        if (mappingNode != null && mappingNode.Name.Equals("mapping"))
                                        {
                                            // get the related device for this mapping 
                                            var deviceAttr = mappingNode.Attributes["device"];
                                            string device = deviceAttr == null ? String.Empty : deviceAttr.Value.Trim();
                                            // replace the '_default' marker with the "" key
                                            if (device.ToLower().Equals("_default")) device = String.Empty;

                                            if (device != String.Empty)
                                            {
                                                // add function to the function def list for the devices
                                                if (funcDevOverrideList.ContainsKey(name)) { funcDevOverrideList[name].Add(device); }
                                                else { funcDevOverrideList.Add(name, new List<string>(1) { device }); }
                                            }

                                            // load the list to add to
                                            Dictionary<string, List<string>> deviceFunctionMappingDict = null;
                                            if (mappings.ContainsKey(device))
                                                deviceFunctionMappingDict = mappings[device];
                                            else
                                            {
                                                deviceFunctionMappingDict = new Dictionary<string, List<string>>();
                                                mappings.Add(device, deviceFunctionMappingDict);
                                            }

                                            // check if an additional priority exists
                                            // NOTICE: not used yet
                                            var priorityAttr = mappingNode.Attributes["priority"];
                                            int priority = priorityAttr == null ? 0 : Convert.ToInt32(priorityAttr.Value);

                                            // get the button combination
                                            var buttonString = mappingNode.InnerText.ToLower();
                                            buttonString = Regex.Replace(buttonString, @"\s+", string.Empty);

                                            // validate the definition
                                            var buttons = new List<string>();
                                            foreach (var btn in buttonString.Split(','))
                                            {
                                                var _btn = StyleButtonName(btn);
                                                if (!String.IsNullOrEmpty(_btn) && !buttons.Contains(_btn))
                                                    buttons.Add(_btn);
                                            }
                                            buttons.Sort(buttonComparer);
                                            // clear from "None" and "Unknown" ?
                                            buttonString = String.Join(",", buttons);

                                            // add mapping to list
                                            if (deviceFunctionMappingDict.ContainsKey(buttonString))
                                            {
                                                deviceFunctionMappingDict[buttonString].Add(name);
                                            }
                                            else
                                            {
                                                deviceFunctionMappingDict.Add(buttonString, new List<String>(1) { name });
                                            }
                                            mappings[device] = deviceFunctionMappingDict;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
                return mappings;
            }
            catch (Exception ex)
            { }
            return null;
        }

        #endregion

        #region Utils

        static readonly Regex addFn = new Regex(@"^fn([1-9][0-5]|[1-9])(_[1-9]\d*)?$"); // e.g. fn12, fn15_1, fn2_12345
        static readonly Regex kbBtn = new Regex("^k[1-8]$");
        static readonly Regex kbFnBtn = new Regex("^f(1|11|2|22)$");

        static string StyleButtonName(string btnString)
        {
            if (!String.IsNullOrWhiteSpace(btnString))
            {
                // check and style button/key ID naming corresponding to the related enum values
                btnString = btnString.Trim().ToLower();

                switch (btnString)
                {
                    case "none": return "None";
                    case "unknown": return "Unknown";
                    case "enter": return "Enter";
                    case "abort": return "Abort";
                    case "gesture": return "Gesture";
                    case "left": return "Left";
                    case "right": return "Right";
                    case "up": return "Up";
                    case "down": return "Down";
                    case "zoomin": return "ZoomIn";
                    case "zoomout": return "ZoomOut";
                    default:

                        if (kbBtn.IsMatch(btnString)) return btnString; // BrailleIO_BrailleKeyboardButton (points)
                        if (kbFnBtn.IsMatch(btnString)) return firstLetterToUpperCase(btnString); // BrailleIO_BrailleKeyboardButton (functions)

                        if (addFn.IsMatch(btnString)) return btnString; // BrailleIO_AdditionalButton
                        break;
                }

                // return btnString;
            }
            return String.Empty;
        }

        /// <summary>Returns the input string with the first character converted to
        /// uppercase, or mutates any nulls passed into string.Empty</summary>
        /// <param name="s">The s.</param>
        /// <returns>System.String.</returns>
        static string firstLetterToUpperCase(string s)
        {
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        /// <summary>
        /// Combines two dictionaries
        /// </summary>
        /// <param name="first">the first (and basic) dictionary. Is the one that will be updated (overwritten) by the second.</param>
        /// <param name="second">the second dictionary, extend or update the first dictionary</param>
        /// <returns>The first dictionary extended or updated by the second dictionary</returns>
        public static Dictionary<String, List<String>> CombineDictionaries(Dictionary<String, List<String>> first, Dictionary<String, List<String>> second)
        {
            Dictionary<String, List<String>> dict = first != null ? first :
                second != null ? second : new Dictionary<String, List<String>>();

            if (first != null && second != null)
            {
                foreach (var item in second)
                {
                    if (dict.ContainsKey(item.Key))
                    {
                        dict[item.Key] = item.Value.Union(dict[item.Key]).ToList();
                    }
                    else
                    {
                        dict.Add(item.Key, item.Value);
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Combines two dictionaries
        /// </summary>
        /// <param name="first">the first (and basic) dictionary. Is the one that will be updated (overwritten) by the second.</param>
        /// <param name="second">the second dictionary, extend or update the first dictionary</param>
        /// <returns>The first dictionary extended or updated by the second dictionary</returns>
        public static Dictionary<String, Dictionary<String, List<String>>> CombineDictionaries(Dictionary<String, Dictionary<String, List<String>>> first, Dictionary<String, Dictionary<String, List<String>>> second)
        {
            Dictionary<String, Dictionary<String, List<String>>> dict = first != null ? first :
                second != null ? second : new Dictionary<String, Dictionary<String, List<String>>>();

            if (first != null && second != null)
            {
                foreach (var locale in second)
                {
                    if (dict.ContainsKey(locale.Key))
                    {
                        dict[locale.Key] = CombineDictionaries(dict[locale.Key], locale.Value);
                    }
                    else
                    {
                        dict.Add(locale.Key, locale.Value);
                    }
                }
            }

            return dict;
        }

        #endregion

    }

    /// <summary>
    /// Implements a comparer for sorting button IDs (general, keyboard, additional, {[i_]additional} )
    /// Implements the <see cref="System.Collections.Generic.IComparer{System.String}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{System.String}" />
    public class ButtonSortingComparer : IComparer<string>
    {
        public int Compare(object x, object y)
        {
            if (x is string && y is string)
                return Compare(x.ToString(), y.ToString());
            return 0;
        }

        public int Compare(string x, string y)
        {
            var val1 = getValueForButton(x);
            var val2 = getValueForButton(y);
            return  val1 - val2;
        }

        static int getValueForButton(string btn)
        {
            // check if function Button
            switch (btn)
            {
                #region general
                case "":
                case "None":
                    return -1000000000;
                case "Unknown":
                    return -100000000;
                case "Enter":
                    return 0;
                case "Abort":
                    return 1;
                case "Gesture":
                    return 2;
                case "Left":
                    return 3;
                case "Right":
                    return 4;
                case "Up":
                    return 5;
                case "Down":
                    return 6;
                case "ZoomIn":
                    return 7;
                case "ZoomOut":
                    return 8;
                #endregion
                #region Braille keyboard
                case "k1":
                    return 9;
                case "k2":
                    return 10;
                case "k3":
                    return 11;
                case "k4":
                    return 12;
                case "k5":
                    return 13;
                case "k6":
                    return 14;
                case "k7":
                    return 15;
                case "k8":
                    return 16;
                case "F1":
                    return 17;
                case "F2":
                    return 18;
                case "F11":
                    return 19;
                case "F22":
                    return 20;
                #endregion
                #region Additional Buttons (0) 
                case "fn1":
                    return 21;
                case "fn2":
                    return 22;
                case "fn3":
                    return 23;
                case "fn4":
                    return 24;
                case "fn5":
                    return 25;
                case "fn6":
                    return 26;
                case "fn7":
                    return 27;
                case "fn8":
                    return 28;
                case "fn9":
                    return 29;
                case "fn10":
                    return 30;
                case "fn11":
                    return 31;
                case "fn12":
                    return 32;
                case "fn13":
                    return 33;
                case "fn14":
                    return 34;
                case "fn15":
                    return 35;
                #endregion
                default:

                    // check if an new additional button?!
                    if (btn.Length > 4 && Char.IsDigit(btn.Last())) // if another addition button ({d}_fn{d})
                    {
                        // split into button name and index
                        var split = btn.Split('_');
                        int i = -1;
                        if (split.Length == 2 && Int32.TryParse(split[1], out i) && i > 0)
                        {
                            int val = getValueForButton(split[0]);
                            if (val > 0) return i * 40 + val;
                        }
                    }
                    break;
            }
            return 0;
        }
    }



}
