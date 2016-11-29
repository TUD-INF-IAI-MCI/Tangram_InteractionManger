using System;
using System.Collections.Generic;
using BrailleIO;
using BrailleIO.Interface;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Abstract basic implementation for an 
    /// </summary>
    public abstract class AbstractBrailleIOButtonMediatorBase 
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractBrailleIOButtonMediatorBase"/> class.
        /// </summary>
        public AbstractBrailleIOButtonMediatorBase() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractBrailleIOButtonMediatorBase"/> class.
        /// </summary>
        /// <param name="device">The related device to this mediator.</param>
        public AbstractBrailleIOButtonMediatorBase(BrailleIODevice device) { this.device = device; }
        /// <summary>
        /// Sets the related device to this mediator.
        /// </summary>
        /// <param name="device">The device.</param>
        public void setDevice(BrailleIODevice device) { this.device = device; }

        /// <summary>
        /// Gets all pressed general buttons.
        /// </summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>a list of pressed general buttons</returns>
        virtual public List<BrailleIO_DeviceButton> GetAllPressedGeneralButtons(BrailleIO_DeviceButtonStates keys)
        {
            List<BrailleIO_DeviceButton> k = new List<BrailleIO_DeviceButton>();
            if ((keys == BrailleIO_DeviceButtonStates.None) || keys < 0) return k;

            if ((keys & BrailleIO_DeviceButtonStates.EnterDown) == BrailleIO_DeviceButtonStates.EnterDown) k.Add(BrailleIO_DeviceButton.Enter);
            if ((keys & BrailleIO_DeviceButtonStates.AbortDown) == BrailleIO_DeviceButtonStates.AbortDown) k.Add(BrailleIO_DeviceButton.Abort);
            if ((keys & BrailleIO_DeviceButtonStates.GestureDown) == BrailleIO_DeviceButtonStates.GestureDown) k.Add(BrailleIO_DeviceButton.Gesture);
            if ((keys & BrailleIO_DeviceButtonStates.LeftDown) == BrailleIO_DeviceButtonStates.LeftDown) k.Add(BrailleIO_DeviceButton.Left);
            if ((keys & BrailleIO_DeviceButtonStates.RightDown) == BrailleIO_DeviceButtonStates.RightDown) k.Add(BrailleIO_DeviceButton.Right);
            if ((keys & BrailleIO_DeviceButtonStates.UpDown) == BrailleIO_DeviceButtonStates.UpDown) k.Add(BrailleIO_DeviceButton.Up);
            if ((keys & BrailleIO_DeviceButtonStates.DownDown) == BrailleIO_DeviceButtonStates.DownDown) k.Add(BrailleIO_DeviceButton.Down);
            if ((keys & BrailleIO_DeviceButtonStates.ZoomInDown) == BrailleIO_DeviceButtonStates.ZoomInDown) k.Add(BrailleIO_DeviceButton.ZoomIn);
            if ((keys & BrailleIO_DeviceButtonStates.ZoomOutDown) == BrailleIO_DeviceButtonStates.ZoomOutDown) k.Add(BrailleIO_DeviceButton.ZoomOut);
            if ((keys & BrailleIO_DeviceButtonStates.Unknown) == BrailleIO_DeviceButtonStates.Unknown) k.Add(BrailleIO_DeviceButton.Unknown);
            return k;
        }

        /// <summary>
        /// Gets all released general buttons.
        /// </summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>a list of released general buttons</returns>
        virtual public List<BrailleIO_DeviceButton> GetAllReleasedGeneralButtons(BrailleIO_DeviceButtonStates keys)
        {
            List<BrailleIO_DeviceButton> k = new List<BrailleIO_DeviceButton>();
            if ((keys == BrailleIO_DeviceButtonStates.None) || keys < 0) return k;

            if ((keys & BrailleIO_DeviceButtonStates.EnterUp) == BrailleIO_DeviceButtonStates.EnterUp) k.Add(BrailleIO_DeviceButton.Enter);
            if ((keys & BrailleIO_DeviceButtonStates.AbortUp) == BrailleIO_DeviceButtonStates.AbortUp) k.Add(BrailleIO_DeviceButton.Abort);
            if ((keys & BrailleIO_DeviceButtonStates.GestureUp) == BrailleIO_DeviceButtonStates.GestureUp) k.Add(BrailleIO_DeviceButton.Gesture);
            if ((keys & BrailleIO_DeviceButtonStates.LeftUp) == BrailleIO_DeviceButtonStates.LeftUp) k.Add(BrailleIO_DeviceButton.Left);
            if ((keys & BrailleIO_DeviceButtonStates.RightUp) == BrailleIO_DeviceButtonStates.RightUp) k.Add(BrailleIO_DeviceButton.Right);
            if ((keys & BrailleIO_DeviceButtonStates.UpUp) == BrailleIO_DeviceButtonStates.UpUp) k.Add(BrailleIO_DeviceButton.Up);
            if ((keys & BrailleIO_DeviceButtonStates.DownUp) == BrailleIO_DeviceButtonStates.DownUp) k.Add(BrailleIO_DeviceButton.Down);
            if ((keys & BrailleIO_DeviceButtonStates.ZoomInUp) == BrailleIO_DeviceButtonStates.ZoomInUp) k.Add(BrailleIO_DeviceButton.ZoomIn);
            if ((keys & BrailleIO_DeviceButtonStates.ZoomOutUp) == BrailleIO_DeviceButtonStates.ZoomOutUp) k.Add(BrailleIO_DeviceButton.ZoomOut);
            if ((keys & BrailleIO_DeviceButtonStates.Unknown) == BrailleIO_DeviceButtonStates.Unknown) k.Add(BrailleIO_DeviceButton.Unknown);
            return k;
        }
    }
}