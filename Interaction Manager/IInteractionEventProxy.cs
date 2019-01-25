using BrailleIO.Interface;
using Gestures.Recognition.Interfaces;
using System;
using System.Collections.Generic;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Interface for proxies forwarding interaction events
    /// </summary>
    public interface IInteractionEventProxy
    {
        /// <summary>
        /// Occurs when at leased one button was released.
        /// </summary>
        event EventHandler<ButtonReleasedEventArgs> ButtonReleased;
        /// <summary>
        /// Occurs when a button combination released (contains of at least one button).
        /// </summary>
        event EventHandler<ButtonReleasedEventArgs> ButtonCombinationReleased;
        /// <summary>
        /// Occurs when at leased one button was pressed.
        /// </summary>
        event EventHandler<ButtonPressedEventArgs> ButtonPressed;
        /// <summary>
        /// Occurs when a gesture was performed.
        /// </summary>
        event EventHandler<GestureEventArgs> GesturePerformed;

        /// <summary>Occurs when a specific function should be called after a user interaction.</summary>
        event EventHandler<FunctionCallInteractionEventArgs> FunctionCall;
    }

    #region EventArgs
    /// <summary>
    /// Cancelable event for BrailleIO interactions
    /// </summary>
    public abstract class BrailleIOInteractionEventArgs : System.ComponentModel.CancelEventArgs
    {
        #region Members

        /// <summary>
        /// The sending device.
        /// </summary>
        /// <value>The device.</value>
        public BrailleIO.BrailleIODevice Device { get; protected set; }

        protected BrailleIO_DeviceButton _pressedGeneralKeys = BrailleIO_DeviceButton.None;
        /// <summary>
        /// Gets or sets the pressed general keys.
        /// </summary>
        /// <value>The pressed general keys.</value>
        public BrailleIO_DeviceButton PressedGeneralKeys
        {
            get { return _pressedGeneralKeys; }
            set { _pressedGeneralKeys = value; }
        }

        protected BrailleIO_BrailleKeyboardButton _pressedBrailleKeyboardKeys = BrailleIO_BrailleKeyboardButton.None;
        /// <summary>
        /// Gets or sets the pressed general Braille-keyboard keys.
        /// </summary>
        /// <value>The pressed general Braille-keyboard keys.</value>
        public BrailleIO_BrailleKeyboardButton PressedBrailleKeyboardKeys
        {
            get { return _pressedBrailleKeyboardKeys; }
            set { _pressedBrailleKeyboardKeys = value; }
        }

        protected BrailleIO_AdditionalButton[] _pressedAdditionalKeys = null;
        /// <summary>
        /// Gets or sets the pressed additional general keys.
        /// </summary>
        /// <value>The pressed additional general keys.</value>
        public BrailleIO_AdditionalButton[] PressedAdditionalKeys
        {
            get { return _pressedAdditionalKeys; }
            set { _pressedAdditionalKeys = value; }
        }

        protected List<String> _pressedGenericKeys = null;
        /// <summary>
        /// Gets or sets the pressed generic keys.
        /// </summary>
        /// <value>The interpreted pressed generic keys.</value>
        public List<String> PressedGenericKeys
        {
            get { return _pressedGenericKeys; }
            set
            {
                _pressedGenericKeys = value;
                if (_pressedGenericKeys != null && _pressedGenericKeys.Count > 1)
                    _pressedGenericKeys.Sort();
            }
        }

        internal BrailleIO.Structs.KeyCombinationItem KeyCombinationItem = new BrailleIO.Structs.KeyCombinationItem();

        /// <summary>
        /// Gets or sets the timestamp the interaction occurs.
        /// </summary>
        /// <value>The timestamp.</value>
        public DateTime Timestamp { get; protected set; }

        #endregion

        #region Constructor

        /// <summary>Initializes a new instance of the <see cref="T:tud.mci.tangram.TangramLector.BrailleIOInteractionEventArgs"/> class.</summary>
        /// <param name="device">The sending device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        public BrailleIOInteractionEventArgs(
            BrailleIO.BrailleIODevice device,
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys) :
            this(DateTime.Now, device, pressedGeneralKeys,
                pressedBrailleKeyboardKeys, pressedAdditionalKeys, 
                pressedGenericKeys)
        { }
        /// <summary>
        /// Initializes a new instance of the <see cref="BrailleIOInteractionEventArgs"/> class.
        /// </summary>
        /// <param name="timestamp">The timestamp the interaction occurred.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        public BrailleIOInteractionEventArgs(
            DateTime timestamp,
            BrailleIO.BrailleIODevice device,
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys)
        {
            Timestamp = timestamp;
            Device = device;
            PressedGeneralKeys = pressedGeneralKeys;
            PressedBrailleKeyboardKeys = pressedBrailleKeyboardKeys;
            PressedAdditionalKeys = pressedAdditionalKeys;
            PressedGenericKeys = pressedGenericKeys;

            KeyCombinationItem = new BrailleIO.Structs.KeyCombinationItem(
                PressedGeneralKeys, BrailleIO_DeviceButton.None,
                PressedBrailleKeyboardKeys, BrailleIO_BrailleKeyboardButton.None,
                PressedAdditionalKeys, null
                );
        }

        #region KeyCombinationItem Function Wrapping

        /// <summary>
        /// Determines whether some currently pressed buttons are detected or not.
        /// </summary>
        /// <returns><c>true</c> if some pressed buttons are registered; otherwise, <c>false</c>.</returns>
        public bool AreButtonsPressed(){ return this.KeyCombinationItem.AreButtonsPressed(); }

        /// <summary>
        /// Determines whether some released buttons are detected or not.
        /// </summary>
        /// <returns><c>true</c> if some released buttons are registered; otherwise, <c>false</c>.</returns>
        public bool AreButtonsReleased() { return this.KeyCombinationItem.AreButtonsReleased(); }

        /// <summary>
        /// Returns a comma separated list of all currently pressed buttons.
        /// </summary>
        /// <returns>String of currently pressed buttons.</returns>
        public string PressedButtonsToString() { return this.KeyCombinationItem.PressedButtonsToString(); }


        /// <summary>
        /// Returns a comma separated list of all released buttons.
        /// </summary>
        /// <returns>String of released buttons.</returns>
        public string ReleasedButtonsToString() { return this.KeyCombinationItem.ReleasedButtonsToString(); }

        #endregion

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return String.Format("Released:'{2}' \tPressed:'{1}' \tDevice: {0}", Device.Name +"|"+Device.AdapterType, PressedButtonsToString(), ReleasedButtonsToString());                
        }

        #endregion
    }

    /// <summary>
    /// Extend the <see cref="BrailleIOInteractionEventArgs"/> with member field for released keys
    /// </summary>
    public class ButtonReleasedEventArgs : BrailleIOInteractionEventArgs
    {
        protected BrailleIO_DeviceButton _releasedGeneralKeys = BrailleIO_DeviceButton.None;
        /// <summary>
        /// Gets or sets the released general keys.
        /// </summary>
        /// <value>The released general keys.</value>
        public BrailleIO_DeviceButton ReleasedGeneralKeys
        {
            get { return _releasedGeneralKeys; }
            set { _releasedGeneralKeys = value; }
        }

        protected BrailleIO_BrailleKeyboardButton _releasedBrailleKeyboardKeys = BrailleIO_BrailleKeyboardButton.None;
        /// <summary>
        /// Gets or sets the pressed general Braille-keyboard keys.
        /// </summary>
        /// <value>The pressed general Braille-keyboard keys.</value>
        public BrailleIO_BrailleKeyboardButton ReleasedBrailleKeyboardKeys
        {
            get { return _releasedBrailleKeyboardKeys; }
            set { _releasedBrailleKeyboardKeys = value; }
        }

        protected BrailleIO_AdditionalButton[] _releasedAdditionalKeys = null;
        /// <summary>
        /// Gets or sets the pressed additional general keys.
        /// </summary>
        /// <value>The pressed additional general keys.</value>
        public BrailleIO_AdditionalButton[] ReleasedAdditionalKeys
        {
            get { return _releasedAdditionalKeys; }
            set { _releasedAdditionalKeys = value; }
        }

        protected List<String> _releasedGenericKeys = null;
        /// <summary>
        /// Gets or sets the released generic keys.
        /// </summary>
        /// <value>The interpreted released generic keys.</value>
        public List<String> ReleasedGenericKeys
        {
            get { return _releasedGenericKeys; }
            set
            {
                _releasedGenericKeys = value;
                if (_releasedGenericKeys != null && _releasedGenericKeys.Count > 1)
                    _releasedGenericKeys.Sort();
            }
        }

        /// <summary>Initializes a new instance of the <see cref="T:tud.mci.tangram.TangramLector.ButtonReleasedEventArgs"/> class.</summary>
        /// <param name="device">The sending device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        /// <param name="releasedGeneralKeys">The released general keys (Flag).</param>
        /// <param name="releasedBrailleKeyboardKeys">The released braille keyboard keys (Flag).</param>
        /// <param name="releasedAdditionalKeys">An array of released additional keys (Flag).</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys (proprietary).</param>
        public ButtonReleasedEventArgs(
            BrailleIO.BrailleIODevice device,
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys,
            BrailleIO_DeviceButton releasedGeneralKeys,
            BrailleIO_BrailleKeyboardButton releasedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] releasedAdditionalKeys,
            List<String> releasedGenericKeys)
            : base(device, pressedGeneralKeys, pressedBrailleKeyboardKeys, pressedAdditionalKeys, pressedGenericKeys)
        {
            ReleasedGeneralKeys = releasedGeneralKeys;
            ReleasedBrailleKeyboardKeys = releasedBrailleKeyboardKeys;
            ReleasedAdditionalKeys = releasedAdditionalKeys;
            ReleasedGenericKeys = releasedGenericKeys;


            KeyCombinationItem = new BrailleIO.Structs.KeyCombinationItem(
                PressedGeneralKeys, ReleasedGeneralKeys,
                PressedBrailleKeyboardKeys, ReleasedBrailleKeyboardKeys,
                PressedAdditionalKeys, ReleasedAdditionalKeys
                );
        }

        /// <summary>Initializes a new instance of the <see cref="T:tud.mci.tangram.TangramLector.ButtonReleasedEventArgs"/> class.</summary>
        /// <param name="device">The sending device.</param>
        /// <param name="keyCombination">The complex key combination item, sent by BrailleIO framework.</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys (proprietary).</param>
        public ButtonReleasedEventArgs(
            BrailleIO.BrailleIODevice device,
            BrailleIO.Structs.KeyCombinationItem keyCombination,
            List<String> pressedGenericKeys,
            List<String> releasedGenericKeys)
            : base(device, keyCombination.PressedGeneralKeys, keyCombination.PressedKeyboardKeys, keyCombination.PressedAdditionalKeys, pressedGenericKeys)
        {
            ReleasedGeneralKeys = keyCombination.ReleasedGeneralKeys;
            ReleasedBrailleKeyboardKeys = keyCombination.ReleasedKeyboardKeys;
            ReleasedAdditionalKeys = keyCombination.ReleasedAdditionalKeys;
            ReleasedGenericKeys = releasedGenericKeys;
            KeyCombinationItem = keyCombination;
        }
    }

    /// <summary>
    /// Event args for pressed buttons <see cref="BrailleIOInteractionEventArgs"/>
    /// </summary>
    public class ButtonPressedEventArgs : BrailleIOInteractionEventArgs
    {
        //TODO: maybe extend this
        /// <summary>Initializes a new instance of the <see cref="T:tud.mci.tangram.TangramLector.ButtonPressedEventArgs"/> class.</summary>
        /// <param name="device">The sending device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        public ButtonPressedEventArgs(
            BrailleIO.BrailleIODevice device, 
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys)
            : base(device, pressedGeneralKeys, pressedBrailleKeyboardKeys, pressedAdditionalKeys, pressedGenericKeys)
        { }
    }

    /// <summary>
    /// extend the <see cref="ButtonReleasedEventArgs"/> event args with members for gestures.
    /// </summary>
    public class GestureEventArgs : ButtonReleasedEventArgs
    {
        /// <summary>
        /// gesture classification result
        /// </summary>
        public IClassificationResult Gesture;

        /// <summary>Initializes a new instance of the <see cref="T:tud.mci.tangram.TangramLector.GestureEventArgs"/> class.</summary>
        /// <param name="device">The sending device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        /// <param name="releasedGeneralKeys">The released general keys (Flag).</param>
        /// <param name="releasedBrailleKeyboardKeys">The released braille keyboard keys (Flag).</param>
        /// <param name="releasedAdditionalKeys">An array of released additional keys (Flag).</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys (proprietary).</param>
        /// <param name="gesture">The gesture classification result.</param>
        public GestureEventArgs(
            BrailleIO.BrailleIODevice device,
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys,
            BrailleIO_DeviceButton releasedGeneralKeys,
            BrailleIO_BrailleKeyboardButton releasedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] releasedAdditionalKeys,
            List<String> releasedGenericKeys,
            IClassificationResult gesture)
            : base(
                  device, pressedGeneralKeys, pressedBrailleKeyboardKeys, pressedAdditionalKeys, pressedGenericKeys, 
                  releasedGeneralKeys, releasedBrailleKeyboardKeys, releasedAdditionalKeys, releasedGenericKeys)
        { this.Gesture = gesture; }
    }
    
    /// <summary>Event arguments for an interaction event that should call a specific function.
    /// extends the <see cref="tud.mci.tangram.TangramLector.ButtonReleasedEventArgs"/></summary>
    public class FunctionCallInteractionEventArgs : ButtonReleasedEventArgs
    {
        /// <summary>
        /// The name of the function to be called.
        /// </summary>
        public string Function { get; private set; }

        /// <summary>
        /// Flag determining if this event/function was already handled or not. 
        /// Set this flag after handling the requested function.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>Initializes a new instance of the <see cref="T:tud.mci.tangram.TangramLector.Interaction_Manager.FunctionCallInteractionEventArgs"/> class.</summary>
        /// <param name="functionName">Name of the function to be called.</param>
        /// <param name="device">The sending device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys (Flag).</param>
        /// <param name="pressedBrailleKeyboardKeys">The pressed braille keyboard keys (Flag).</param>
        /// <param name="pressedAdditionalKeys">An array of pressed additional keys (Flag).</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        /// <param name="releasedGeneralKeys">The released general keys (Flag).</param>
        /// <param name="releasedBrailleKeyboardKeys">The released braille keyboard keys (Flag).</param>
        /// <param name="releasedAdditionalKeys">An array of released additional keys (Flag).</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys (proprietary).</param>
        public FunctionCallInteractionEventArgs(
            string functionName,
            BrailleIO.BrailleIODevice device,
            BrailleIO_DeviceButton pressedGeneralKeys,
            BrailleIO_BrailleKeyboardButton pressedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] pressedAdditionalKeys,
            List<String> pressedGenericKeys,
            BrailleIO_DeviceButton releasedGeneralKeys,
            BrailleIO_BrailleKeyboardButton releasedBrailleKeyboardKeys,
            BrailleIO_AdditionalButton[] releasedAdditionalKeys,
            List<String> releasedGenericKeys)
            : base(device,
                  pressedGeneralKeys, pressedBrailleKeyboardKeys, pressedAdditionalKeys, pressedGenericKeys,
                  releasedGeneralKeys, releasedBrailleKeyboardKeys, releasedAdditionalKeys, releasedGenericKeys)
        {
            Function = functionName;
        }

        /// <summary>Initializes a new instance of the <see cref="T:tud.mci.tangram.TangramLector.Interaction_Manager.FunctionCallInteractionEventArgs"/> class.</summary>
        /// <param name="functionName">Name of the function to be called.</param>
        /// <param name="device">The sending device.</param>
        /// <param name="keyCombination">The complex key combination item, sent by BrailleIO framework.</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys (proprietary).</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys (proprietary).</param>
        public FunctionCallInteractionEventArgs(
            string functionName,
            BrailleIO.BrailleIODevice device,
            BrailleIO.Structs.KeyCombinationItem keyCombination,
            List<String> pressedGenericKeys,
            List<String> releasedGenericKeys)
            : base(device, keyCombination, pressedGenericKeys, releasedGenericKeys)
        {
            Function = functionName;
        }
    }
    
    #endregion


}
