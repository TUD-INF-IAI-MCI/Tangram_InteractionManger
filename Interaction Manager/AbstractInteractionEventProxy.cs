using System;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Abstract implementation for an <see cref="IInteractionEventProxy"/>
    /// </summary>
    public abstract class AbstractInteractionEventProxy : IInteractionEventProxy
    {
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
