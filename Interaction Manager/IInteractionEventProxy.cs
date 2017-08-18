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
    }

    #region EventArgs
    /// <summary>
    /// Cancelable event for BrailleIO interactions
    /// </summary>
    public abstract class BrailleIOInteractionEventArgs : System.ComponentModel.CancelEventArgs
    {
        /// <summary>
        /// The sending device.
        /// </summary>
        /// <value>The device.</value>
        public BrailleIO.BrailleIODevice Device { get; protected set; }

        protected List<BrailleIO_DeviceButton> _pressedGeneralKeys = null;
        /// <summary>
        /// Gets or sets the pressed general keys.
        /// </summary>
        /// <value>The pressed general keys.</value>
        public List<BrailleIO_DeviceButton> PressedGeneralKeys
        {
            get { return _pressedGeneralKeys; }
            set
            {
                _pressedGeneralKeys = value;
                _pressedGeneralKeys.Sort();
            }
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
                _pressedGenericKeys.Sort();
            }
        }
        /// <summary>
        /// Gets or sets the timestamp the interaction occurs.
        /// </summary>
        /// <value>The timestamp.</value>
        public DateTime Timestamp { get; protected set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="BrailleIOInteractionEventArgs"/> class.
        /// </summary>
        /// <param name="device">The sending device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys.</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys.</param>
        public BrailleIOInteractionEventArgs(BrailleIO.BrailleIODevice device, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys) : this(DateTime.Now, device, pressedGeneralKeys, pressedGenericKeys) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="BrailleIOInteractionEventArgs"/> class.
        /// </summary>
        /// <param name="timestamp">The timestamp the interaction occurred.</param>
        /// <param name="device">The device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys.</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys.</param>
        public BrailleIOInteractionEventArgs(DateTime timestamp, BrailleIO.BrailleIODevice device, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys) { Timestamp = timestamp; Device = device; PressedGeneralKeys = pressedGeneralKeys; PressedGenericKeys = pressedGenericKeys; }
    }

    /// <summary>
    /// Extend the <see cref="BrailleIOInteractionEventArgs"/> with member field for released keys
    /// </summary>
    public class ButtonReleasedEventArgs : BrailleIOInteractionEventArgs
    {

        protected List<BrailleIO_DeviceButton> _releasedGeneralKeys = null;
        /// <summary>
        /// Gets or sets the released general keys.
        /// </summary>
        /// <value>The released general keys.</value>
        public List<BrailleIO_DeviceButton> ReleasedGeneralKeys
        {
            get { return _releasedGeneralKeys; }
            set
            {
                _releasedGeneralKeys = value;
                _releasedGeneralKeys.Sort();
            }
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
                _releasedGenericKeys.Sort();
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonReleasedEventArgs"/> class.
        /// </summary>
        /// <param name="device">The sending device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys.</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys.</param>
        /// <param name="releasedGeneralKeys">The released general keys.</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys.</param>
        public ButtonReleasedEventArgs(BrailleIO.BrailleIODevice device, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys, List<BrailleIO_DeviceButton> releasedGeneralKeys, List<String> releasedGenericKeys)
            : base(device, pressedGeneralKeys, pressedGenericKeys)
        {
            ReleasedGeneralKeys = releasedGeneralKeys;
            ReleasedGenericKeys = releasedGenericKeys;
        }
    }

    /// <summary>
    /// Event args for pressed buttons <see cref="BrailleIOInteractionEventArgs"/>
    /// </summary>
    public class ButtonPressedEventArgs : BrailleIOInteractionEventArgs
    {
        //TODO: maybe extend this
        public ButtonPressedEventArgs(BrailleIO.BrailleIODevice device, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys)
            : base(device, pressedGeneralKeys, pressedGenericKeys)
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

        /// <summary>
        /// Initializes a new instance of the <see cref="GestureEventArgs"/> class.
        /// </summary>
        /// <param name="device">The sending device.</param>
        /// <param name="pressedGeneralKeys">The pressed general keys.</param>
        /// <param name="pressedGenericKeys">The interpreted pressed generic keys.</param>
        /// <param name="releasedGeneralKeys">The released general keys.</param>
        /// <param name="releasedGenericKeys">The interpreted released generic keys.</param>
        /// <param name="gesture">The gesture classification result.</param>
        public GestureEventArgs(BrailleIO.BrailleIODevice device, List<BrailleIO_DeviceButton> pressedGeneralKeys, List<String> pressedGenericKeys,
            List<BrailleIO_DeviceButton> releasedGeneralKeys, List<String> releasedGenericKeys,
            IClassificationResult gesture)
            : base(device, pressedGeneralKeys, pressedGenericKeys, releasedGeneralKeys, releasedGenericKeys)
        { this.Gesture = gesture; }
    }
    #endregion


}
