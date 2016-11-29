using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrailleIO;
using System.Collections.Concurrent;
using BrailleIO.Interface;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Factory for building a <see cref="tud.mci.tangram.TangramLector.IBrailleIOButtonMediator"/> for a related <see cref="BrailleIO.Interface.IBrailleIOAdapter"/>
    /// </summary>
    public static class BrailleIOButtonMediatorFactory
    {
        #region Members

        private static ConcurrentDictionary<String, Type> _mcList = new ConcurrentDictionary<String, Type>();
        readonly static object _mcListLock = new object();

        /// <summary>
        /// Gets or sets the mediator class list.
        /// Dictionary combining the adapter.GetType().ToString() with his related Button mediator.
        /// </summary>
        /// <value>
        /// The mediator class list.
        /// </value>
        public static ConcurrentDictionary<String, Type> MediatorClassList
        {
            get
            {
                if (_mcList == null) _mcList = new ConcurrentDictionary<String, Type>();
                if (_mcList.Count < 1) initializeMediatorList();
                return _mcList;
            }
            set
            {
                lock (_mcListLock)
                {
                    _mcList = value;
                }
            }
        }

        static ConcurrentDictionary<String, IBrailleIOButtonMediator> _mList = new ConcurrentDictionary<String, IBrailleIOButtonMediator>();
        readonly static object _mListLock = new object();

        /// <summary>
        /// Gets or sets the mediator list.
        /// A dictionary combining a device name with the related mediator. 
        /// </summary>
        /// <value>
        /// The mediator list.
        /// </value>
        public static ConcurrentDictionary<String, IBrailleIOButtonMediator> MediatorList
        {
            get
            {
                if (_mList == null) _mList = new ConcurrentDictionary<String, IBrailleIOButtonMediator>();
                return _mList;
            }
            set
            {
                lock (_mListLock)
                {
                    _mList = value;
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets the mediator related to the adapter. The mediator should interpret the adapters generic
        /// data and map them to the general event style of the interaction manager events.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns>A mediator for interpreting the adapter buttons</returns>
        public static IBrailleIOButtonMediator GetMediator(IBrailleIOAdapter adapter)
        {
            if (adapter == null) return null;
            BrailleIODevice device = adapter.Device;
            IBrailleIOButtonMediator mediator;
            if (MediatorList.TryGetValue(device.Name, out mediator)) { return mediator; }

            Type mediatorType;
            if (MediatorClassList.TryGetValue(adapter.GetType().ToString(), out mediatorType))
            {
                if (mediatorType != null)
                {
                    try
                    {
                        var obj = (IBrailleIOButtonMediator)Activator.CreateInstance(mediatorType);
                        MediatorList.TryAdd(device.Name, obj);
                        if (obj is AbstractBrailleIOButtonMediatorBase)
                        {
                            ((AbstractBrailleIOButtonMediatorBase)obj).setDevice(device);
                        }
                        return obj;
                    }
                    catch (System.Exception) { }
                }
            }
            return null;
        }

        private static void initializeMediatorList()
        {
            loadExtensionAdapterMediators();
        }

        #region Extension Loader    |   Adapter Button Mediator

        static void loadExtensionAdapterMediators()
        {
            string path = extensibility.ExtensionLoader.GetCurrentDllDirectory() + "\\Extensions\\Adapter";
            var adapterMediators = extensibility.ExtensionLoader.LoadAllExtensions(typeof(IBrailleIOButtonMediator), path);

            if (adapterMediators != null && adapterMediators.Count > 0)
            {
                foreach (var subdir in adapterMediators)
                {
                    if (subdir.Value != null && subdir.Value.Count > 0)
                    {
                        foreach (Type adapterMediatorType in subdir.Value)
                        {
                            try
                            {
                                IBrailleIOButtonMediator mediator = extensibility.ExtensionLoader.CreateObjectFromType(adapterMediatorType) as IBrailleIOButtonMediator;
                                if (mediator != null)
                                {
                                    List<Type> relatedTypes = mediator.GetRelatedAdapterTypes();
                                    if (relatedTypes != null && relatedTypes.Count > 0)
                                    {
                                        foreach (Type adapterType in relatedTypes)
                                        {
                                            if (adapterType != null)
                                            {
                                                //add to the look up dictionary
                                                _mcList[adapterType.ToString()] = adapterMediatorType;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Log(LogPriority.DEBUG, "BrailleIOButtonMediatorFactory", "[ERROR] Can't load button mediator from extension: \n" + ex);
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
