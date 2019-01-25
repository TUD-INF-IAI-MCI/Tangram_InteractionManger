using BrailleIO;
using BrailleIO.Interface;
using BrailleIO.Structs;
using Gestures.Recognition;
using Gestures.Recognition.GestureData;
using Gestures.Recognition.Interfaces;
using Gestures.Recognition.Preprocessing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using tud.mci.LanguageLocalization;
using tud.mci.tangram.audio;
using tud.mci.tangram.TangramLector.Button2FunctionMapping;
using tud.mci.tangram.TangramLector.Control;
using tud.mci.tangram.TangramLector.Interaction_Manager;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Interprets and forwards input interaction from 
    /// <see cref="BrailleIO.Interface.IBrailleIOAdapter"/>
    /// </summary>
    public class InteractionManager : AbstractInteractionEventProxy, IDisposable, ILocalizable
    {
        #region Members

        #region private

        readonly LL LL = new LL(Properties.Resources.Language);

        bool _run;
        Thread inputQueueThread;
        readonly Dictionary<String, IBrailleIOAdapter> devices = new Dictionary<String, IBrailleIOAdapter>();
        readonly ConcurrentQueue<InteractionQueueItem> inputQueue = new ConcurrentQueue<InteractionQueueItem>();

        readonly ConcurrentDictionary<BrailleIODevice, ITrackBlobs> blobTrackers = new ConcurrentDictionary<BrailleIODevice, ITrackBlobs>();
        readonly ConcurrentDictionary<BrailleIODevice, IRecognizeGestures> gestureRecognizers = new ConcurrentDictionary<BrailleIODevice, IRecognizeGestures>();

        static readonly ButtonMappingLoader mappingLoader = new ButtonMappingLoader();
       // Dictionary<string, Dictionary<string, List<string>>> buttonCombination2FunctionMappings = new Dictionary<string, Dictionary<string, List<string>>>();

        #endregion

        #region public

        private BrailleKeyboardInput _bki = new BrailleKeyboardInput();
        /// <summary>
        /// Gets the current active Braille keyboard input (an interpreted complete entered text input by a Braille keyboard).
        /// </summary>
        /// <value>The input by a Braille keyboard.</value>
        public BrailleKeyboardInput BKI
        {
            get { return _bki; }
        }

        InteractionMode _mode = InteractionMode.Normal;
        /// <summary>
        /// Gets or sets the mode giving a hint about the current activity state.
        /// </summary>
        /// <value>The new interaction mode.</value>
        public InteractionMode Mode
        {
            get { return _mode; }
            private set
            {
                var old = _mode;
                _mode = value;
                fireInteractionModeChangedEvent(old, _mode);
            }
        }

        static readonly List<IClassifyActivation> _gestureClassifierTypes = new List<IClassifyActivation>()
        {
            new IClassifyActivation(typeof(Gestures.Recognition.Classifier.TapClassifier)),
            new IClassifyActivation(typeof(Gestures.Recognition.Classifier.MultitouchClassifier))            
        };
        static readonly object _classifierTypeLock = new object();
        static List<IClassifyActivation> gestureClassifierTypes
        {
            get
            {
                lock (_classifierTypeLock)
                {
                    return InteractionManager._gestureClassifierTypes;
                }
            }
        }


        static Type _geRecType = typeof(GestureRecognizer);
        /// <summary>
        /// Gets or sets the type of the gesture recognizer to use for handling touch data.
        /// </summary>
        /// <value>
        /// The type of the gesture recognizer - must implement <see cref="IRecognizeGestures"/>.
        /// </value>
        static public Type GestureRecognizerType
        {
            set
            {
                if (value != null && typeof(IRecognizeGestures).IsAssignableFrom(value))
                    _geRecType = value;
                else
                    _geRecType = null;
            }
            get
            {
                if (_geRecType == null)
                    _geRecType = typeof(GestureRecognizer);
                return _geRecType;
            }
        }

        /// <summary>
        /// The button combination mapper. 
        /// A class for mapping device-related button combinations to function names.
        /// </summary>
        public readonly Button2FunctionProxy ButtoncombinationMapper = new Button2FunctionProxy();

        #endregion

        #region Singleton

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        /// <value>The instance.</value>
        public static InteractionManager Instance
        {
            get { return instance; }
        }

        private static readonly InteractionManager instance = new InteractionManager();

        #endregion

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionManager"/> class.
        /// </summary>
        private InteractionManager()
        {
            createInputQueueThread();
            //this.ButtonReleased += new EventHandler<ButtonReleasedEventArgs>(InteractionManager_ButtonReleased);
        }

        ~InteractionManager()
        {
            try
            {
                _run = false; if (inputQueueThread != null && inputQueueThread.IsAlive) inputQueueThread.Abort();
            }
            catch { }
        }

        /// <summary>
        /// Dispose this instance;
        /// </summary>
        public void Dispose()
        {
            try
            {
                _run = false;
                this.inputQueueThread.Abort();
            }
            catch { }
        }

        #endregion

        #region Device Handling

        /// <summary>
        /// Adds a new device to the InteractionManger.
        /// </summary>
        /// <param name="device">The new device.</param>
        /// <returns>true if it could be added otherwise false</returns>
        public bool AddNewDevice(IBrailleIOAdapter device)
        {
            if (device != null && device.Device != null
                && !String.IsNullOrWhiteSpace(device.Device.Name))
            {
                unregisterForEvents(device);
                registerForEvents(device);

                // gesture recognizer registration for the device
                initalizeGestureRecognition(device.Device);
                if (device.Connected)
                {
                    if (devices.ContainsKey(device.Device.Name))
                    {
                        devices[device.Device.Name] = device;
                    }
                    else { devices.Add(device.Device.Name, device); }
                    return devices.ContainsKey(device.Device.Name);
                }
            }
            return false;
        }

        /// <summary>
        /// Removes a device from this interaction manager - so no more inputs will be interpreted.
        /// </summary>
        /// <param name="device">The device to remove.</param>
        /// <returns><c>true</c> if the device was successfully removed.</returns>
        public bool RemoveDevice(BrailleIO.AbstractBrailleIOAdapterBase device)
        {
            if (device != null)
            {
                unregisterForEvents(device);

                // gesture recognizer registration for the device
                unregisterGestureRecognition(device.Device);
                if (device.Connected)
                {
                    devices.Remove(device.Device.Name);
                }
            }
            return true;
        }

        #endregion

        #region External Controlling Functions

        /// <summary>
        /// Changes the interaction mode.
        /// </summary>
        /// <param name="mode">The new mode.</param>
        /// <returns>the current interaction mode</returns>
        public InteractionMode ChangeMode(InteractionMode mode)
        {
            Mode = mode; return Mode;
        }

        /// <summary>
        /// Aborts all audio output.
        /// </summary>
        public void AbortAudio()
        {
            AudioRenderer.Instance.Abort();
            Logger.Instance.Log(LogPriority.MIDDLE, this, "[INTERACTION] abort speech output");
        }

        #endregion

        #region Interaction Input Queue

        #region Thread

        void createInputQueueThread()
        {
            _run = true;
            if (inputQueueThread != null)
            {
                if (inputQueueThread.IsAlive) return;
                else inputQueueThread.Start();
            }
            else
            {
                inputQueueThread = new Thread(new ThreadStart(checkInputQueue));
                inputQueueThread.Name = "TangramLectorInputQueueThread";
                inputQueueThread.IsBackground = true;
                inputQueueThread.Start();
            }
        }

        void checkInputQueue()
        {
            while (_run)
            {
                if (inputQueue.Count > 0)
                {
                    try
                    {
                        InteractionQueueItem result;
                        int i = 0;
                        while (!inputQueue.TryDequeue(out result) && i++ < 5) { Thread.Sleep(5); }
                        handleInputQueueItem(result);
                    }
                    catch { }
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }

        #endregion

        #region Dequeue

        private void handleInputQueueItem(InteractionQueueItem interactionQueueItem)
        {
            try
            {
                switch (interactionQueueItem.Type)
                {
                    case InteractionQueueObjectType.Unknown:
                        break;
                    case InteractionQueueObjectType.ButtonPressed:
                        handleKeyPressedEvent(interactionQueueItem.Sender, interactionQueueItem.Device, interactionQueueItem.Args as BrailleIO_KeyPressed_EventArgs);
                        break;
                    case InteractionQueueObjectType.ButtonStateChange:
                        handleKeyStateChangedEvent(interactionQueueItem.Sender, interactionQueueItem.Device, interactionQueueItem.Args as BrailleIO_KeyStateChanged_EventArgs);
                        break;
                    case InteractionQueueObjectType.Touch:
                        handleTouchEvent(interactionQueueItem.Sender, interactionQueueItem.Device, interactionQueueItem.Args as BrailleIO_TouchValuesChanged_EventArgs);
                        break;
                    case InteractionQueueObjectType.InputChanged:
                        break;
                    case InteractionQueueObjectType.Error:
                        break;
                    case InteractionQueueObjectType.ButtonCombination:
                        handleButtonCombinationEvent(interactionQueueItem.Sender, interactionQueueItem.Device, interactionQueueItem.Args as BrailleIO_KeyCombinationReleased_EventArgs);
                        break;
                    default:
                        Logger.Instance.Log(LogPriority.ALWAYS, this, "[ERROR] Cannot handle enqueued interaction event: " + interactionQueueItem.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(LogPriority.ALWAYS, this, "[ERROR] Exception while handling input event: " + ex);
            }
        }

        #endregion

        #region Enqueue

        /// <summary>
        /// Enqueues an interaction queue item.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <param name="sender">The sender.</param>
        protected void enqueueInteractionQueueItem(DateTime timestamp, EventArgs e, object sender) { enqueueInteractionQueueItem(timestamp, InteractionQueueObjectType.Unknown, e, sender); }
        /// <summary>
        /// Enqueues an interaction queue item.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <param name="sender">The sender.</param>
        protected void enqueueInteractionQueueItem(EventArgs e, object sender) { enqueueInteractionQueueItem(InteractionQueueObjectType.Unknown, e, sender); }
        /// <summary>
        /// Enqueues an interaction queue item.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <param name="sender">The sender.</param>
        protected void enqueueInteractionQueueItem(InteractionQueueObjectType type, EventArgs e, object sender) { enqueueInteractionQueueItem(DateTime.Now, type, e, sender); }
        /// <summary>
        /// Enqueues an interaction queue item.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="type">The type.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <param name="sender">The sender.</param>
        protected void enqueueInteractionQueueItem(DateTime timestamp, InteractionQueueObjectType type, EventArgs e, object sender)
        {
            if (e != null)
            {
                BrailleIO.BrailleIODevice device = null;
                if (sender != null && sender is IBrailleIOAdapter) device = ((IBrailleIOAdapter)sender).Device;
                inputQueue.Enqueue(new InteractionQueueItem(timestamp, type, device, e, sender));
            }
        }
        #endregion

        #endregion

        #region Create and fire events from inputQueue

        private void handleKeyPressedEvent(Object sender, BrailleIODevice brailleIODevice, BrailleIO_KeyPressed_EventArgs brailleIO_KeyPressed_EventArgs)
        {
            if (brailleIO_KeyPressed_EventArgs != null)
            {
                List<String> pressedGenKeys = new List<String>();
                BrailleIO_DeviceButton pressedKeys = BrailleIO_DeviceButton.None;
                BrailleIO_BrailleKeyboardButton pressedKbKeys = BrailleIO_BrailleKeyboardButton.None;
                BrailleIO_AdditionalButton[] pressedAddKeys = null;

                var mediator = BrailleIOButtonMediatorFactory.GetMediator(sender as IBrailleIOAdapter);
                if (mediator != null)
                {
                    pressedGenKeys = mediator.GetAllPressedGenericButtons(brailleIO_KeyPressed_EventArgs);
                    pressedKeys = mediator.GetAllPressedGeneralButtons(brailleIO_KeyPressed_EventArgs);
                    pressedKbKeys = mediator.GetAllPressedBrailleKeyboardButtons(brailleIO_KeyPressed_EventArgs);
                    pressedAddKeys = mediator.GetAllPressedAdditionalButtons(brailleIO_KeyPressed_EventArgs);
                }
                if (pressedKeys != BrailleIO_DeviceButton.None || pressedKbKeys != BrailleIO_BrailleKeyboardButton.None || pressedAddKeys != null)
                    fireButtonPressedEvent(brailleIODevice, pressedKeys, pressedKbKeys, pressedAddKeys, pressedGenKeys);

                startGesture(pressedKeys, pressedKbKeys, pressedAddKeys, pressedGenKeys, brailleIODevice);
            }
        }

        private void handleKeyStateChangedEvent(Object sender, BrailleIODevice brailleIODevice, BrailleIO_KeyStateChanged_EventArgs brailleIO_KeyStateChanged_EventArgs)
        {
            List<String> pressedGenKeys = new List<String>();
            BrailleIO_DeviceButton pressedKeys = BrailleIO_DeviceButton.None;
            BrailleIO_BrailleKeyboardButton pressedKbKeys = BrailleIO_BrailleKeyboardButton.None;
            BrailleIO_AdditionalButton[] pressedAddKeys = null;
            List<String> releasedGenKeys = new List<String>();
            BrailleIO_DeviceButton releasedKeys = BrailleIO_DeviceButton.None;
            BrailleIO_BrailleKeyboardButton releasedKbKeys = BrailleIO_BrailleKeyboardButton.None;
            BrailleIO_AdditionalButton[] releasedAddKeys = null;

            BrailleIO.Structs.KeyCombinationItem kbi = new BrailleIO.Structs.KeyCombinationItem();

            var mediator = BrailleIOButtonMediatorFactory.GetMediator(sender as BrailleIO.Interface.IBrailleIOAdapter);
            if (mediator != null)
            {
                pressedGenKeys = mediator.GetAllPressedGenericButtons(brailleIO_KeyStateChanged_EventArgs);
                pressedKeys = mediator.GetAllPressedGeneralButtons(brailleIO_KeyStateChanged_EventArgs);
                pressedKbKeys = mediator.GetAllPressedBrailleKeyboardButtons(brailleIO_KeyStateChanged_EventArgs);
                pressedAddKeys = mediator.GetAllPressedAdditionalButtons(brailleIO_KeyStateChanged_EventArgs);

                releasedGenKeys = mediator.GetAllReleasedGenericButtons(brailleIO_KeyStateChanged_EventArgs);
                releasedKeys = mediator.GetAllReleasedGeneralButtons(brailleIO_KeyStateChanged_EventArgs);
                releasedKbKeys = mediator.GetAllReleasedBrailleKeyboardButtons(brailleIO_KeyStateChanged_EventArgs);
                releasedAddKeys = mediator.GetAllReleasedAdditionalButtons(brailleIO_KeyStateChanged_EventArgs);
            }

            kbi = new BrailleIO.Structs.KeyCombinationItem(
                pressedKeys, releasedKeys,
                pressedKbKeys, releasedKbKeys,
                pressedAddKeys, releasedAddKeys
                );

            if (kbi.AreButtonsPressed() || (pressedGenKeys != null && pressedGenKeys.Count > 0))
                fireButtonPressedEvent(brailleIODevice, pressedKeys, pressedKbKeys, pressedAddKeys, pressedGenKeys);
            if (kbi.AreButtonsReleased() || (releasedGenKeys != null && releasedGenKeys.Count > 0))
                fireButtonReleasedEvent(brailleIODevice, pressedKeys, pressedKbKeys, pressedAddKeys, pressedGenKeys, 
                    releasedKeys, releasedKbKeys, releasedAddKeys, releasedGenKeys);

            startGesture(pressedKeys, pressedKbKeys, pressedAddKeys, pressedGenKeys, brailleIODevice);
            endGesture(
                brailleIODevice,
                kbi,
                //releasedKeys, releasedKbKeys, releasedAddKeys, 
                pressedGenKeys, 
                releasedGenKeys 
                //pressedKeys, pressedKbKeys, pressedAddKeys, 
                );

        }

        private void handleButtonCombinationEvent(object sender, BrailleIODevice device,
            BrailleIO_KeyCombinationReleased_EventArgs brailleIO_KeyStateChanged_EventArgs)
        {
            if (brailleIO_KeyStateChanged_EventArgs != null)
            {
                var combination = brailleIO_KeyStateChanged_EventArgs.KeyCombination;

                // ATTENTION: those proprietary buttons are not interpretable by this event
                List<String> pressedGenKeys = new List<String>();
                List<String> releasedGenKeys = new List<String>();

                var mediator = BrailleIOButtonMediatorFactory.GetMediator(sender as BrailleIO.Interface.IBrailleIOAdapter);
                if (mediator != null)
                {
                    //pressedGenKeys = mediator.GetAllPressedGenericButtons(brailleIO_KeyStateChanged_EventArgs);
                    combination.PressedGeneralKeys = mediator.GetAllPressedGeneralButtons(combination.PressedGeneralKeys);
                    combination.PressedKeyboardKeys = mediator.GetAllPressedBrailleKeyboardButtons(combination.PressedKeyboardKeys);
                    combination.PressedAdditionalKeys = mediator.GetAllPressedAdditionalButtons(combination.PressedAdditionalKeys);

                    //releasedGenKeys = mediator.GetAllReleasedGenericButtons(brailleIO_KeyStateChanged_EventArgs);
                    combination.PressedGeneralKeys = mediator.GetAllReleasedGeneralButtons(combination.PressedGeneralKeys);
                    combination.ReleasedKeyboardKeys = mediator.GetAllReleasedBrailleKeyboardButtons(combination.ReleasedKeyboardKeys);
                    combination.ReleasedAdditionalKeys = mediator.GetAllReleasedAdditionalButtons(combination.ReleasedAdditionalKeys);

                }

                // check for function mapping
                if(ButtoncombinationMapper != null)
                {
                    List<string> func = ButtoncombinationMapper.GetFunctionMapping(device, combination.ReleasedButtonsToString());

                    if (func != null && func.Count > 0)
                    {
                        bool cancel = false;
                        // fire event
                        foreach (var item in func)
                        {
                            bool handled = fireFunctionCall(item, device, combination, pressedGenKeys, releasedGenKeys, out cancel);
                            if (handled) return;
                        }

                    }
                }

                // End gesture call?
                fireButtonCombinationReleasedEvent(device, combination,pressedGenKeys, releasedGenKeys);
            }   
        }

        #endregion

        #region Adapter Event handlers

        private void registerForEvents(IBrailleIOAdapter adapter)
        {
            unregisterForEvents(adapter);
            try
            {
                adapter.initialized += new EventHandler<BrailleIO_Initialized_EventArgs>(adapter_initialized);
                adapter.errorOccurred += new EventHandler<BrailleIO_ErrorOccured_EventArgs>(adapter_errorOccured);
                adapter.inputChanged += new EventHandler<BrailleIO_InputChanged_EventArgs>(adapter_inputChanged);
                adapter.keyPressed += new EventHandler<BrailleIO_KeyPressed_EventArgs>(adapter_keyPressed);
                adapter.keyStateChanged += new EventHandler<BrailleIO_KeyStateChanged_EventArgs>(adapter_keyStateChanged);
                adapter.touchValuesChanged += new EventHandler<BrailleIO_TouchValuesChanged_EventArgs>(adapter_touchValuesChanged);

                if (adapter is IBrailleIOAdapter2)
                {
                    ((IBrailleIOAdapter2)adapter).keyCombinationReleased += adapter_keyCombinationReleased;
                }
            
            }
            catch (Exception)
            { }
        }

        private void unregisterForEvents(IBrailleIOAdapter adapter)
        {
            try
            {
                adapter.initialized -= new EventHandler<BrailleIO_Initialized_EventArgs>(adapter_initialized);
                adapter.errorOccurred -= new EventHandler<BrailleIO_ErrorOccured_EventArgs>(adapter_errorOccured);
                adapter.inputChanged -= new EventHandler<BrailleIO_InputChanged_EventArgs>(adapter_inputChanged);
                adapter.keyPressed -= new EventHandler<BrailleIO_KeyPressed_EventArgs>(adapter_keyPressed);
                adapter.keyStateChanged -= new EventHandler<BrailleIO_KeyStateChanged_EventArgs>(adapter_keyStateChanged);
                adapter.touchValuesChanged -= new EventHandler<BrailleIO_TouchValuesChanged_EventArgs>(adapter_touchValuesChanged);

                if (adapter is IBrailleIOAdapter2)
                {
                    ((IBrailleIOAdapter2)adapter).keyCombinationReleased -= adapter_keyCombinationReleased;
                }            
            }
            catch (Exception)
            { }
        }

        void adapter_initialized(object sender, BrailleIO.Interface.BrailleIO_Initialized_EventArgs e)
        {
            if (e != null && e.device != null && sender != null && sender is AbstractBrailleIOAdapterBase)
            {
                if (devices.ContainsKey(e.device.Name))
                {
                    var oldDevice = devices[e.device.Name];
                    unregisterForEvents(oldDevice);
                    devices.Remove(e.device.Name);
                }
                devices.Add(e.device.Name, sender as AbstractBrailleIOAdapterBase);

                unregisterForEvents(sender as AbstractBrailleIOAdapterBase);
                registerForEvents(sender as AbstractBrailleIOAdapterBase);

                // gesture recognizer registration for the device
                try { initalizeGestureRecognition(e.device); }
                catch (Exception ex)
                {
                    AudioRenderer.Instance.PlaySound(LL.GetTrans("tangram.interaction_manager.error.gesture_recognizer"));
                    Logger.Instance.Log(LogPriority.DEBUG, this, "gesture recognizer initialization error", ex);
                }
            }
        }

        void adapter_touchValuesChanged(object sender, BrailleIO.Interface.BrailleIO_TouchValuesChanged_EventArgs e)
        {
            enqueueInteractionQueueItem(TimeStampUtils.UnixTimeStampToDateTime((double)e.timestamp), InteractionQueueObjectType.Touch, e, sender);
        }
        void adapter_keyStateChanged(object sender, BrailleIO.Interface.BrailleIO_KeyStateChanged_EventArgs e)
        {
            enqueueInteractionQueueItem(InteractionQueueObjectType.ButtonStateChange, e, sender);
        }
        void adapter_keyPressed(object sender, BrailleIO.Interface.BrailleIO_KeyPressed_EventArgs e)
        {
            enqueueInteractionQueueItem(InteractionQueueObjectType.ButtonPressed, e, sender);
        }
        void adapter_inputChanged(object sender, BrailleIO.Interface.BrailleIO_InputChanged_EventArgs e)
        {
            enqueueInteractionQueueItem(TimeStampUtils.UnixTimeStampToDateTime((double)e.timestamp), InteractionQueueObjectType.InputChanged, e, sender);
        }
        void adapter_errorOccured(object sender, BrailleIO.Interface.BrailleIO_ErrorOccured_EventArgs e)
        {
            enqueueInteractionQueueItem(InteractionQueueObjectType.Error, e, sender);
        }
        void adapter_keyCombinationReleased(object sender, BrailleIO_KeyCombinationReleased_EventArgs e)
        {
            enqueueInteractionQueueItem(InteractionQueueObjectType.ButtonCombination, e, sender);
        }

        //void InteractionManager_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        //{
        //    // stop this handling here and use the generic handling by Braille IO
        //    // FIXME: 
        //    //return;
        //    //if (e != null)
        //    //{
        //    //    checkForKeyCombination(e.Device, e.PressedGeneralKeys, e.PressedGenericKeys, e.ReleasedGeneralKeys, e.ReleasedGenericKeys);
        //    //}
        //}

        #endregion

        #region Fire Events

        /// <summary>
        /// Fires the button released event.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        /// <param name="releasedGeneralKeys">The released general keys (Flag).</param>
        /// <param name="releasedBrailleKeyboardKeys">The released braille keyboard keys (Flag).</param>
        /// <param name="releasedAdditionalKeys">An array of released additional keys (Flag).</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys (proprietary).</param>
        /// <returns>
        ///   <c>true</c> if the handling was canceled by an EventHanlder, <c>false</c> otherwise.</returns>
        protected bool fireButtonReleasedEvent(
            BrailleIO.BrailleIODevice device,
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys,
            BrailleIO_DeviceButton releasedGeneralKeys,
            BrailleIO_BrailleKeyboardButton releasedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] releasedAdditionalKeys,
            List<String> releasedGenericKeys)
        {
            var args = new ButtonReleasedEventArgs(device, 
                pressedGeneralKeys, pressedBrailleKeyboardKeys, pressedAdditionalKeys, pressedGenericKeys, 
                releasedGeneralKeys, releasedBrailleKeyboardKeys, releasedAdditionalKeys, releasedGenericKeys);
            Logger.Instance.Log(LogPriority.OFTEN, this, "Button released: " + args.ToString());
            bool cancel = base.fireButtonReleasedEvent(args);
            if (cancel) { System.Diagnostics.Debug.WriteLine("InteractionManager Event canceled"); }
            return cancel;
        }

        /// <summary>Fires the button combination released event.</summary>
        /// <param name="device">The device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        /// <param name="releasedGeneralKeys">The released general keys (Flag).</param>
        /// <param name="releasedBrailleKeyboardKeys">The released braille keyboard keys (Flag).</param>
        /// <param name="releasedAdditionalKeys">An array of released additional keys (Flag).</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys (proprietary).</param>
        /// <returns>
        ///   <c>true</c> if the handling was canceled by an EventHanlder, <c>false</c> otherwise.</returns>
        protected bool fireButtonCombinationReleasedEvent(BrailleIO.BrailleIODevice device,
            BrailleIO.Structs.KeyCombinationItem combinationItem,
            List<String> pressedGenericKeys,
            List<String> releasedGenericKeys)
        {
            var args = new ButtonReleasedEventArgs(device,
                combinationItem.PressedGeneralKeys, combinationItem.PressedKeyboardKeys, combinationItem.PressedAdditionalKeys, pressedGenericKeys,
                combinationItem.ReleasedGeneralKeys, combinationItem.ReleasedKeyboardKeys, combinationItem.ReleasedAdditionalKeys, releasedGenericKeys);
            Logger.Instance.Log(LogPriority.OFTEN, this, "Button combination released: " + args.ToString());

            args.ReleasedButtonsToString();

            bool cancel = base.fireButtonCombinationReleasedEvent(args);
            if (cancel) { System.Diagnostics.Debug.WriteLine("InteractionManager Event canceled"); }
            return cancel;
        }


        /// <summary>Fires the function call.</summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="device">The device.</param>
        /// <param name="keyCombination">The key combination.</param>
        /// <param name="pressedGenericKeys">The pressed generic keys.</param>
        /// <param name="releasedGenericKeys">The released generic keys.</param>
        /// <param name="canceled">if set to <c>true</c> the further forwarding of the event was canceled by one event handler.</param>
        /// <returns>
        ///   <c>true</c> if the function/event was handled by (at least) one event handler, <c>false</c> otherwise.</returns>
        protected bool fireFunctionCall(
            string functionName,
            BrailleIO.BrailleIODevice device,
            BrailleIO.Structs.KeyCombinationItem keyCombination,
            List<String> pressedGenericKeys,
            List<String> releasedGenericKeys,
            out bool canceled)
        {
            canceled = false;
            bool handled = false;
            Logger.Instance.Log(LogPriority.OFTEN, this, "Function call: " + functionName);
            handled = base.fireFunctionCalledEvent(functionName, device, keyCombination, pressedGenericKeys, releasedGenericKeys, out canceled);
            if (canceled) { System.Diagnostics.Debug.WriteLine("InteractionManager Event canceled"); }
            if (handled) { System.Diagnostics.Debug.WriteLine("InteractionManager function Event handled"); }
            return handled;
        }


        /// <summary>
        /// Fires the button pressed event.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        /// <returns>
        ///   <c>true</c> if the handling was canceled by an EventHanlder, <c>false</c> otherwise.</returns>
        protected bool fireButtonPressedEvent(
            BrailleIO.BrailleIODevice device,
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys)
        {
            var args = new ButtonPressedEventArgs(device, pressedGeneralKeys, pressedBrailleKeyboardKeys, pressedAdditionalKeys,  pressedGenericKeys);
            Logger.Instance.Log(LogPriority.OFTEN, this, "Button pressed: " + args.ToString());
            bool cancel = base.fireButtonPressedEvent(args);
            if (cancel) { System.Diagnostics.Debug.WriteLine("InteractionManager Event canceled"); }
            return cancel;
        }

        /// <summary>
        /// Fires the gesture event.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        /// <param name="releasedGeneralKeys">The released general keys (Flag).</param>
        /// <param name="releasedBrailleKeyboardKeys">The released braille keyboard keys (Flag).</param>
        /// <param name="releasedAdditionalKeys">An array of released additional keys (Flag).</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys (proprietary).</param>
        /// <param name="gesture">The gesture.</param>
        /// <returns>
        ///   <c>true</c> if the handling was canceled by an EventHanlder, <c>false</c> otherwise.</returns>
        protected bool fireGestureEvent(
            BrailleIO.BrailleIODevice device,
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys,
            BrailleIO_DeviceButton releasedGeneralKeys,
            BrailleIO_BrailleKeyboardButton releasedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] releasedAdditionalKeys,
            List<String> releasedGenericKeys, 
            Gestures.Recognition.Interfaces.IClassificationResult gesture)
        {
            var args = new GestureEventArgs(device, 
                pressedGeneralKeys, pressedBrailleKeyboardKeys, pressedAdditionalKeys, pressedGenericKeys, 
                releasedGeneralKeys, releasedBrailleKeyboardKeys, releasedAdditionalKeys, releasedGenericKeys, 
                gesture);
            Logger.Instance.Log(LogPriority.OFTEN, this, "Gesture performed: " + args.Gesture.ToString());
            bool cancel = base.fireGestureEvent(args);
            if (cancel) { System.Diagnostics.Debug.WriteLine("InteractionManager Event canceled"); }
            return cancel;
        }

        /// <summary>
        /// Occurs when interaction mode is changed.
        /// </summary>
        public event EventHandler<InteractionModeChangedEventArgs> InteractionModeChanged;

        /// <summary>
        /// Fires the interaction mode changed event.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected void fireInteractionModeChangedEvent(InteractionMode oldValue, InteractionMode newValue)
        {
            if (InteractionModeChanged != null)
            {
                try
                {
                    InteractionModeChanged.Invoke(this, new InteractionModeChangedEventArgs(oldValue, newValue));
                }
                catch (Exception){}
            }
        }

        #endregion

        //#region Key Combination Interpreter

        //Dictionary<BrailleIODevice, System.Timers.Timer> _keyCombinationTimerList = new Dictionary<BrailleIODevice, System.Timers.Timer>();
        //Dictionary<System.Timers.Timer, KeyCombinationItem> _keyCombinationTimerButtonList = new Dictionary<System.Timers.Timer, KeyCombinationItem>();

        //readonly object _timerListLock = new object();
        //readonly object _timerListButtonsLock = new object();

        //private const double _keyCombinationTimerInterval = 500;

        //Dictionary<BrailleIODevice, System.Timers.Timer> keyCombinationTimerList
        //{
        //    get
        //    {
        //        lock (_timerListLock)
        //        {
        //            return _keyCombinationTimerList;
        //        }
        //    }
        //    set
        //    {
        //        lock (_timerListLock)
        //        {
        //            _keyCombinationTimerList = value;
        //        }
        //    }
        //}
        //Dictionary<System.Timers.Timer, KeyCombinationItem> keyCombinationTimerButtonList
        //{
        //    get
        //    {
        //        lock (_timerListButtonsLock)
        //        {
        //            return _keyCombinationTimerButtonList;
        //        }
        //    }
        //    set
        //    {
        //        lock (_timerListButtonsLock)
        //        {
        //            _keyCombinationTimerButtonList = value;
        //        }
        //    }
        //}

        //private void checkForKeyCombination(BrailleIODevice device,
        //    List<BrailleIO_DeviceButton> pressedGeneralKeys,
        //    List<String> pressedGenericKeys,
        //    List<BrailleIO_DeviceButton> releaesedGeneralKeys,
        //    List<String> releasedGenericKeys)
        //{
        //    if (keyCombinationTimerList.ContainsKey(device))
        //    {
        //        System.Timers.Timer t = keyCombinationTimerList[device];
        //        t.Stop();

        //        List<String> pGenButtonList = new List<String>();
        //        List<String> rGenButtonList = new List<String>();

        //        List<BrailleIO_DeviceButton> pGButtonList = new List<BrailleIO_DeviceButton>();
        //        List<BrailleIO_DeviceButton> rGButtonList = new List<BrailleIO_DeviceButton>();

        //        KeyCombinationItem kc;

        //        if (keyCombinationTimerButtonList.TryGetValue(t, out kc))
        //        {
        //            pGenButtonList = kc.PressedGenericKeys;
        //            pGButtonList = kc.PressedGeneralKeys;
        //            rGenButtonList = kc.ReleasedGenericKeys;
        //            rGButtonList = kc.ReleasedGeneralKeys;
        //        }
        //        else
        //        {
        //            kc = new KeyCombinationItem(pGButtonList, pGenButtonList, rGButtonList, rGenButtonList);
        //        }
        //        List<String> nrbl = rGenButtonList.Union(releasedGenericKeys).ToList();
        //        List<BrailleIO_DeviceButton> nrgbl = rGButtonList.Union(releaesedGeneralKeys).ToList();

        //        kc.PressedGenericKeys = pressedGenericKeys;
        //        kc.PressedGeneralKeys = pressedGeneralKeys;
        //        kc.ReleasedGenericKeys = nrbl;
        //        kc.ReleasedGeneralKeys = nrgbl;

        //        //System.Diagnostics.Debug.WriteLine("\t\t\tnew list: '" + String.Join(", ", nbl) + "'");
        //        if (keyCombinationTimerButtonList.ContainsKey(t))
        //        {
        //            keyCombinationTimerButtonList[t] = kc;
        //        }
        //        else
        //        {
        //            keyCombinationTimerButtonList.Add(t, kc);
        //        }

        //        if (pressedGenericKeys.Count < 1)
        //        {
        //            t_Elapsed(t, null);
        //        }
        //        else
        //        {
        //            t.Start();
        //        }
        //    }
        //    else
        //    {
        //        System.Timers.Timer t = new System.Timers.Timer(_keyCombinationTimerInterval);
        //        keyCombinationTimerList.Add(device, t);
        //        keyCombinationTimerButtonList.Add(t, new KeyCombinationItem(pressedGeneralKeys, pressedGenericKeys, releaesedGeneralKeys, releasedGenericKeys));
        //        if (pressedGenericKeys.Count < 1)
        //        {
        //            t_Elapsed(t, null);
        //        }
        //        else
        //        {
        //            t.Elapsed += new ElapsedEventHandler(t_Elapsed);
        //            t.Start();
        //        }
        //    }
        //}

        //void t_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    System.Timers.Timer t = sender as System.Timers.Timer;
        //    if (t != null)
        //    {
        //        t.Stop();
        //        KeyCombinationItem kc;
        //        //try get the keys
        //        if (keyCombinationTimerButtonList.TryGetValue(t, out kc))
        //        {
        //            keyCombinationTimerButtonList.Remove(t);

        //            var device = keyCombinationTimerList.FirstOrDefault(x => x.Value == t).Key;
        //            if (device != null)
        //            {
        //                keyCombinationTimerList.Remove(device);
        //            }

        //            if (kc.ReleasedGenericKeys != null && kc.ReleasedGenericKeys.Count > 0)
        //            {
        //                // fireButtonCombinationReleasedEvent(device, kc.PressedGeneralKeys, kc.PressedGenericKeys, kc.ReleasedGeneralKeys, kc.ReleasedGenericKeys);
        //            }
        //        }
        //    }
        //}

        //#endregion

        #region Gesture Interpreter

        private void initalizeGestureRecognition(BrailleIODevice device)
        {
            // gesture recognizer registration for the device
            ITrackBlobs blobTracker = new BlobTracker(); // TODO: make the blob tracker changeable?!
            var gestureRecognizer = GetNewGestureRecognizer(blobTracker);// new GestureRecognizer(blobTracker);

            if (gestureRecognizer != null && blobTracker != null)
            {
                //int c = 0;

                // try to register blob tracker
                blobTrackers.AddOrUpdate(device, blobTracker,
                    (key, existingBlobTracker) =>
                    {
                        // If this delegate is invoked, then the key already exists.
                        return blobTracker;
                    }
                );

                //while (!blobTrackers.TryAdd(device, blobTracker) && c++ < 20) { Thread.Sleep(5); };
                //if (c > 19) throw new AccessViolationException("Cannot add blob tracker to dictionary - access denied");

                // try to register gesture recognizer
                gestureRecognizers.AddOrUpdate(device, gestureRecognizer,
                    (key, existingGestureRecognizer) =>
                    {
                        // If this delegate is invoked, then the key already exists.
                        if (existingGestureRecognizer != null)
                        {
                            existingGestureRecognizer.FinishEvaluation();
                        }
                        return gestureRecognizer;
                    }
                );


                IClassify[] classifieres = GetNewGestureClassifierInstances();
                if (classifieres != null && classifieres.Length > 0)
                {
                    foreach (var classifier in classifieres)
                    {
                        if (classifier != null) gestureRecognizer.AddClassifier(classifier);
                    }
                }

                ////while (!gestureRecognizers.TryAdd(device, gestureRecognizer) && c++ < 40) { Thread.Sleep(5); };
                ////if (c > 39) throw new AccessViolationException("Cannot add gesture recognizer to dictionary - access denied");

                //var multitouchClassifier = new MultitouchClassifier();
                //var tabClassifier = new TapClassifier();

                //gestureRecognizer.AddClassifier(tabClassifier);
                //gestureRecognizer.AddClassifier(multitouchClassifier);

                blobTracker.InitiateTracking();
            }
        }

        bool unregisterGestureRecognition(BrailleIODevice device)
        {
            int c = 0;
            ITrackBlobs trash;
            IRecognizeGestures trash2;
            while (c++ < 10 && blobTrackers.TryRemove(device, out trash)) ;
            while (c++ < 20 && gestureRecognizers.TryRemove(device, out trash2)) ;
            return true;
        }

        private static bool updateGestureRecognizer(BrailleIODevice device, BlobTracker oldBt, BlobTracker newBt)
        {
            return true;
        }

        readonly ConcurrentBag<BrailleIODevice> gesturingDevices = new ConcurrentBag<BrailleIODevice>();

        private void startGesture(
            BrailleIO_DeviceButton pressedKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenKeys, 
            BrailleIODevice device)
        {

            if(device != null)
            {
                BrailleIO.Structs.KeyCombinationItem kbi = new BrailleIO.Structs.KeyCombinationItem(
                    pressedKeys, BrailleIO_DeviceButton.None,
                    pressedBrailleKeyboardKeys, BrailleIO_BrailleKeyboardButton.None,
                    pressedAdditionalKeys, null
                    );

                if (kbi.AreButtonsPressed())
                {
                    string combination = kbi.PressedButtonsToString();
                    List<string> function = this.ButtoncombinationMapper.GetFunctionMapping(device, combination);

                    int gestOrMan = 0;

                    if (function != null && function.Count > 0)
                    {
                        if (function.Contains("Gesture")) gestOrMan = -1;
                        if (function.Contains("Manipulation")) gestOrMan = 1;
                    }

                    if (gestOrMan != 0) // gesture button is released
                    {
                        //start gesture recording
                        ITrackBlobs blobTracker;
                        blobTrackers.TryGetValue(device, out blobTracker);
                        IRecognizeGestures gestureRecognizer;
                        gestureRecognizers.TryGetValue(device, out gestureRecognizer);

                        if (blobTracker != null && gestureRecognizer != null)
                        {
                            if (gestOrMan < 0)
                            {
                                Mode |= InteractionMode.Gesture;
                            }
                            // FIXME: make this run without the proprietary key IDs for BrailleDis devices
                            else // manipulation
                            {
                                Mode |= InteractionMode.Manipulation;
                            }

                            if (!gesturingDevices.Contains(device)) gesturingDevices.Add(device);
                            startGestureTracking(blobTracker, gestureRecognizer);
                        }
                        else
                        {
                            initalizeGestureRecognition(device);
                        }
                    }
                }
            }

            //// left and right gesture buttons !!!!
            //if (pressedGenKeys.Contains("hbr") || pressedKeys.HasFlag(BrailleIO_DeviceButton.Gesture))
            //{
            //    // check if additional buttons are pressed?
            //    if (pressedBrailleKeyboardKeys != BrailleIO_BrailleKeyboardButton.None || 
            //        pressedBrailleKeyboardKeys != BrailleIO_BrailleKeyboardButton.Unknown)
            //        return;
            //    if (pressedAdditionalKeys != null && pressedAdditionalKeys.Length > 0)
            //    {
            //        foreach (var item in pressedAdditionalKeys)
            //            if (item != BrailleIO_AdditionalButton.None || 
            //                item != BrailleIO_AdditionalButton.Unknown)
            //                return;
            //    }

            //    //start gesture recording
            //    ITrackBlobs blobTracker;
            //    blobTrackers.TryGetValue(device, out blobTracker);
            //    IRecognizeGestures gestureRecognizer;
            //    gestureRecognizers.TryGetValue(device, out gestureRecognizer);

            //    // TODO: ask for function name !!!!!


            //    if (blobTracker != null && gestureRecognizer != null)
            //    {
            //        if (pressedKeys.HasFlag(BrailleIO_DeviceButton.Gesture))
            //        {
            //            Mode |= InteractionMode.Gesture;
            //        }
            //        // FIXME: make this run without the proprietary key IDs for BrailleDis devices
            //        else if (pressedGenKeys.Contains("hbr")) // manipulation
            //        {
            //            Mode |= InteractionMode.Manipulation;
            //        }
            //        else
            //        {
            //            return;
            //        }

            //        if (!gesturingDevices.Contains(device)) gesturingDevices.Add(device);
            //        startGestureTracking(blobTracker, gestureRecognizer);
            //    }
            //    else
            //    {
            //        initalizeGestureRecognition(device);
            //    }
            //}
        }

        private void startGestureTracking(ITrackBlobs blobTracker, IRecognizeGestures gestureRecognizer)
        {
            if (blobTracker != null && gestureRecognizer != null)
            {
                blobTracker.InitiateTracking();
                gestureRecognizer.StartEvaluation();
            }
        }

        private void endGesture(
            //            BrailleIO_DeviceButton releasedKeys,
            //BrailleIO_BrailleKeyboardButton releasedBrailleKeyboardKeys,
            //BrailleIO_AdditionalButton[] releasedAdditionalKeys,
            //            BrailleIO_DeviceButton pressedKeys,
            //BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            //BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            BrailleIODevice device,
            BrailleIO.Structs.KeyCombinationItem kbi,
            List<String> pressedGenKeys, 
            List<String> releasedGenKeys)
        {
            // TODO: ask for function name !!!!!
            // FIXME: make this run without the proprietary key IDs for BrailleDis devices

            if (device != null && kbi.AreButtonsReleased()) {

                string combination = kbi.ReleasedButtonsToString();
                List<string> function = this.ButtoncombinationMapper.GetFunctionMapping(device, combination);

                int gestOrMan = 0;

                if(function != null && function.Count > 0)
                {
                    if (function.Contains("Gesture")) gestOrMan = -1;
                    if (function.Contains("Manipulation")) gestOrMan = 1;
                }

                if (gestOrMan != 0) // gesture button is released
                {
                    if (!kbi.AreButtonsPressed()) // no other buttons are pressed
                    {

                        // FIXME: maybe release this state later (after handling the gesture)
                        //if(Mode.HasFlag(InteractionMode.Gesture) && gestOrMan < 0)
                        //{
                        //    Mode &= ~InteractionMode.Gesture;
                        //}
                        //if(Mode.HasFlag(InteractionMode.Manipulation) && gestOrMan > 0)
                        //{
                        //    Mode &= ~InteractionMode.Manipulation;
                        //}

                        if (gesturingDevices.Contains(device))
                        {
                            BrailleIODevice trash = device;
                            int i = 0;
                            while (!gesturingDevices.TryTake(out trash) && i++ < 10) { Thread.Sleep(5); }

                        }

                        IClassificationResult result = classifyGesture(device);
                        fireClassifiedGestureEvent(
                            result, device,
                            kbi.PressedGeneralKeys, kbi.PressedKeyboardKeys, kbi.PressedAdditionalKeys, pressedGenKeys,
                            kbi.ReleasedGeneralKeys, kbi.ReleasedKeyboardKeys, kbi.ReleasedAdditionalKeys, releasedGenKeys);

                        return;
                    }
                    else // other buttons are currently pressed 
                    {   // --> so gesture button was pressed accidentally?
                        // stop the gesture anyway
                        if (gesturingDevices.Contains(device))
                        {
                            BrailleIODevice trash = device;
                            int i = 0;
                            while (!gesturingDevices.TryTake(out trash) && i++ < 10) { Thread.Sleep(5); }
                            classifyGesture(trash); // stop evaluation
                        }
                    }
                }


            //    if (Mode.HasFlag(InteractionMode.Gesture)
            //        //&& releasedKeys.HasFlag(BrailleIO_DeviceButton.Gesture)
            //    //&& releasedGenKeys.Count == 1 && pressedGenKeys.Count == 0)
            //    )
            //{
            //        Mode &= ~InteractionMode.Gesture;
            //    }
            //    else if ((Mode & InteractionMode.Manipulation) == InteractionMode.Manipulation
            //        && releasedGenKeys.Contains("hbr")
            //        )
            //    {
            //        Mode &= ~InteractionMode.Manipulation;
            //    }
            //    else { return; }





                
            }
        }

        private void fireClassifiedGestureEvent(
            IClassificationResult result, 
            BrailleIODevice device,
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys,
            BrailleIO_DeviceButton releasedGeneralKeys,
            BrailleIO_BrailleKeyboardButton releasedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] releasedAdditionalKeys,
            List<String> releasedGenericKeys)
        {
            if (result != null)
            {
                System.Diagnostics.Debug.WriteLine("gesture recognized: " + result);
                Logger.Instance.Log(LogPriority.DEBUG, this, "[GESTURE] result " + result);
                fireGestureEvent(device, pressedGeneralKeys, pressedBrailleKeyboardKeys, pressedAdditionalKeys, pressedGenericKeys,
                    releasedGeneralKeys, releasedBrailleKeyboardKeys, releasedAdditionalKeys, releasedGenericKeys, result);
            }
        }

        private IClassificationResult classifyGesture(BrailleIODevice device)
        {
            IClassificationResult result = null;
            IRecognizeGestures gestureRecognizer;
            gestureRecognizers.TryGetValue(device, out gestureRecognizer);

            if (gestureRecognizer != null)
            {
                result = gestureRecognizer.FinishEvaluation(false);

            }
            return result;
        }



        private void handleTouchEvent(Object sender, BrailleIODevice brailleIODevice, BrailleIO_TouchValuesChanged_EventArgs brailleIO_TouchValuesChanged_EventArgs)
        {
            if (gesturingDevices.Contains(brailleIODevice)
                && ((Mode & InteractionMode.Gesture) == InteractionMode.Gesture
                || (Mode & InteractionMode.Manipulation) == InteractionMode.Manipulation))
            {
                ITrackBlobs blobTracker;
                blobTrackers.TryGetValue(brailleIODevice, out blobTracker);

                if (brailleIO_TouchValuesChanged_EventArgs != null && blobTracker != null)
                {
                    Frame f = null;
                    if (brailleIO_TouchValuesChanged_EventArgs.DetailedTouches != null &&
                        brailleIO_TouchValuesChanged_EventArgs.DetailedTouches.Count > 0)
                        f = new Frame(DateTime.Now, brailleIO_TouchValuesChanged_EventArgs.DetailedTouches.ToArray());
                    else
                        f = getFrameFromSampleSet(brailleIO_TouchValuesChanged_EventArgs.touches);
                    blobTracker.AddFrame(f);
                }
            }
        }

        private Frame getFrameFromSampleSet(double[,] sampleSet)
        {
            List<Touch> touchList = new List<Touch>();
            if (sampleSet != null)
            {
                var clusterer = new GestureRecognition.Clusterer(sampleSet.Length);
                var cluster = clusterer.Cluster(sampleSet, 0);

                foreach (var c in cluster.Values)
                {
                    //get blob extension in x and y axis
                    double cX = 1, cY = 1;
                    int count = 0;
                    double val = 0;
                    foreach (var m in c.ClusterSet.Values)
                    {
                        int x = m / sampleSet.GetLength(1);
                        int y = m % sampleSet.GetLength(1);
                        try
                        {
                            val += sampleSet[x, y];
                            count++;
                        }
                        catch { }
                        if (Math.Abs(x - c.Mean[0]) >= cX) cX = Math.Abs(x - c.Mean[0]);
                        if (Math.Abs(y - c.Mean[1]) >= cY) cY = Math.Abs(y - c.Mean[1]);
                    }
                    Touch t = new Touch(c.Id, c.Mean[0], c.Mean[1], cX, cY, val / count);
                    touchList.Add(t);
                }
            }
            Frame frame = new Frame(DateTime.Now, touchList.ToArray());
            return frame;
        }

        #endregion

        #region ILocalizable

        void ILocalizable.SetLocalizationCulture(System.Globalization.CultureInfo culture)
        {
            if (LL != null) LL.SetStandardCulture(culture);
        }

        #endregion

        #region Gesture Recognizer and Classifier Handling

        /// <summary>
        /// Adds a new type to the list of gesture classifier types.
        /// </summary>
        /// <param name="_type">The type to add - must implement the <see cref="IClassify"/>.</param>
        /// <param name="position">The position to add to.</param>
        /// <returns>The new array of registered classifier types.</returns>
        public static IClassifyActivation[] AddGestureClassifierType(IClassifyActivation _type, int position = Int32.MaxValue)
        {
            try
            {
                if (!gestureClassifierTypes.Contains(_type))
                {
                    if (position >= gestureClassifierTypes.Count)
                        gestureClassifierTypes.Add(_type);
                    else
                        gestureClassifierTypes.Insert(Math.Max(0, position), _type);

                    Logger.Instance.Log(LogPriority.DEBUG, "InteractionManager", "[NOTICE] " + _type.ToString() + " successfully added to classifier type list.");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(LogPriority.ALWAYS, "InteractionManager", "[FATAL ERROR] can't add classifier type to list.", ex);
            }

            return gestureClassifierTypes.ToArray();
        }

        /// <summary>
        /// Removes the specific type from the gesture classifier type list.
        /// </summary>
        /// <param name="_type">The type to remove.</param>
        /// <returns>The new array of registered classifier types.</returns>
        public static IClassifyActivation[] RemoveGestureClassifierType(IClassifyActivation _type)
        {
            try
            {
                if (gestureClassifierTypes.Contains(_type))
                {
                    gestureClassifierTypes.Remove(_type);
                    Logger.Instance.Log(LogPriority.DEBUG, "InteractionManager", "[NOTICE] " + _type.ToString() + " successfully removed from classifier type list.");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(LogPriority.ALWAYS, "InteractionManager", "[FATAL ERROR] can't remove classifier type from list.", ex);
            }

            return gestureClassifierTypes.ToArray();
        }

        /// <summary>
        /// Gets all registered gesture classifier types.
        /// </summary>
        /// <returns>Array of registered classifier types.</returns>
        public static IClassifyActivation[] GetGestureClassifierTypes()
        {
            return gestureClassifierTypes.ToArray();
        }

        /// <summary>
        /// Generates new instances for all registered gesture classifier types.
        /// </summary>
        /// <returns>Array of registered IClassify instances.</returns>
        public static IClassify[] GetNewGestureClassifierInstances()
        {
            lock (_classifierTypeLock)
            {
                if (gestureClassifierTypes != null && gestureClassifierTypes.Count > 0)
                {
                    IClassify[] classifiers = new IClassify[gestureClassifierTypes.Count];
                    for (int i = 0; i < gestureClassifierTypes.Count; i++)
                    {
                        try
                        {
                            var classifier = gestureClassifierTypes[i];
                            IClassify c = Activator.CreateInstance(classifier.ClassType, classifier.Params) as IClassify;
                            if (classifier.PostInitAct != null) classifier.PostInitAct(c);
                            classifiers[i] = c;
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Log(LogPriority.ALWAYS, "InteractionManager", "[FATAL ERROR] can't create instance of classifier type from list.", ex);
                        }
                    }
                    return classifiers;
                }
            }

            return null;
        }


        /// <summary>
        /// Gets the new gesture recognizer.
        /// </summary>
        /// <returns><see cref="IRecognizeGestures"/> instance of the registered type.</returns>
        static IRecognizeGestures GetNewGestureRecognizer(ITrackBlobs blobTracker)
        {
            if (GestureRecognizerType != null && blobTracker != null)
            {
                try
                {
                    IRecognizeGestures c = Activator.CreateInstance(GestureRecognizerType, new object[1] { blobTracker }) as IRecognizeGestures;
                    return c;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(LogPriority.ALWAYS, "InteractionManager", "[FATAL ERROR] can't create instance of gesture recognizer type.", ex);
                }
            }

            return null;
        }

        /// <summary>
        /// Refreshes the gesture recognizer to device assignments.
        /// </summary>
        /// <returns><c>true</c> if the refresh was successful.</returns>
        public bool RefreshGestureRecognition2DeviceAssignments()
        {
            foreach (var device in gestureRecognizers.Keys)
            {
                try
                {
                    if (device != null) initalizeGestureRecognition(device);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(LogPriority.ALWAYS, this, "[FATAL ERROR] Exception in refreshing gesture recognizer for device.", ex);
                }
            }
            return true;
        }


        #endregion

        #region Key-combination to Function Mapping

        /// <summary>
        /// Adds the buttons to function mapping definition XML to the global definition dictionary.
        /// </summary>
        /// <param name="mappingDefXML">The mapping definition XML string.</param>
        /// <returns><c>true</c> if the definitions are loaded correctly, <c>false</c> otherwise.</returns>
        public bool AddButton2FunctionMapping(string mappingDefXML)
        {
            try
            {
                ButtoncombinationMapper.LoadFunctionMapping(mappingDefXML, true);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(LogPriority.ALWAYS, this, "[ERROR]\tcould not load button to function mapping definition.\r\n", ex);
                return false;
            }           
            return true;
        }

        /// <summary>
        /// Adds the buttons to function mapping definition XML to the global definition dictionary.
        /// </summary>
        /// <param name="mappingDefXML">The mapping definition XML file path.</param>
        /// <returns><c>true</c> if the definitions are loaded correctly, <c>false</c> otherwise.</returns>
        public bool AddButton2FunctionMappingFile(string mappingDefXMLFilePath)
        {
            if(mappingDefXMLFilePath != null && File.Exists(mappingDefXMLFilePath))
            {
                return AddButton2FunctionMapping(File.ReadAllText(mappingDefXMLFilePath));
            }
            return false;
        }

        #endregion

    }

    #region enums

    /// <summary>
    /// Modes of the interaction manager
    /// </summary>
    [Flags]
    public enum InteractionMode
    {
        /// <summary>
        /// No mode is set
        /// </summary>
        None = 0,
        /// <summary>
        /// normal mode - all inputs will be forwarded
        /// </summary>
        Normal = 1,
        /// <summary>
        /// inputs will be interpreted as Braille keyboard inputs
        /// </summary>
        Braille = 2,
        /// <summary>
        /// Gesture mode was activated - touch inputs should be collected for a gesture interpretation
        /// </summary>
        Gesture = 4,
        /// <summary>
        /// Manipulation mode was activated - elements e.g. in a document will be manipulated dirctly by inputs 
        /// </summary>
        Manipulation = 8
    }

    /// <summary>
    /// types of objects that can be queued in the input queue
    /// </summary>
    public enum InteractionQueueObjectType
    {
        /// <summary>
        /// unknown entry
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// key was pressed
        /// </summary>
        ButtonPressed,
        /// <summary>
        /// status of key has changed
        /// </summary>
        ButtonStateChange,
        /// <summary>
        /// touch values changed
        /// </summary>
        Touch,
        /// <summary>
        /// an input was performed (???)
        /// </summary>
        InputChanged,
        /// <summary>
        /// an error has occurred
        /// </summary>
        Error,
        /// <summary>
        /// and button combination was released
        /// </summary>
        ButtonCombination
    }

    #endregion

    #region Structs

    /// <summary>
    /// Item in the input queue
    /// </summary>
    struct InteractionQueueItem
    {
        /// <summary>
        /// the timestamp of the creation or when the input happens
        /// </summary>
        public DateTime Timestamp;
        /// <summary>
        /// type indicator for this generic object
        /// </summary>
        public InteractionQueueObjectType Type;
        /// <summary>
        /// source device of the input
        /// </summary>
        public BrailleIO.BrailleIODevice Device;
        /// <summary>
        /// original raw data event arguments
        /// </summary>
        public EventArgs Args;
        /// <summary>
        /// sending <see cref="BrailleIO.Interface.IBrailleIOAdapter"/>
        /// </summary>
        public object Sender;
        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionQueueItem"/> struct.
        /// </summary>
        /// <param name="timestamp">The timestamp when the input happens.</param>
        /// <param name="type">The type of the input.</param>
        /// <param name="device">The device from which ii was sent.</param>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <param name="sender">The sending Adapter.</param>
        public InteractionQueueItem(DateTime timestamp, InteractionQueueObjectType type, BrailleIODevice device, EventArgs args, Object sender)
        {
            Timestamp = timestamp; Type = type; Device = device; Args = args; Sender = sender;
        }

        override public String ToString() { return "IAEQI " + Timestamp.ToString() + ": " + Type.ToString() + " from: " + Device.ToString() + " | " + Args.ToString(); }

    }

    /// <summary>
    /// Bundle of information for a key combination
    /// </summary>
    struct KeyCombinationItem
    {
        /// <summary>
        /// list of interpreted pressed keys 
        /// </summary>
        public List<String> PressedGenericKeys;
        /// <summary>
        /// list of interpreted released keys
        /// </summary>
        public List<String> ReleasedGenericKeys;
        /// <summary>
        /// list of pressed keys
        /// </summary>
        public List<BrailleIO_DeviceButton> PressedGeneralKeys;
        /// <summary>
        /// list of released keys
        /// </summary>
        public List<BrailleIO_DeviceButton> ReleasedGeneralKeys;
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyCombinationItem"/> struct.
        /// </summary>
        /// <param name="pressedGeneralKeys">The pressed general keys.</param>
        /// <param name="pressedGenericKeys">The pressed generic keys.</param>
        /// <param name="releasedGeneralKeys">The released general keys.</param>
        /// <param name="releasedGenericKeys">The released generic keys.</param>
        public KeyCombinationItem(List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys, List<BrailleIO_DeviceButton> releasedGeneralKeys, List<String> releasedGenericKeys)
        {
            this.PressedGenericKeys = pressedGenericKeys; this.PressedGeneralKeys = pressedGeneralKeys; this.ReleasedGenericKeys = releasedGenericKeys; this.ReleasedGeneralKeys = releasedGeneralKeys;
        }
    }

    #endregion

    /// <summary>
    /// Util class for time to timestamp converting
    /// </summary>
    static class TimeStampUtils
    {
        /// <summary>
        /// Converts a unix time stamp into a .Net DateTime object.
        /// </summary>
        /// <param name="unixTimeStamp">The unix time stamp.</param>
        /// <returns>The corresponding DateTime element to the given unix time stamp.</returns>
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        ///  Converts a java time stamp into a .Net DateTime object.
        /// </summary>
        /// <param name="javaTimeStamp">The java time stamp.</param>
        /// <returns>The corresponding DateTime element to the given java time stamp.</returns>
        public static DateTime JavaTimeStampToDateTime(double javaTimeStamp)
        {
            // Java timestamp is milliseconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(Math.Round(javaTimeStamp / 1000)).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// Converts a DateTime object into a unix timestamp
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>the corresponding unix time stamp</returns>
        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }
    }

    /// <summary>
    /// Event args for interaction mode change.
    /// </summary>
    public class InteractionModeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// interaction mode before change
        /// </summary>
        public readonly InteractionMode OldValue;
        /// <summary>
        /// current interaction mode
        /// </summary>
        public readonly InteractionMode NewValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionModeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public InteractionModeChangedEventArgs(InteractionMode oldValue, InteractionMode newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
    
    /// <summary>
    /// An struct for wrapping anonymous activations of new instances for gesture classifiers.
    /// </summary>
    public struct IClassifyActivation
    {
        /// <summary>
        /// The class type to instantiate.
        /// The type must implement the interface <see cref="IClassify"/>
        /// </summary>
        public readonly Type ClassType;

        /// <summary>
        /// The parameters for the constructor.
        /// </summary>
        public readonly object[] Params;

        /// <summary>
        /// An Action to be executed after creating a new instance.
        /// </summary>
        /// <param name="classifierInstance">The classifier instance.</param>
        public delegate void postInitAction(IClassify classifierInstance);

        /// <summary>
        /// The post initialize action to perform after activating a new instance.
        /// </summary>
        public readonly postInitAction PostInitAct;

        /// <summary>
        /// Initializes a new instance of the <see cref="IClassifyActivation"/> struct.
        /// </summary>
        /// <param name="_type">The type of the classifier.</param>
        /// <param name="_params">The parameters to use for calling the constructor of this class.</param>
        /// <param name="action">An action to be performed after creating a new instance.</param>
        /// <exception cref="System.ArgumentNullException">_type - The type of the classifier cannot be null</exception>
        /// <exception cref="System.ArgumentException">The type fro the classifier have to implement the IClassify interface. - _type</exception>
        public IClassifyActivation(Type _type, object[] _params = null, postInitAction action = null)
        {
            if (_type == null)
                throw new ArgumentNullException("_type", "The type of the classifier cannot be null");
            if (!(typeof(IClassify).IsAssignableFrom(_type)))
                throw new ArgumentException("The type for the classifier have to implement the IClassify interface.", "_type");

            ClassType = _type;
            Params = _params;
            PostInitAct = action;
        }
    }

    public class Button2FunctionProxy : AbstarctButton2FunctionProxyBase
    {
        public Button2FunctionProxy() : base()
        {

        }
    }

}