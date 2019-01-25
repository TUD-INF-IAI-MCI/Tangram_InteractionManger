using System;
using System.Collections.Generic;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Abstract implementation for an <see cref="IInteractionEventProxy"/>
    /// </summary>
    public abstract class AbstractInteractionEventProxy : IInteractionEventProxy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractInteractionEventProxy"/> class.
        /// </summary>
        public AbstractInteractionEventProxy(){}

        /// <summary>
        /// Occurs when at leased one button was released.
        /// </summary>
        public event EventHandler<ButtonReleasedEventArgs> ButtonReleased;
        /// <summary>
        /// Occurs when a button combination released (contains of at least one button).
        /// </summary>
        public event EventHandler<ButtonReleasedEventArgs> ButtonCombinationReleased;
        /// <summary>
        /// Occurs when at leased one button was pressed.
        /// </summary>
        public event EventHandler<ButtonPressedEventArgs> ButtonPressed;
        /// <summary>
        /// Occurs when a gesture was performed.
        /// </summary>
        public event EventHandler<GestureEventArgs> GesturePerformed;

        /// <summary>Occurs when a specific function should be called after a user interaction.</summary>
        public event EventHandler<FunctionCallInteractionEventArgs> FunctionCall;


        /// <summary>
        /// Fires the button released event.
        /// </summary>
        /// <param name="args">The <see cref="ButtonReleasedEventArgs"/> instance containing the event data.</param>
        /// <returns>cancel event throwing or not</returns>
        protected virtual bool fireButtonReleasedEvent(ButtonReleasedEventArgs args)
        {
            bool cancel = false;
            if (ButtonReleased != null && args != null)
            {
                foreach (EventHandler<ButtonReleasedEventArgs> hndl in ButtonReleased.GetInvocationList())
                {
                    try
                    {
                        if (hndl != null) { hndl.DynamicInvoke(this, args); }
                        if (args.Cancel == true)
                        {
                            cancel = args.Cancel;
                            break;
                        }
                    }
                    catch (Exception) { }
                }
            }
            return cancel;

        }
        /// <summary>
        /// Fires the button combination released event.
        /// </summary>
        /// <param name="args">The <see cref="ButtonReleasedEventArgs"/> instance containing the event data.</param>
        /// <returns>cancel event throwing or not</returns>
        protected virtual bool fireButtonCombinationReleasedEvent(ButtonReleasedEventArgs args)
        {
            bool cancel = false;
            if (ButtonCombinationReleased != null && args != null)
            {
                foreach (EventHandler<ButtonReleasedEventArgs> hndl in ButtonCombinationReleased.GetInvocationList())
                {
                    try
                    {
                        if (hndl != null) { hndl.DynamicInvoke(this, args); }
                        if (args.Cancel == true)
                        {
                            cancel = args.Cancel;
                            break;
                        }
                    }
                    catch (Exception) { }
                }
            }
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
        protected virtual bool fireFunctionCalledEvent(
            string functionName,
            BrailleIO.BrailleIODevice device,
            BrailleIO.Structs.KeyCombinationItem keyCombination,
            List<String> pressedGenericKeys,
            List<String> releasedGenericKeys,
            out bool canceled)
        {
            canceled = false;
            bool handled = false;

            if (FunctionCall != null)
            {
                FunctionCallInteractionEventArgs args = new FunctionCallInteractionEventArgs(
                    functionName, device, keyCombination, pressedGenericKeys, releasedGenericKeys);

                foreach (EventHandler<FunctionCallInteractionEventArgs> hndl in FunctionCall.GetInvocationList())
                {
                    try
                    {
                        if (hndl != null) { hndl.DynamicInvoke(this, args); }
                        if (args.Handled == true)
                        {
                            handled = true;
                        }
                        if (args.Cancel == true)
                        {
                            canceled = true;
                            break;
                        }
                    }
                    catch (Exception) { }
                }

            }
            return handled;
        }
        /// <summary>
        /// Fires the button pressed event.
        /// </summary>
        /// <param name="args">The <see cref="ButtonPressedEventArgs"/> instance containing the event data.</param>
        /// <returns>cancel event throwing or not</returns>
        protected virtual bool fireButtonPressedEvent(ButtonPressedEventArgs args)
        {
            bool cancel = false;
            if (ButtonPressed != null && args != null)
            {
                foreach (EventHandler<ButtonPressedEventArgs> hndl in ButtonPressed.GetInvocationList())
                {
                    try
                    {
                        if (hndl != null) { hndl.DynamicInvoke(this, args); }
                        if (args.Cancel == true)
                        {
                            cancel = args.Cancel;
                            break;
                        }
                    }
                    catch (Exception) { }
                }
            }
            return cancel;
        }
        /// <summary>
        /// Fires the gesture event.
        /// </summary>
        /// <param name="args">The <see cref="GestureEventArgs"/> instance containing the event data.</param>
        /// <returns>cancel event throwing or not</returns>
        protected virtual bool fireGestureEvent(GestureEventArgs args)
        {
            bool cancel = false;
            if (GesturePerformed != null && args != null)
            {
                foreach (EventHandler<GestureEventArgs> hndl in GesturePerformed.GetInvocationList())
                {
                    try
                    {
                        if (hndl != null) { hndl.DynamicInvoke(this, args); }
                        if (args.Cancel == true)
                        {
                            cancel = args.Cancel;
                            break;
                        }
                    }
                    catch (Exception) { }
                }
            }
            return cancel;
        }
    }
}
