using BrailleIO;
using BrailleIO.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tud.mci.tangram.TangramLector.Button2FunctionMapping;

namespace tud.mci.tangram.TangramLector.Interaction_Manager
{
    /// <summary>
    /// Interface for providing device-related button/key-combination to function-name mappings.
    /// </summary>
    interface IButton2FunctionProxy
    {
        #region definition mappings

        /// <summary>Gets the function mappings for the given adapter.</summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> GetFunctionMappingsForAdapter(IBrailleIOAdapter adapter);
        /// <summary>Gets the function mappings for the given device.</summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> GetFunctionMappingsForAdapter(BrailleIODevice device);
        /// <summary>Gets the function mappings for the given adapter type.</summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> GetFunctionMappingsForAdapter(Type adapterType);
        /// <summary>Gets the function mappings for the given adapter type name (e.g. "BrailleIO.ShowOffAdapter".</summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> GetFunctionMappingsForAdapter(string adapterTypeName);
        /// <summary>Gets the default function mappings if exist.</summary>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> GetDefaultFunctionsMappings();

        #endregion

        #region mapping requests

        /// <summary>Gets the function mapping for the given button-combination by the adapter.</summary>
        /// <param name="adapter">The sending adapter.</param>
        /// <param name="buttonCombination">The button combination.</param>
        /// <returns>The found function-name mapping, if exist; otherwise the empty string is returned.</returns>
        List<string> GetFunctionMapping(IBrailleIOAdapter adapter, string buttonCombination);
        /// <summary>Gets the function mapping for the given button-combination by the adapter.</summary>
        /// <param name="adapter">The sending device.</param>
        /// <param name="buttonCombination">The button combination.</param>
        /// <returns>The found function-name mapping, if exist; otherwise the empty string is returned.</returns>
        List<string> GetFunctionMapping(BrailleIODevice device, string buttonCombination);
        /// <summary>Gets the function mapping for the given button-combination by the adapter.</summary>
        /// <param name="adapter">The related adapter-type.</param>
        /// <param name="buttonCombination">The button combination.</param>
        /// <returns>The found function-name mapping, if exist; otherwise the empty string is returned.</returns>
        List<string> GetFunctionMapping(Type adapterType, string buttonCombination);
        /// <summary>Gets the function mapping for the given button-combination by the adapter.</summary>
        /// <param name="adapter">The sending adapter's type name.</param>
        /// <param name="buttonCombination">The button combination.</param>
        /// <returns>The found function-name mapping, if exist; otherwise the empty string is returned.</returns>
        List<string> GetFunctionMapping(string adapterTypeName, string buttonCombination);
        /// <summary>Gets the default function mapping for the given button-combination, if exist.</summary>
        /// <param name="buttonCombination">The button combination.</param>
        /// <returns>The found function-name mapping, if exist; otherwise the empty string is returned.</returns>
        List<string> GetDefaultFunctionMapping(string buttonCombination);

        #endregion

        /// <summary>Loads the function mappings from a given XML string.</summary>
        /// <param name="mappingXML">The mapping XML content string.</param>
        /// <param name="combine">if set to <c>true</c> the definition will be included into the 
        /// existing mappings (duplicates will be overwritten); otherwise the whole mapping will be replaced (dangerous!!).</param>
        /// <returns>
        ///   <c>true</c> if the mapping was loaded successfully, <c>false</c> otherwise.</returns>
        bool LoadFunctionMapping(string mappingXML, bool combine = true);

    }

    /// <summary>
    /// Basic abstract implementation for a generic mapper of device-related 
    /// button/key combination codes to functions to be called.
    /// </summary>
    public abstract class AbstarctButton2FunctionProxyBase : IButton2FunctionProxy
    {
        /// <summary>
        /// Dictionary combining the specialized lists for adapter-type names (key) 
        /// with their key-combination to function mapping.
        /// </summary>
        protected Dictionary<string, Dictionary<string, List<string>>> combinationToFunctionDic = new Dictionary<string, Dictionary<string, List<string>>>();

        /// <summary>
        /// Dictionary of defined function names and if a certain device or adapter type is overwriting it.
        /// </summary>
        protected Dictionary<string, List<string>> functionToDeviceDefinitionDic = new Dictionary<string, List<string>>();

        /// <summary>
        /// The caching table for function mappings
        /// </summary>
        protected readonly ConcurrentDictionary<int, Dictionary<string, List<string>>> _cachingTable = new ConcurrentDictionary<int, Dictionary<string, List<string>>>();


        private ReaderWriterLockSlim mappingLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The mapping loader used to interpret the mapping definition XMl files. 
        /// </summary>
        protected readonly ButtonMappingLoader mappingLoader = new ButtonMappingLoader();

        ///// <summary>Occurs when a specific function should be called after a user interaction.</summary>
        ///// <remarks>If the function was handled, set the <see cref="FunctionCallInteractionEventArgs.Handled"/> 
        ///// flag in the event args to <c>true</c>. 
        ///// By doing so, the <see cref="IInteractionEventProxy.ButtonCombinationReleased"/> is not thrown afterwards.
        ///// You can also cancel further forwarding to other event handlers by setting the 
        ///// <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> flag of the event args. Canceling the event but 
        ///// not setting the <see cref="FunctionCallInteractionEventArgs.Handled"/> flag, will not preventing to throw
        ///// the <see cref="IInteractionEventProxy.ButtonCombinationReleased"/> event.</remarks>
        //public event EventHandler<FunctionCallInteractionEventArgs> FunctionCall;


        #region IButton2FunctionProxy

        #region definition mappings

        /// <summary>Gets the default function mappings if exist.</summary>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> IButton2FunctionProxy.GetDefaultFunctionsMappings()
        {
            //mappingLock.EnterReadLock();
            //try
            //{
            return combinationToFunctionDic != null && combinationToFunctionDic.ContainsKey(string.Empty) ?
                combinationToFunctionDic[string.Empty] :
                null;
            //}
            //finally
            //{
            //    mappingLock.ExitReadLock();
            //}
        }

        /// <summary>Gets the function mappings for the given adapter.</summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> IButton2FunctionProxy.GetFunctionMappingsForAdapter(IBrailleIOAdapter adapter)
        {
            return adapter != null ? ((IButton2FunctionProxy)this).GetFunctionMappingsForAdapter(adapter.GetType()) :
                ((IButton2FunctionProxy)this).GetDefaultFunctionsMappings();
        }

        /// <summary>Gets the function mappings for the given device.</summary>
        /// <param name="device">The device.</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> IButton2FunctionProxy.GetFunctionMappingsForAdapter(BrailleIODevice device)
        {
            return device != null ? ((IButton2FunctionProxy)this).GetFunctionMappingsForAdapter(device.AdapterType) :
                ((IButton2FunctionProxy)this).GetDefaultFunctionsMappings();
        }

        /// <summary>Gets the function mappings for the given adapter type.</summary>
        /// <param name="adapterType">Type of the adapter.</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> IButton2FunctionProxy.GetFunctionMappingsForAdapter(Type adapterType)
        {
            return adapterType != null ? ((IButton2FunctionProxy)this).GetFunctionMappingsForAdapter(adapterType.ToString()) :
                ((IButton2FunctionProxy)this).GetDefaultFunctionsMappings();
        }

        /// <summary>Gets the function mappings for the given adapter type name (e.g. "BrailleIO.ShowOffAdapter".</summary>
        /// <param name="adapterTypeName">Name of the adapter type.</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt; - a Dictionary of the button-combination to function mappings.</returns>
        Dictionary<string, List<string>> IButton2FunctionProxy.GetFunctionMappingsForAdapter(string adapterTypeName)
        {
            //mappingLock.EnterReadLock();
            //try
            //{
            return combinationToFunctionDic != null && combinationToFunctionDic.ContainsKey(adapterTypeName) ?
            combinationToFunctionDic[adapterTypeName] :
            ((IButton2FunctionProxy)this).GetDefaultFunctionsMappings();
            //}
            //finally
            //{
            //    mappingLock.ExitReadLock();
            //}
        }

        #endregion

        #region mapping requests

        /// <summary></summary>
        /// <param name="adapter"></param>
        /// <param name="buttonCombination"></param>
        public List<string> GetFunctionMapping(IBrailleIOAdapter adapter, string buttonCombination)
        {
            return adapter != null ?
                GetFunctionMapping(adapter.GetType(), buttonCombination) :
                GetDefaultFunctionMapping(buttonCombination);
        }

        /// <summary>Gets the function mapping for the given button-combination by the adapter.</summary>
        /// <param name="device">The device.</param>
        /// <param name="buttonCombination">The button combination.</param>
        /// <returns>The found function-name mapping, if exist; otherwise the empty string is returned.</returns>
        public List<string> GetFunctionMapping(BrailleIODevice device, string buttonCombination)
        {
            List<string> fnc;
            if (device != null && !String.IsNullOrWhiteSpace(buttonCombination))
            {

                if(getCachedMapping(device, buttonCombination, out fnc))
                {
                    return fnc;
                }

                if (fnc == null) fnc = new List<string>();

                // get default function mappings
                List<String> defFnc = GetDefaultFunctionMapping(buttonCombination);
                // get function mappings of the adapter
                List<String> adptrFnc = null;
                if (!String.IsNullOrEmpty(device.AdapterType))
                {
                    var def = ((IButton2FunctionProxy)this).GetFunctionMappingsForAdapter(device.AdapterType);
                    if (def != null && def.Count > 0 && def.ContainsKey(buttonCombination))
                        adptrFnc = def[buttonCombination];
                }

                // get device function mappings
                List<String> devFnc = null;
                if (!String.IsNullOrEmpty(device.Name))
                {
                    var def = ((IButton2FunctionProxy)this).GetFunctionMappingsForAdapter(device.Name);
                    if (def != null && def.Count > 0 && def.ContainsKey(buttonCombination))
                        devFnc = def[buttonCombination];
                }

                // clean the adapter definitions by overriding of the device
                adptrFnc = cleanFunctionDefintion(adptrFnc, device.Name);

                // clean the default definition by the adapter definitions
                defFnc = cleanFunctionDefintion(defFnc, device.AdapterType);
                // clean the default list by device definitions
                defFnc = cleanFunctionDefintion(defFnc, device.Name);

                // add default functions
                if (defFnc != null) fnc = fnc.Union(defFnc).ToList();
                // add adapter functions
                if (adptrFnc != null) fnc = fnc.Union(adptrFnc).ToList();
                // add device functions
                if (devFnc != null) fnc = fnc.Union(devFnc).ToList();
            }
            else
            {
                fnc = GetDefaultFunctionMapping(buttonCombination);
            }

            // cache the result
            addToMappingCache(device, buttonCombination, fnc);

            return fnc;
        }

        /// <summary></summary>
        /// <param name="adapterType"></param>
        /// <param name="buttonCombination"></param>
        public List<string> GetFunctionMapping(Type adapterType, string buttonCombination)
        {
            return adapterType != null ?
                GetFunctionMapping(adapterType.ToString(), buttonCombination) :
                GetDefaultFunctionMapping(buttonCombination);
        }

        /// <summary>Gets the function mapping for the given button-combination by the adapter.</summary>
        /// <param name="adapterTypeName">Name of the adapter type.</param>
        /// <param name="buttonCombination">The button combination.</param>
        /// <returns>The found function-name mapping, if exist; otherwise the empty string is returned.</returns>
        public List<string> GetFunctionMapping(string adapterTypeName, string buttonCombination)
        {

            List<string> fnc = new List<string>();
            if (!String.IsNullOrEmpty(adapterTypeName) && !String.IsNullOrWhiteSpace(buttonCombination))
            {
                // get default function mappings
                List<String> defFnc = GetDefaultFunctionMapping(buttonCombination);
                // get function mappings of the adapter
                List<String> adptrFnc = null;
                if (!String.IsNullOrEmpty(adapterTypeName))
                {
                    var def = ((IButton2FunctionProxy)this).GetFunctionMappingsForAdapter(adapterTypeName);
                    if (def != null && def.Count > 0 && def.ContainsKey(buttonCombination))
                        adptrFnc = def[buttonCombination];
                }

                // clean the default definition by the adapter definitions
                defFnc = cleanFunctionDefintion(defFnc, adapterTypeName);

                // add default functions
                if (defFnc != null) fnc = fnc.Union(defFnc).ToList();
                // add adapter functions
                if (adptrFnc != null) fnc = fnc.Union(adptrFnc).ToList();
            }
            else
            {
                fnc = GetDefaultFunctionMapping(buttonCombination);
            }

            return fnc;
        }

        /// <summary>Gets the default function mapping for the given button-combination, if exist.</summary>
        /// <param name="buttonCombination">The button combination.</param>
        /// <returns>The found function-name mapping, if exist; otherwise the empty string is returned.</returns>
        public List<string> GetDefaultFunctionMapping(string buttonCombination)
        {
            //mappingLock.EnterReadLock();
            //try
            //{
            if (!string.IsNullOrWhiteSpace(buttonCombination))
            {
                var definition = ((IButton2FunctionProxy)this).GetDefaultFunctionsMappings();
                if (definition != null)
                {
                    if (definition.ContainsKey(buttonCombination))
                        return definition[buttonCombination];
                }
            }

            return null;
            //}
            //finally
            //{
            //    mappingLock.ExitReadLock();
            //}
        }

        #region Caching

        protected bool getCachedMapping(BrailleIODevice device, string combination, out List<string> function)
        {
            function = new List<string>();
            bool success = false;

            if (!String.IsNullOrEmpty(combination))
            {
                // check if device is listed
                Dictionary<string, List<string>> mappings;
                if (_cachingTable.TryGetValue(device.GetHashCode(), out mappings))
                {
                    if (mappings != null)
                        // check for 
                        success = mappings.TryGetValue(combination, out function);
                }
            }
            return success;
        }


        protected bool addToMappingCache(BrailleIODevice device, string combination, List<string> functionName)
        {
            if(!string.IsNullOrEmpty(combination))
            {
                int hash = device.GetHashCode();
                if (_cachingTable.ContainsKey(hash))
                {
                    _cachingTable.AddOrUpdate(
                        hash,
                        new Dictionary<string, List<string>>(1) { { combination, functionName } },
                        (key, oldValue) => { oldValue.Add(combination, functionName); return oldValue; });
                }
                else
                {
                    Dictionary<string, List<string>> mapping = new Dictionary<string, List<string>>();
                    mapping.Add(combination, functionName);
                    _cachingTable.AddOrUpdate(
                        hash, 
                        mapping, 
                        (key, oldValue) => mapping);
                }
            }
            return true;
        }  

        void cleanMappingCache()
        {
            _cachingTable.Clear();
        }


        #endregion

        #region Utils 

        List<String> cleanFunctionDefintion(List<String> functions, String adapterName)
        {
            if (functions != null && !String.IsNullOrEmpty(adapterName))
            {
                if (this.combinationToFunctionDic.ContainsKey(adapterName))
                {
                    for (int i = 0; i < functions.Count; i++)
                    {
                        // check if function has an overriding definition
                        if (this.functionToDeviceDefinitionDic.ContainsKey(functions[i]))
                        {
                            // check if one overriding is from the requested adapter
                            var overridings = this.functionToDeviceDefinitionDic[functions[i]];
                            if (overridings != null && overridings.Count > 0)
                            {
                                foreach (var adapter in overridings)
                                {
                                    if (adapter.Equals(adapterName)) functions[i] = String.Empty;
                                }
                            }
                        }
                    }
                }
            }
            return functions;
        }

        #endregion 

        #endregion

        /// <summary>Loads the function mappings from a given XML string.</summary>
        /// <param name="mappingXML">The mapping XML content string.</param>
        /// <param name="combine">if set to <c>true</c> the definition will be included into the 
        /// existing mappings (duplicates will be overwritten); otherwise the whole mapping will be replaced (dangerous!!).</param>
        /// <returns>
        ///   <c>true</c> if the mapping was loaded successfully, <c>false</c> otherwise.</returns>
        public virtual bool LoadFunctionMapping(string mappingXML, bool combine = true)
        {
            cleanMappingCache();
            if (mappingLoader != null)
            {
                try
                {
                    Dictionary<string, List<string>> funcDevList;
                    var mappings = mappingLoader.LoadMapping(mappingXML, out funcDevList);
                    if (combine)
                    {
                        Dictionary<string, List<string>> allFuncDevList = null;
                        Dictionary<string, Dictionary<string, List<string>>> allMappings = null;
                        //try
                        //{
                        //mappingLock.EnterReadLock();
                        allMappings = ButtonMappingLoader.CombineDictionaries(this.combinationToFunctionDic, mappings);
                        allFuncDevList = ButtonMappingLoader.CombineDictionaries(this.functionToDeviceDefinitionDic, funcDevList);
                        //}
                        //finally
                        //{
                        //    mappingLock.ExitReadLock();
                        //}
                        // thread-save store back ?!
                        //try
                        //{
                        //    mappingLock.EnterWriteLock();
                        this.combinationToFunctionDic = allMappings;
                        this.functionToDeviceDefinitionDic = allFuncDevList;
                        //}
                        //finally
                        //{
                        //    mappingLock.ExitWriteLock();
                        //}
                    }
                    else
                    {
                        this.combinationToFunctionDic = mappings;
                        this.functionToDeviceDefinitionDic = funcDevList;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(LogPriority.ALWAYS, this, "[ERROR]\terror while loading button-combination mapping XML.", ex);
                }
            }
            return false;
        }

        #endregion

    }
}
