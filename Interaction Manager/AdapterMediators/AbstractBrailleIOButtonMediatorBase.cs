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
        protected BrailleIODevice device;
        protected List<String> lastGenericPressedkeys = new List<String>();
        protected List<String> releasedGenericPressedkeys = new List<String>();
        protected List<BrailleIO_DeviceButton> lastGeneralPressedkeys = new List<BrailleIO_DeviceButton>();
        protected List<BrailleIO_DeviceButton> releasedGeneralPressedkeys = new List<BrailleIO_DeviceButton>();

        #endregion

        public AbstractBrailleIOButtonMediatorBase() { }
        public AbstractBrailleIOButtonMediatorBase(BrailleIODevice device) { this.device = device; }
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

        //virtual public List<BrailleIO_DeviceButton> GetAllPressedGeneralButtons(EventArgs args)
        //{
        //    return new List<BrailleIO_DeviceButton>();
        //}

        //virtual public List<BrailleIO_DeviceButton> GetAllReleasedGeneralButtons(EventArgs args)
        //{
        //    return new List<BrailleIO_DeviceButton>();
        //}

        //virtual public List<string> GetAllPressedGenericButtons(EventArgs args)
        //{
        //    return new List<string>();
        //}

        //virtual public List<string> GetAllReleasedGenericButtons(EventArgs args)
        //{
        //    return new List<string>();
        //}

        //virtual public object GetGesture(EventArgs args)
        //{
        //    return null;
        //}
    }
}