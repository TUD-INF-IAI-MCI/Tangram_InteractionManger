using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using BrailleIO;
using BrailleIO.Interface;
using Gestures.Recognition;
using Gestures.Recognition.GestureData;
using Gestures.Recognition.Interfaces;
using Gestures.Recognition.Preprocessing;
using tud.mci.LanguageLocalization;
using tud.mci.tangram.TangramLector.Control;
using tud.mci.tangram.audio;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Interprets and forwards input interaction from <see cref="BrailleIO.Interface.IBrailleIOAdapter"/>
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

        readonly ConcurrentDictionary<BrailleIODevice, BlobTracker> blobTrackers = new ConcurrentDictionary<BrailleIODevice, BlobTracker>();
        readonly ConcurrentDictionary<BrailleIODevice, GestureRecognizer> gestureRecognizers = new ConcurrentDictionary<BrailleIODevice, GestureRecognizer>();

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
        #endregion

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

        #region Constructor / Destructor

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionManager"/> class.
        /// </summary>
        private InteractionManager()
        {
            createInputQueueThread();
            this.ButtonReleased += new EventHandler<ButtonReleasedEventArgs>(InteractionManager_ButtonReleased);
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

        #region Input queue

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
            List<String> pressedGenKeys = new List<String>();
            List<BrailleIO_DeviceButton> pressedKeys = new List<BrailleIO_DeviceButton>();
            var mediator = BrailleIOButtonMediatorFactory.GetMediator(sender as BrailleIO.Interface.IBrailleIOAdapter);
            if (mediator != null)
            {
                pressedGenKeys = mediator.GetAllPressedGenericButtons(brailleIO_KeyPressed_EventArgs);
                pressedKeys = mediator.GetAllPressedGeneralButtons(brailleIO_KeyPressed_EventArgs);
            }
            if ((pressedKeys != null && pressedKeys.Count > 0) || (pressedGenKeys != null && pressedGenKeys.Count > 0)) fireButtonPressedEvent(brailleIODevice, pressedKeys, pressedGenKeys);

            startGesture(pressedKeys, pressedGenKeys, brailleIODevice);
        }

        private void handleKeyStateChangedEvent(Object sender, BrailleIODevice brailleIODevice, BrailleIO_KeyStateChanged_EventArgs brailleIO_KeyStateChanged_EventArgs)
        {
            List<String> pressedGenKeys = new List<String>();
            List<BrailleIO_DeviceButton> pressedKeys = new List<BrailleIO_DeviceButton>();
            List<String> releasedGenKeys = new List<String>();
            List<BrailleIO_DeviceButton> releasedKeys = new List<BrailleIO_DeviceButton>();

            var mediator = BrailleIOButtonMediatorFactory.GetMediator(sender as BrailleIO.Interface.IBrailleIOAdapter);
            if (mediator != null)
            {
                pressedGenKeys = mediator.GetAllPressedGenericButtons(brailleIO_KeyStateChanged_EventArgs);
                pressedKeys = mediator.GetAllPressedGeneralButtons(brailleIO_KeyStateChanged_EventArgs);
                releasedGenKeys = mediator.GetAllReleasedGenericButtons(brailleIO_KeyStateChanged_EventArgs);
                releasedKeys = mediator.GetAllReleasedGeneralButtons(brailleIO_KeyStateChanged_EventArgs);
            }
            if ((pressedKeys != null && pressedKeys.Count > 0) || (pressedGenKeys != null && pressedGenKeys.Count > 0)) fireButtonPressedEvent(brailleIODevice, pressedKeys, pressedGenKeys);
            if ((releasedKeys != null && releasedKeys.Count > 0) || (releasedGenKeys != null && releasedGenKeys.Count > 0)) fireButtonReleasedEvent(brailleIODevice, pressedKeys, pressedGenKeys, releasedKeys, releasedGenKeys);

            startGesture(pressedKeys, pressedGenKeys, brailleIODevice);
            endGesture(releasedKeys, releasedGenKeys, pressedKeys, pressedGenKeys, brailleIODevice);

        }

        #endregion

        #region Event handlers

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
        void InteractionManager_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e != null)
            {
                checkForKeyCombination(e.Device, e.PressedGeneralKeys, e.PressedGenericKeys, e.ReleasedGeneralKeys, e.ReleasedGenericKeys);
            }
        }

        #endregion

        #region fire events

        /// <summary>
        /// Fires the button released event.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys.</param>
        /// <param name="pressedGenericKeys">The pressed generic keys.</param>
        /// <param name="releasedGeneralKeys">The released general keys.</param>
        /// <param name="releasedGenericKeys">The released generic keys.</param>
        /// <returns></returns>
        protected bool fireButtonReleasedEvent(BrailleIO.BrailleIODevice device, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys, List<BrailleIO_DeviceButton> releasedGeneralKeys, List<String> releasedGenericKeys)
        {
            var args = new ButtonReleasedEventArgs(device, pressedGeneralKeys, pressedGenericKeys, releasedGeneralKeys, releasedGenericKeys);
            Logger.Instance.Log(LogPriority.OFTEN, this, "Button released: " + String.Join(",", args.ReleasedGenericKeys) + " pressed: " + String.Join(",", args.PressedGenericKeys));
            bool cancel = base.fireButtonReleasedEvent(args);
            if (cancel) { System.Diagnostics.Debug.WriteLine("InteractionManager Event canceled"); }
            return cancel;
        }

        /// <summary>
        /// Fires the button combination released event.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys.</param>
        /// <param name="pressedGenericKeys">The pressed generic keys.</param>
        /// <param name="releasedGeneralKeys">The released general keys.</param>
        /// <param name="releasedGenericKeys">The released generic keys.</param>
        /// <returns></returns>
        protected bool fireButtonCombinationReleasedEvent(BrailleIO.BrailleIODevice device, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys, List<BrailleIO_DeviceButton> releasedGeneralKeys, List<String> releasedGenericKeys)
        {
            var args = new ButtonReleasedEventArgs(device, pressedGeneralKeys, pressedGenericKeys, releasedGeneralKeys, releasedGenericKeys);
            Logger.Instance.Log(LogPriority.OFTEN, this, "Button combination released: " + String.Join(",", args.ReleasedGenericKeys) + " pressed: " + String.Join(",", args.PressedGenericKeys));
            bool cancel = base.fireButtonCombinationReleasedEvent(args);
            if (cancel) { System.Diagnostics.Debug.WriteLine("InteractionManager Event canceled"); }
            return cancel;
        }

        /// <summary>
        /// Fires the button pressed event.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys.</param>
        /// <param name="pressedGenericKeys">The pressed generic keys.</param>
        /// <returns></returns>
        protected bool fireButtonPressedEvent(BrailleIO.BrailleIODevice device, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys)
        {
            var args = new ButtonPressedEventArgs(device, pressedGeneralKeys, pressedGenericKeys);
            Logger.Instance.Log(LogPriority.OFTEN, this, "Button pressed: " + String.Join(",", args.PressedGenericKeys));
            bool cancel = base.fireButtonPressedEvent(args);
            if (cancel) { System.Diagnostics.Debug.WriteLine("InteractionManager Event canceled"); }
            return cancel;
        }

        /// <summary>
        /// Fires the gesture event.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="releasedGeneralKeys">The released general keys.</param>
        /// <param name="releasedGenericKeys">The released generic keys.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys.</param>
        /// <param name="pressedGenericKeys">The pressed generic keys.</param>
        /// <param name="gesture">The gesture.</param>
        /// <returns></returns>
        protected bool fireGestureEvent(BrailleIO.BrailleIODevice device, List<BrailleIO_DeviceButton> releasedGeneralKeys, List<String> releasedGenericKeys, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys, Gestures.Recognition.Interfaces.IClassificationResult gesture)
        {
            var args = new GestureEventArgs(device, pressedGeneralKeys, pressedGenericKeys, releasedGeneralKeys, releasedGenericKeys, gesture);
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
                catch (Exception)
                {
                }
            }
        }

        #endregion

        #region Key Combination Interpreter

        Dictionary<BrailleIODevice, System.Timers.Timer> _keyCombinationTimerList = new Dictionary<BrailleIODevice, System.Timers.Timer>();
        Dictionary<System.Timers.Timer, KeyCombinationItem> _keyCombinationTimerButtonList = new Dictionary<System.Timers.Timer, KeyCombinationItem>();

        readonly object _timerListLock = new object();
        readonly object _timerListButtonsLock = new object();

        private const double _keyCombinationTimerInterval = 500;

        Dictionary<BrailleIODevice, System.Timers.Timer> keyCombinationTimerList
        {
            get
            {
                lock (_timerListLock)
                {
                    return _keyCombinationTimerList;
                }
            }
            set
            {
                lock (_timerListLock)
                {
                    _keyCombinationTimerList = value;
                }
            }
        }
        Dictionary<System.Timers.Timer, KeyCombinationItem> keyCombinationTimerButtonList
        {
            get
            {
                lock (_timerListButtonsLock)
                {
                    return _keyCombinationTimerButtonList;
                }
            }
            set
            {
                lock (_timerListButtonsLock)
                {
                    _keyCombinationTimerButtonList = value;
                }
            }
        }

        private void checkForKeyCombination(BrailleIODevice device, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys, List<BrailleIO_DeviceButton> releaesedGeneralKeys, List<String> releasedGenericKeys)
        {
            if (keyCombinationTimerList.ContainsKey(device))
            {
                System.Timers.Timer t = keyCombinationTimerList[device];
                t.Stop();

                List<String> pGenButtonList = new List<String>();
                List<String> rGenButtonList = new List<String>();

                List<BrailleIO_DeviceButton> pGButtonList = new List<BrailleIO_DeviceButton>();
                List<BrailleIO_DeviceButton> rGButtonList = new List<BrailleIO_DeviceButton>();

                KeyCombinationItem kc;

                if (keyCombinationTimerButtonList.TryGetValue(t, out kc))
                {
                    pGenButtonList = kc.PressedGenericKeys;
                    pGButtonList = kc.PressedGeneralKeys;
                    rGenButtonList = kc.ReleasedGenericKeys;
                    rGButtonList = kc.ReleasedGeneralKeys;
                }
                else
                {
                    kc = new KeyCombinationItem(pGButtonList, pGenButtonList, rGButtonList, rGenButtonList);
                }
                List<String> nrbl = rGenButtonList.Union(releasedGenericKeys).ToList();
                List<BrailleIO_DeviceButton> nrgbl = rGButtonList.Union(releaesedGeneralKeys).ToList();

                kc.PressedGenericKeys = pressedGenericKeys;
                kc.PressedGeneralKeys = pressedGeneralKeys;
                kc.ReleasedGenericKeys = nrbl;
                kc.ReleasedGeneralKeys = nrgbl;

                //System.Diagnostics.Debug.WriteLine("\t\t\tnew list: '" + String.Join(", ", nbl) + "'");
                if (keyCombinationTimerButtonList.ContainsKey(t))
                {
                    keyCombinationTimerButtonList[t] = kc;
                }
                else
                {
                    keyCombinationTimerButtonList.Add(t, kc);
                }

                if (pressedGenericKeys.Count < 1)
                {
                    t_Elapsed(t, null);
                }
                else
                {
                    t.Start();
                }
            }
            else
            {
                System.Timers.Timer t = new System.Timers.Timer(_keyCombinationTimerInterval);
                keyCombinationTimerList.Add(device, t);
                keyCombinationTimerButtonList.Add(t, new KeyCombinationItem(pressedGeneralKeys, pressedGenericKeys, releaesedGeneralKeys, releasedGenericKeys));
                if (pressedGenericKeys.Count < 1)
                {
                    t_Elapsed(t, null);
                }
                else
                {
                    t.Elapsed += new ElapsedEventHandler(t_Elapsed);
                    t.Start();
                }
            }
        }

        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            System.Timers.Timer t = sender as System.Timers.Timer;
            if (t != null)
            {
                t.Stop();
                KeyCombinationItem kc;
                //try get the keys
                if (keyCombinationTimerButtonList.TryGetValue(t, out kc))
                {
                    keyCombinationTimerButtonList.Remove(t);

                    var device = keyCombinationTimerList.FirstOrDefault(x => x.Value == t).Key;
                    if (device != null)
                    {
                        keyCombinationTimerList.Remove(device);
                    }

                    if (kc.ReleasedGenericKeys != null && kc.ReleasedGenericKeys.Count > 0)
                    {
                        fireButtonCombinationReleasedEvent(device, kc.PressedGeneralKeys, kc.PressedGenericKeys, kc.ReleasedGeneralKeys, kc.ReleasedGenericKeys);
                    }
                }
            }
        }

        #endregion

        #region Gesture Interpreter

        private void initalizeGestureRecognition(BrailleIODevice device)
        {
            // gesture recognizer registration for the device
            var blobTracker = new BlobTracker();
            var gestureRecognizer = new GestureRecognizer(blobTracker);

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

            //while (!gestureRecognizers.TryAdd(device, gestureRecognizer) && c++ < 40) { Thread.Sleep(5); };
            //if (c > 39) throw new AccessViolationException("Cannot add gesture recognizer to dictionary - access denied");

            var multitouchClassifier = new MultitouchClassifier();
            var tabClassifier = new TapClassifier();

            gestureRecognizer.AddClassifier(tabClassifier);
            gestureRecognizer.AddClassifier(multitouchClassifier);

            blobTracker.InitiateTracking();
        }

        bool unregisterGestureRecognition(BrailleIODevice device)
        {
            int c = 0;
            BlobTracker trash;
            GestureRecognizer trash2;
            while (c++ < 10 && blobTrackers.TryRemove(device, out trash)) ;
            while (c++ < 20 && gestureRecognizers.TryRemove(device, out trash2)) ;
            return true;
        }

        private static bool updateGestureRecognizer(BrailleIODevice device, BlobTracker oldBt, BlobTracker newBt)
        {
            return true;
        }

        readonly ConcurrentBag<BrailleIODevice> gesturingDevices = new ConcurrentBag<BrailleIODevice>();

        private void startGesture(List<BrailleIO_DeviceButton> pressedKeys, List<String> pressedGenKeys, BrailleIODevice device)
        {
            if (pressedKeys != null && pressedGenKeys.Count < 2
                && (pressedGenKeys.Contains("hbr") || pressedKeys.Contains(BrailleIO_DeviceButton.Gesture)))
            {
                //start gesture recording
                BlobTracker blobTracker;
                blobTrackers.TryGetValue(device, out blobTracker);
                GestureRecognizer gestureRecognizer;
                gestureRecognizers.TryGetValue(device, out gestureRecognizer);

                if (blobTracker != null && gestureRecognizer != null)
                {
                    if (pressedKeys.Contains(BrailleIO_DeviceButton.Gesture))
                    {
                        Mode |= InteractionMode.Gesture;
                    }
                    else if (pressedGenKeys.Contains("hbr")) // manipulation
                    {
                        Mode |= InteractionMode.Manipulation;
                    }
                    else
                    {
                        return;
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

        private void startGestureTracking(BlobTracker blobTracker, GestureRecognizer gestureRecognizer)
        {
            if (blobTracker != null && gestureRecognizer != null)
            {
                blobTracker.InitiateTracking();
                gestureRecognizer.StartEvaluation();
            }
        }

        private void endGesture(List<BrailleIO_DeviceButton> releasedKeys, List<String> releasedGenKeys, List<BrailleIO_DeviceButton> pressedKeys, List<String> pressedGenKeys, BrailleIODevice device)
        {
            if ((Mode & InteractionMode.Gesture) == InteractionMode.Gesture
                && releasedKeys.Contains(BrailleIO_DeviceButton.Gesture)
                && releasedGenKeys.Count == 1 && pressedGenKeys.Count == 0)
            {
                Mode &= ~InteractionMode.Gesture;
            }
            else if ((Mode & InteractionMode.Manipulation) == InteractionMode.Manipulation
                && releasedGenKeys.Contains("hbr")
                )
            {
                Mode &= ~InteractionMode.Manipulation;
            }
            else { return; }

            if (gesturingDevices.Contains(device))
            {
                BrailleIODevice trash = device;
                int i = 0;
                while (!gesturingDevices.TryTake(out trash) && i++ < 10) { Thread.Sleep(5); }
            }

            IClassificationResult result = classifyGesture(device);
            fireClassifiedGestureEvent(result, device, releasedKeys, releasedGenKeys, pressedKeys, pressedGenKeys);
        }

        private void fireClassifiedGestureEvent(IClassificationResult result, BrailleIODevice device, List<BrailleIO_DeviceButton> releasedKeys, List<String> releasedGenKeys, List<BrailleIO_DeviceButton> pressedKeys, List<String> pressedGenKeys)
        {
            if (result != null)
            {
                System.Diagnostics.Debug.WriteLine("gesture recognized: " + result);
                Logger.Instance.Log(LogPriority.DEBUG, this, "[GESTURE] result " + result);
                fireGestureEvent(device, releasedKeys, releasedGenKeys, pressedKeys, pressedGenKeys, result);
            }
        }

        private IClassificationResult classifyGesture(BrailleIODevice device)
        {
            IClassificationResult result = null;
            GestureRecognizer gestureRecognizer;
            gestureRecognizers.TryGetValue(device, out gestureRecognizer);

            if (gestureRecognizer != null)
            {
                result = gestureRecognizer.FinishEvaluation();
            }
            return result;
        }

        private void handleTouchEvent(Object sender, BrailleIODevice brailleIODevice, BrailleIO_TouchValuesChanged_EventArgs brailleIO_TouchValuesChanged_EventArgs)
        {

            if (gesturingDevices.Contains(brailleIODevice)
                && ((Mode & InteractionMode.Gesture) == InteractionMode.Gesture
                || (Mode & InteractionMode.Manipulation) == InteractionMode.Manipulation))
            {
                BlobTracker blobTracker;
                blobTrackers.TryGetValue(brailleIODevice, out blobTracker);

                if (brailleIO_TouchValuesChanged_EventArgs != null && blobTracker != null)
                {
                    Frame f = getFrameFromSampleSet(brailleIO_TouchValuesChanged_EventArgs.touches);
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
        Error
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

}