using System;

namespace tud.mci.tangram.TangramLector.SpecializedFunctionProxies
{
    /// <summary>
    /// A special abstract implementation of the <see cref="AbstractInteractionEventProxy"/> extended 
    /// with an abstract implementation of <see cref="IInteractionContextProxy"/> including a zIndex 
    /// and an activation flag.
    /// </summary>
    public class AbstractSpecializedFunctionProxyBase : AbstractInteractionEventProxy, IInteractionContextProxy
    {
        #region Members

        IInteractionEventProxy interactionEventSource;

        private volatile bool _active = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IInteractionContextProxy" /> is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public virtual bool Active
        {
            get { return _active; }
            set
            {
                if (value != _active)
                {
                    _active = value;
                    if (_active) { fireActivatedEvent(); }
                    else { fireDeactivatedEvent(); }
                }
            }
        }

        private int _zindex = 1;
        /// <summary>
        /// sorting index for calling. 
        /// The higher the value the earlier it is called 
        /// in the function proxy chain.
        /// </summary>
        public virtual int ZIndex
        {
            get { return _zindex; }
            set { _zindex = value; }
        }

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractSpecializedFunctionProxyBase"/> class.
        /// </summary>
        public AbstractSpecializedFunctionProxyBase() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractSpecializedFunctionProxyBase"/> class.
        /// </summary>
        /// <param name="zIndex">Index position in the list of loaded and requested proxies. 
        /// As higher the index as earlier it will be requested. Standard is 0.</param>
        public AbstractSpecializedFunctionProxyBase(int zIndex) { ZIndex = zIndex; }

        ~AbstractSpecializedFunctionProxyBase() { UnregisterFromEvents(interactionEventSource); }

        #endregion

        #region IInteractionContextProxy

        /// <summary>
        /// Registers this instance to all events of the given <see cref="IInteractionEventProxy"/>.
        /// </summary>
        /// <param name="iep">The event proxy that forwards specific interaction events.</param>
        public virtual void RegisterToEvents(IInteractionEventProxy iep)
        {
            interactionEventSource = iep;
            if (iep != null)
            {
                UnregisterFromEvents(iep);
                iep.ButtonCombinationReleased += new EventHandler<ButtonReleasedEventArgs>(im_ButtonCombinationReleased);
                iep.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(im_ButtonPressed);
                iep.ButtonReleased += new EventHandler<ButtonReleasedEventArgs>(im_ButtonReleased);
                iep.GesturePerformed += new EventHandler<GestureEventArgs>(im_GesturePerformed);
            }
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Unregisters this instance from all events of the given <see cref="IInteractionEventProxy"/>.
        /// </summary>
        /// <param name="iep">The event proxy that forwards specific interaction events.</param>
        public virtual void UnregisterFromEvents(IInteractionEventProxy iep)
        {
            if (iep != null)
            {
                try { iep.ButtonCombinationReleased -= new EventHandler<ButtonReleasedEventArgs>(im_ButtonCombinationReleased); }
                catch { }
                try { iep.ButtonPressed -= new EventHandler<ButtonPressedEventArgs>(im_ButtonPressed); }
                catch { }
                try { iep.ButtonReleased -= new EventHandler<ButtonReleasedEventArgs>(im_ButtonReleased); }
                catch { }
                try { iep.GesturePerformed -= new EventHandler<GestureEventArgs>(im_GesturePerformed); }
                catch { }
            }
        }

        /// <summary>
        /// Occurs when this instance is [activated].
        /// </summary>
        public event EventHandler Activated;
        /// <summary>
        /// Occurs when this instance is [deactivated].
        /// </summary>
        public event EventHandler Deactivated;

        /// <summary>
        /// Handles the GesturePerformed event of the <see cref="IInteractionEventProxy"/> control.
        /// Base implementation forwards this event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GestureEventArgs"/> instance containing the event data.</param>
        protected virtual void im_GesturePerformed(object sender, GestureEventArgs e)
        {
            if (Active && e.Gesture != null) { base.fireGestureEvent(e); }
        }

        /// <summary>
        /// Handles the ButtonReleased event of the <see cref="IInteractionEventProxy"/> control.
        /// Base implementation forwards this event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ButtonReleasedEventArgs"/> instance containing the event data.</param>
        protected virtual void im_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (Active) { base.fireButtonReleasedEvent(e); }
        }

        /// <summary>
        /// Handles the ButtonPressed event of the <see cref="IInteractionEventProxy"/> control.
        /// Base implementation forwards this event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ButtonPressedEventArgs"/> instance containing the event data.</param>
        protected virtual void im_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Active) { base.fireButtonPressedEvent(e); }
        }

        /// <summary>
        /// Handles the ButtonCombinationReleased event of the <see cref="IInteractionEventProxy"/> control.
        /// Base implementation forwards this event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ButtonReleasedEventArgs"/> instance containing the event data.</param>
        protected virtual void im_ButtonCombinationReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (Active) { base.fireButtonCombinationReleasedEvent(e); }
        }

        /// <summary>
        /// Fires the activated event.
        /// </summary>
        protected virtual void fireActivatedEvent()
        {
            if (Activated != null)
            {
                try { Activated.DynamicInvoke(this, new EventArgs()); }
                catch { }
            }
        }

        /// <summary>
        /// Fires the deactivated event.
        /// </summary>
        protected virtual void fireDeactivatedEvent()
        {
            if (Deactivated != null)
            {
                try { Deactivated.DynamicInvoke(this, new EventArgs()); }
                catch { }
            }
        }
        
        #endregion

    }
}
