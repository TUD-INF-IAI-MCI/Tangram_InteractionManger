using BrailleIO;
using BrailleIO.Interface;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Abstract basic implementation for an <see cref="IBrailleIOButtonMediator"/>.
    /// </summary>
    public abstract class AbstractBrailleIOButtonMediatorBase : IBrailleIOButtonMediator
    {
        #region Members
        /// <summary>
        /// The device
        /// </summary>
        protected BrailleIODevice device;
        /// <summary>
        /// List of last generic keys pressed
        /// </summary>
        protected List<String> lastGenericPressedkeys = new List<String>();
        /// <summary>
        /// List of last generic keys released
        /// </summary>
        protected List<String> releasedGenericPressedkeys = new List<String>();
        /// <summary>
        // List of last general keys pressed
        /// </summary>
        protected List<BrailleIO_DeviceButton> lastGeneralPressedkeys = new List<BrailleIO_DeviceButton>();
        /// <summary>
        /// List of last general keys released
        /// </summary>
        protected List<BrailleIO_DeviceButton> releasedGeneralPressedkeys = new List<BrailleIO_DeviceButton>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractBrailleIOButtonMediatorBase"/> class.
        /// </summary>
        public AbstractBrailleIOButtonMediatorBase() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractBrailleIOButtonMediatorBase"/> class.
        /// </summary>
        /// <param name="device">The related device to this mediator.</param>
        public AbstractBrailleIOButtonMediatorBase(BrailleIODevice device) { this.device = device; }

        #endregion

        /// <summary>
        /// Sets the related device to this mediator.
        /// </summary>
        /// <param name="device">The device.</param>
        public void SetDevice(BrailleIODevice device) { this.device = device; }

        #region General Buttons

        const int generalUpFilter = 174762; // (int)BrailleIO_DeviceButtonStates.AbortUp + (int)BrailleIO_DeviceButtonStates.DownUp + (int)BrailleIO_DeviceButtonStates.EnterUp + (int)BrailleIO_DeviceButtonStates.GestureUp + (int)BrailleIO_DeviceButtonStates.LeftUp + (int)BrailleIO_DeviceButtonStates.RightUp + (int)BrailleIO_DeviceButtonStates.UpUp + (int)BrailleIO_DeviceButtonStates.ZoomInUp + (int)BrailleIO_DeviceButtonStates.ZoomOutUp;
        const int generalDownFilter = 349524; //  (int)BrailleIO_DeviceButtonStates.AbortDown + (int)BrailleIO_DeviceButtonStates.DownDown + (int)BrailleIO_DeviceButtonStates.EnterDown + (int)BrailleIO_DeviceButtonStates.GestureDown + (int)BrailleIO_DeviceButtonStates.LeftDown + (int)BrailleIO_DeviceButtonStates.RightDown + (int)BrailleIO_DeviceButtonStates.UpDown + (int)BrailleIO_DeviceButtonStates.ZoomInDown + (int)BrailleIO_DeviceButtonStates.ZoomOutDown;
        
        /// <summary>Gets all pressed general buttons.</summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>a list of pressed general buttons (Flag)</returns>
        public virtual BrailleIO_DeviceButton GetAllPressedGeneralButtons(BrailleIO_DeviceButtonStates keys)
        {
            if ((keys == BrailleIO_DeviceButtonStates.None) || keys < 0) return BrailleIO_DeviceButton.None;
            int pressed = (int)keys & generalDownFilter;
            int binKeys = pressed >> 1;
            var pressedBinaryKeys = (BrailleIO_DeviceButton)binKeys;
            return pressedBinaryKeys;
        }

        /// <summary>Gets all pressed general buttons.</summary>
        /// <param name="keys">All currently pressed general keys.</param>
        /// <returns>a list of pressed general buttons (Flag)</returns>
        public virtual BrailleIO_DeviceButton GetAllPressedGeneralButtons(BrailleIO_DeviceButton keys)
        {
            return keys;
        }

        /// <summary>Gets all pressed general buttons.</summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of pressed general buttons (Flag)</returns>
        public virtual BrailleIO_DeviceButton GetAllPressedGeneralButtons(EventArgs args)
        {
            if (args != null && args is BrailleIO_KeyStateChanged_EventArgs)
            {
                BrailleIO_KeyStateChanged_EventArgs kscea = args as BrailleIO_KeyStateChanged_EventArgs;
                if (kscea.keyCode != BrailleIO_DeviceButtonStates.None)
                {
                    return GetAllPressedGeneralButtons(kscea.keyCode);
                }
            }
            return BrailleIO_DeviceButton.None;
        }

        /// <summary>Gets all released general buttons.</summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>a list of released general buttons (Flag)</returns>
        public virtual BrailleIO_DeviceButton GetAllReleasedGeneralButtons(BrailleIO_DeviceButtonStates keys)
        {
            if ((keys == BrailleIO_DeviceButtonStates.None) || keys < 0) return BrailleIO_DeviceButton.None;
            int released = (int)keys & generalUpFilter;
            var releasedBinaryKeys = (BrailleIO_DeviceButton)released;
            return releasedBinaryKeys;
        }

        /// <summary>Gets all released general buttons.</summary>
        /// <param name="keys">All currently released general keys.</param>
        /// <returns>a list of released general buttons (Flag)</returns>
        public virtual BrailleIO_DeviceButton GetAllReleasedGeneralButtons(BrailleIO_DeviceButton keys)
        {
            return keys;
        }

        /// <summary>Gets all released general buttons.</summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of released general buttons (Flag)</returns>
        public virtual BrailleIO_DeviceButton GetAllReleasedGeneralButtons(EventArgs args)
        {
            if (args != null && args is BrailleIO_KeyStateChanged_EventArgs)
            {
                BrailleIO_KeyStateChanged_EventArgs kscea = args as BrailleIO_KeyStateChanged_EventArgs;
                if (kscea.keyCode != BrailleIO_DeviceButtonStates.None)
                {
                    return GetAllReleasedGeneralButtons(kscea.keyCode);
                }
            }
            return BrailleIO_DeviceButton.None;
        }

        #endregion

        #region Braille Keyboard

        const int bkbUpFilter = 11184810; // (int)BrailleIO_BrailleKeyboardButtonStates.F11Up + (int)BrailleIO_BrailleKeyboardButtonStates.F1Up + (int)BrailleIO_BrailleKeyboardButtonStates.F22Up + (int)BrailleIO_BrailleKeyboardButtonStates.F2Up + (int)BrailleIO_BrailleKeyboardButtonStates.k1Up + (int)BrailleIO_BrailleKeyboardButtonStates.k2Up + (int)BrailleIO_BrailleKeyboardButtonStates.k3Up + (int)BrailleIO_BrailleKeyboardButtonStates.k4Up + (int)BrailleIO_BrailleKeyboardButtonStates.k5Up + (int)BrailleIO_BrailleKeyboardButtonStates.k6Up + (int)BrailleIO_BrailleKeyboardButtonStates.k7Up + (int)BrailleIO_BrailleKeyboardButtonStates.k8Up;
        const int bkbDownFilter = 22369620; // (int)BrailleIO_BrailleKeyboardButtonStates.F11Down + (int)BrailleIO_BrailleKeyboardButtonStates.F1Down + (int)BrailleIO_BrailleKeyboardButtonStates.F22Down + (int)BrailleIO_BrailleKeyboardButtonStates.F2Down + (int)BrailleIO_BrailleKeyboardButtonStates.k1Down + (int)BrailleIO_BrailleKeyboardButtonStates.k2Down + (int)BrailleIO_BrailleKeyboardButtonStates.k3Down + (int)BrailleIO_BrailleKeyboardButtonStates.k4Down + (int)BrailleIO_BrailleKeyboardButtonStates.k5Down + (int)BrailleIO_BrailleKeyboardButtonStates.k6Down + (int)BrailleIO_BrailleKeyboardButtonStates.k7Down + (int)BrailleIO_BrailleKeyboardButtonStates.k8Down;

        /// <summary>Gets all pressed Braille keyboard buttons.</summary>
        /// <param name="keys">All current Braille keyboard keys states.</param>
        /// <returns>a list of pressed Braille keyboard buttons (Flag)</returns>
        public virtual BrailleIO_BrailleKeyboardButton GetAllPressedBrailleKeyboardButtons(BrailleIO_BrailleKeyboardButtonStates keys)
        {
            if ((keys == BrailleIO_BrailleKeyboardButtonStates.None) || keys < 0) return BrailleIO_BrailleKeyboardButton.None;
            int pressed = (int)keys & bkbDownFilter;
            int binKeys = pressed >> 1;
            var pressedBinaryKeys = (BrailleIO_BrailleKeyboardButton)binKeys;
            return pressedBinaryKeys;
        }

        /// <summary>Gets all pressed Braille keyboard buttons.</summary>
        /// <param name="keys">All currently pressed Braille keyboard keys.</param>
        /// <returns>a list of pressed Braille keyboard buttons (Flag)</returns>
        public virtual BrailleIO_BrailleKeyboardButton GetAllPressedBrailleKeyboardButtons(BrailleIO_BrailleKeyboardButton keys)
        {
            return keys;
        }

        /// <summary>Gets all pressed Braille keyboard buttons.</summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of pressed Braille keyboard buttons (Flag)</returns>
        public virtual BrailleIO_BrailleKeyboardButton GetAllPressedBrailleKeyboardButtons(EventArgs args)
        {
            if (args != null && args is BrailleIO_KeyStateChanged_EventArgs)
            {
                BrailleIO_KeyStateChanged_EventArgs kscea = args as BrailleIO_KeyStateChanged_EventArgs;
                if (kscea.keyboardCode != BrailleIO_BrailleKeyboardButtonStates.None)
                {
                    return GetAllPressedBrailleKeyboardButtons(kscea.keyboardCode);
                }
            }
            return BrailleIO_BrailleKeyboardButton.None;
        }

        /// <summary>Gets all released Braille keyboard buttons.</summary>
        /// <param name="keys">All current Braille keyboard keys states.</param>
        /// <returns>a list of released Braille keyboard buttons (Flag)</returns>
        public virtual BrailleIO_BrailleKeyboardButton GetAllReleasedBrailleKeyboardButtons(BrailleIO_BrailleKeyboardButtonStates keys)
        {
            if ((keys == BrailleIO_BrailleKeyboardButtonStates.None) || keys < 0) return BrailleIO_BrailleKeyboardButton.None;
            int released = (int)keys & bkbUpFilter;
            var releasedBinaryKeys = (BrailleIO_BrailleKeyboardButton)released;
            return releasedBinaryKeys;
        }

        /// <summary>Gets all released Braille keyboard buttons.</summary>
        /// <param name="keys">All currently released Braille keyboard keys states.</param>
        /// <returns>a list of released Braille keyboard buttons (Flag)</returns>
        public virtual BrailleIO_BrailleKeyboardButton GetAllReleasedBrailleKeyboardButtons(BrailleIO_BrailleKeyboardButton keys)
        {
            return keys;
        }

        /// <summary>Gets all released Braille keyboard buttons.</summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of released Braille keyboard buttons (Flag)</returns>
        public virtual BrailleIO_BrailleKeyboardButton GetAllReleasedBrailleKeyboardButtons(EventArgs args)
        {
            if (args != null && args is BrailleIO_KeyStateChanged_EventArgs)
            {
                BrailleIO_KeyStateChanged_EventArgs kscea = args as BrailleIO_KeyStateChanged_EventArgs;
                if (kscea.keyboardCode != BrailleIO_BrailleKeyboardButtonStates.None)
                {
                    return GetAllReleasedBrailleKeyboardButtons(kscea.keyboardCode);
                }
            }
            return BrailleIO_BrailleKeyboardButton.None;
        }

        #endregion

        #region Additional Buttons

        const int addUpFilter = 715827882; //(int)BrailleIO_AdditionalButtonStates.fn1Up + (int)BrailleIO_AdditionalButtonStates.fn2Up + (int)BrailleIO_AdditionalButtonStates.fn3Up + (int)BrailleIO_AdditionalButtonStates.fn4Up + (int)BrailleIO_AdditionalButtonStates.fn5Up + (int)BrailleIO_AdditionalButtonStates.fn6Up + (int)BrailleIO_AdditionalButtonStates.fn7Up + (int)BrailleIO_AdditionalButtonStates.fn8Up + (int)BrailleIO_AdditionalButtonStates.fn9Up + (int)BrailleIO_AdditionalButtonStates.fn10Up + (int)BrailleIO_AdditionalButtonStates.fn11Up + (int)BrailleIO_AdditionalButtonStates.fn12Up + (int)BrailleIO_AdditionalButtonStates.fn13Up + (int)BrailleIO_AdditionalButtonStates.fn14Up + (int)BrailleIO_AdditionalButtonStates.fn15Up;
        const int addDownFilter = 1431655764; // (int)BrailleIO_AdditionalButtonStates.fn1Down + (int)BrailleIO_AdditionalButtonStates.fn2Down + (int)BrailleIO_AdditionalButtonStates.fn3Down + (int)BrailleIO_AdditionalButtonStates.fn4Down + (int)BrailleIO_AdditionalButtonStates.fn5Down + (int)BrailleIO_AdditionalButtonStates.fn6Down + (int)BrailleIO_AdditionalButtonStates.fn7Down + (int)BrailleIO_AdditionalButtonStates.fn8Down + (int)BrailleIO_AdditionalButtonStates.fn9Down + (int)BrailleIO_AdditionalButtonStates.fn10Down + (int)BrailleIO_AdditionalButtonStates.fn11Down + (int)BrailleIO_AdditionalButtonStates.fn12Down + (int)BrailleIO_AdditionalButtonStates.fn13Down + (int)BrailleIO_AdditionalButtonStates.fn14Down + (int)BrailleIO_AdditionalButtonStates.fn15Down;

        /// <summary>Gets all pressed additional buttons.</summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>An array of lists of pressed additional buttons (Flag)</returns>
        public virtual BrailleIO_AdditionalButton[] GetAllPressedAdditionalButtons(BrailleIO_AdditionalButtonStates[] keys)
        {
            if(keys != null && keys.Length > 0)
            {
                BrailleIO_AdditionalButton[] result = new BrailleIO_AdditionalButton[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    BrailleIO_AdditionalButtonStates item = keys[i];
                    if ((item == BrailleIO_AdditionalButtonStates.None) || item < 0)
                    {
                        result[i] = BrailleIO_AdditionalButton.None;
                        continue;
                    }
                    int pressed = (int)item & addDownFilter;
                    int binKeys = pressed >> 1;
                    var pressedBinaryKeys = (BrailleIO_AdditionalButton)binKeys;
                    result[i] = pressedBinaryKeys;
                }
                return result;
            }
            return null;
        }

        /// <summary>Gets all pressed additional buttons.</summary>
        /// <param name="keys">All currently pressed additional keys states.</param>
        /// <returns>An array of lists of pressed additional buttons (Flag)</returns>
        public virtual BrailleIO_AdditionalButton[] GetAllPressedAdditionalButtons(BrailleIO_AdditionalButton[] keys)
        {
            return keys;
        }

        /// <summary>Gets all pressed general buttons.</summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>An array of lists of pressed additional buttons (Flag)</returns>
        public virtual BrailleIO_AdditionalButton[] GetAllPressedAdditionalButtons(EventArgs args)
        {
            if (args != null && args is BrailleIO_KeyStateChanged_EventArgs)
            {
                BrailleIO_KeyStateChanged_EventArgs kscea = args as BrailleIO_KeyStateChanged_EventArgs;
                if (kscea.additionalKeyCode != null && kscea.additionalKeyCode.Length > 0)
                {
                    return GetAllPressedAdditionalButtons(kscea.additionalKeyCode);
                }
            }
            return null;
        }

        /// <summary>Gets all released general buttons.</summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>An array of lists of released additional buttons (Flag)</returns>
        public virtual BrailleIO_AdditionalButton[] GetAllReleasedAdditionalButtons(BrailleIO_AdditionalButtonStates[] keys)
        {
            if (keys != null && keys.Length > 0)
            {
                BrailleIO_AdditionalButton[] result = new BrailleIO_AdditionalButton[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    BrailleIO_AdditionalButtonStates item = keys[i];
                    if ((item == BrailleIO_AdditionalButtonStates.None) || item < 0)
                    {
                        result[i] = BrailleIO_AdditionalButton.None;
                        continue;
                    }
                    int released = (int)item & addUpFilter;
                    int binKeys = released;
                    var releasedBinaryKeys = (BrailleIO_AdditionalButton)binKeys;
                    result[i] = releasedBinaryKeys;
                }
                return result;
            }
            return null;
        }

        /// <summary>Gets all released general buttons.</summary>
        /// <param name="keys">All currently released additional keys.</param>
        /// <returns>An array of lists of released additional buttons (Flag)</returns>
        public virtual BrailleIO_AdditionalButton[] GetAllReleasedAdditionalButtons(BrailleIO_AdditionalButton[] keys)
        {
            return keys;
        }

        /// <summary>Gets all released general buttons.</summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>An array of lists of released additional buttons (Flag)</returns>
        public virtual BrailleIO_AdditionalButton[] GetAllReleasedAdditionalButtons(EventArgs args)
        {
            if (args != null && args is BrailleIO_KeyStateChanged_EventArgs)
            {
                BrailleIO_KeyStateChanged_EventArgs kscea = args as BrailleIO_KeyStateChanged_EventArgs;
                if (kscea.additionalKeyCode != null && kscea.additionalKeyCode.Length > 0)
                {
                    return GetAllReleasedAdditionalButtons(kscea.additionalKeyCode);
                }
            }
            return null;
        }

        #endregion

        #region Generic (proprietary)

        /// <summary>Gets all pressed generic buttons.</summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of pressed and interpreted generic buttons</returns>
        public virtual List<string> GetAllPressedGenericButtons(EventArgs args)
        {
            return new List<string>();
        }

        /// <summary>Gets all pressed generic buttons.</summary>
        /// <param name="raw">The raw event data.</param>
        /// <returns>a list of pressed and interpreted generic buttons</returns>
        public virtual List<string> GetAllPressedGenericButtons(OrderedDictionary raw)
        {
            return new List<string>();
        }

        /// <summary>Gets all released generic buttons.</summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of released and interpreted generic buttons</returns>
        public virtual List<string> GetAllReleasedGenericButtons(EventArgs args)
        {
            return new List<string>();
        }

        /// <summary>Gets all released generic buttons.</summary>
        /// <param name="raw">The raw event data.</param>
        /// <returns>a list of released and interpreted generic buttons</returns>
        public virtual List<string> GetAllReleasedGenericButtons(OrderedDictionary raw)
        {
            return new List<string>();
        }

        #endregion

        /// <summary>Gets all adapter types this mediator is related to.</summary>
        /// <returns>a list of adapter class types this mediator is related to</returns>
        public abstract List<Type> GetRelatedAdapterTypes();


    }
}