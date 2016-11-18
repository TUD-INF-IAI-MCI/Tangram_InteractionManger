using System;
using tud.mci.tangram.TangramLector.Classes;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Class that interprets input commands from the interaction manager 
    /// and forward them to listeners who handle those interactions.
    /// </summary>
    partial class ScriptFunctionProxy
    {
        #region Members

        private readonly OrderedConcurrentDictionary<int, IInteractionContextProxy> proxies = new OrderedConcurrentDictionary<int, IInteractionContextProxy>(new InteractionContextProxyComparer());
        private readonly BasicInteractionEventForwarder eventForwarder = new BasicInteractionEventForwarder();

        #endregion

        /// <summary>
        /// Adds a new proxy for receiving interaction events.
        /// </summary>
        /// <param name="proxy">The proxy.</param>
        /// <returns><c>true</c> if the proxy could been added.</returns>
        public bool AddProxy(IInteractionContextProxy proxy)
        {
            if (proxy != null && proxies != null)
            {
                if (proxies.Contains(proxy.GetHashCode())) return false;
                try
                {
                    proxies.Add(proxy.GetHashCode(), proxy);
                    reregisterEventHandlers();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(LogPriority.ALWAYS, this, "Can not add specialized function proxy", ex);
                } 
            }
            return false;
        }

        /// <summary>
        /// Removes the function proxy.
        /// </summary>
        /// <param name="proxy">The proxy.</param>
        /// <returns></returns>
        public bool RemoveFunctionProxy(IInteractionContextProxy proxy)
        {
            if (proxy != null && proxies != null && proxies.Contains(proxy.GetHashCode()))
            {
                try
                {
                    proxy.Active = false;
                    proxy.UnregisterFromEvents(eventForwarder);
                    proxies.Remove(proxy.GetHashCode());
                }
                catch (System.Exception ex)
                {
                    Logger.Instance.Log(LogPriority.ALWAYS, this, "Can not remove specialized function proxy", ex);
                }
            }
            return true;
        }

        /// <summary>
        /// important function for ordered event queue handling.
        /// unregisters all listeners and reregister them in zIndex order
        /// </summary>
        void reregisterEventHandlers()
        {
            foreach (var item in proxies)
            {
                if (item.Value != null)
                {
                    item.Value.UnregisterFromEvents(eventForwarder);
                }
            }

            foreach (var item in proxies.GetSortedValues())
            {
                if (item.Value != null)
                {
                    item.Value.RegisterToEvents(eventForwarder);
                }
            }

        }

        private readonly object _listLock = new Object();

        /// <summary>
        /// Gets the list of all registered interaction context proxies.
        /// </summary>
        /// <returns>list of registered proxies</returns>
        public OrderedConcurrentDictionary<int,IInteractionContextProxy> GetInteractionContextProxies()
        {
            lock (_listLock)
            {
                return proxies; 
            }
        }

        #region Forward Events to Specialized Function Proxies

        private void sentButtonCombinationReleasedToRegisteredSpecifiedFunctionProxies(object sender, ref ButtonReleasedEventArgs e)
        {
            if (eventForwarder != null) { eventForwarder.fireButtonCombinationReleasedEvent(sender, e); }
        }
        private void sentButtonReleasedToRegisteredSpecifiedFunctionProxies(object sender, ButtonReleasedEventArgs e)
        {
            if (eventForwarder != null) { eventForwarder.fireButtonReleasedEvent(sender, e); }
        }
        private void sentButtonPressedToRegisteredSpecifiedFunctionProxies(object sender, ButtonPressedEventArgs e)
        {
            if (eventForwarder != null) { eventForwarder.fireButtonPressedEvent(sender, e); }
        }
        private void sentGesturePerformedToRegisteredSpecifiedFunctionProxies(object sender, GestureEventArgs e)
        {
            if (eventForwarder != null) { eventForwarder.fireGestureEvent(sender, e); }
        }

        #endregion

    }

    /// <summary>
    /// Class to forward interactionManager events to e.g. specialized function proxies.
    /// This forwarder controls if an event queue should be canceled.
    /// </summary>
    class BasicInteractionEventForwarder : IInteractionEventProxy
    {
        public event EventHandler<ButtonReleasedEventArgs> ButtonReleased;
        public event EventHandler<ButtonReleasedEventArgs> ButtonCombinationReleased;
        public event EventHandler<ButtonPressedEventArgs> ButtonPressed;
        public event EventHandler<GestureEventArgs> GesturePerformed;
        
        internal bool fireButtonReleasedEvent(Object sender, ButtonReleasedEventArgs args)
        {
            bool cancel = false;
            if (ButtonReleased != null && args != null)
            {
                foreach (EventHandler<ButtonReleasedEventArgs> hndl in ButtonReleased.GetInvocationList())
                {
                    if (cancel) return cancel;

                    if (hndl != null && hndl.Target is IInteractionContextProxy)
                    {
                        if (!((IInteractionContextProxy)hndl.Target).Active)
                        {
                            continue;
                        }
                    }

                    try
                    {
                        if (hndl != null) { hndl.Invoke(sender, args); }
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

        internal bool fireButtonCombinationReleasedEvent(Object sender, ButtonReleasedEventArgs args)
        {
            bool cancel = false;
            if (ButtonCombinationReleased != null && args != null)
            {
                foreach (EventHandler<ButtonReleasedEventArgs> hndl in ButtonCombinationReleased.GetInvocationList())
                {
                    if (cancel) return cancel;

                    if (hndl != null && hndl.Target is IInteractionContextProxy)
                    {
                        if (!((IInteractionContextProxy)hndl.Target).Active) {
                            continue;
                        }
                    }

                    try
                    {
                        if (hndl != null) { hndl.Invoke(sender, args); }
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

        internal bool fireButtonPressedEvent(Object sender, ButtonPressedEventArgs args)
        {
            bool cancel = false;
            if (ButtonPressed != null && args != null)
            {
                foreach (EventHandler<ButtonPressedEventArgs> hndl in ButtonPressed.GetInvocationList())
                {
                    if (cancel) return cancel;

                    if (hndl != null && hndl.Target is IInteractionContextProxy)
                    {
                        if (!((IInteractionContextProxy)hndl.Target).Active)
                        {
                           continue;
                        }
                    }

                    try
                    {
                        if (hndl != null) { hndl.Invoke(sender, args); }
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

        internal bool fireGestureEvent(Object sender, GestureEventArgs args)
        {
            bool cancel = false;
            if (GesturePerformed != null && args != null)
            {
                foreach (EventHandler<GestureEventArgs> hndl in GesturePerformed.GetInvocationList())
                {
                    if (cancel) return cancel;

                    if (hndl != null && hndl.Target is IInteractionContextProxy)
                    {
                        if (!((IInteractionContextProxy)hndl.Target).Active)
                        {
                            continue;
                        }
                    }

                    try
                    {
                        if (hndl != null) { hndl.Invoke(sender, args); }
                        if (args.Cancel == true)
                        {
                            cancel = args.Cancel;
                            break;
                        }
                    }
                    catch (Exception ex) { }
                }
            }
            return cancel;
        }
    }
}