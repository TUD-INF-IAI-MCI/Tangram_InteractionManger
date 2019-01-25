using System;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Class that interprets input commands from the interaction manager 
    /// and forward them to listeners who handle those interactions.
    /// </summary>
    partial class ScriptFunctionProxy
    {
        #region Member

        private static readonly ScriptFunctionProxy _instance = new ScriptFunctionProxy();
        private InteractionManager interactionManager;

        /// <summary>
        /// The global settings storage for sharing settings over multiple accessors.
        /// </summary>
        public readonly System.Collections.Concurrent.ConcurrentDictionary<String, Object> GlobalSettings = new System.Collections.Concurrent.ConcurrentDictionary<string, object>();

        #endregion

        #region Constructor / Destructor / Singleton
        
        ScriptFunctionProxy() { }
        ~ScriptFunctionProxy() { }

        /// <summary>
        /// Gets the singleton instance of the ScriptFunctionProxy Object.
        /// </summary>
        /// <value>The instance.</value>
        public static ScriptFunctionProxy Instance { get { return _instance; } }
       
        #endregion

        /// <summary>
        /// Sets the interaction manager.
        /// </summary>
        /// <param name="interactionManager">The interaction manager.</param>
        public void SetInteractionManager(InteractionManager interactionManager)
        {
            if (interactionManager != null)
            {
                if (this.interactionManager != null)
                {
                    try
                    {
                        this.interactionManager.ButtonPressed -= new EventHandler<ButtonPressedEventArgs>(interactionManager_ButtonPressed);
                        this.interactionManager.ButtonReleased -= new EventHandler<ButtonReleasedEventArgs>(interactionManager_ButtonReleased);
                        this.interactionManager.GesturePerformed -= new EventHandler<GestureEventArgs>(interactionManager_GesturePerformed);
                        this.interactionManager.ButtonCombinationReleased -= new EventHandler<ButtonReleasedEventArgs>(interactionManager_ButtonCombinationReleased);
                        this.interactionManager.FunctionCall -= new EventHandler<FunctionCallInteractionEventArgs>(interactionManager_FunctionCall);
                    }
                    catch { }
                }
                this.interactionManager = interactionManager;
                this.interactionManager.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(interactionManager_ButtonPressed);
                this.interactionManager.ButtonReleased += new EventHandler<ButtonReleasedEventArgs>(interactionManager_ButtonReleased);
                this.interactionManager.GesturePerformed += new EventHandler<GestureEventArgs>(interactionManager_GesturePerformed);
                this.interactionManager.ButtonCombinationReleased += new EventHandler<ButtonReleasedEventArgs>(interactionManager_ButtonCombinationReleased);
                this.interactionManager.FunctionCall += new EventHandler<FunctionCallInteractionEventArgs>(interactionManager_FunctionCall);
            }
        }

        /// <summary>
        /// Initializes the instance with the important global objects.
        /// </summary>
        /// <param name="interactionManager">The interaction manager.</param>
        /// <param name="windowManager">The window manager.</param>
        public void Initialize(InteractionManager interactionManager)
        {
            SetInteractionManager(interactionManager);
        }

        #region InteractionManager events
        void interactionManager_GesturePerformed(object sender, GestureEventArgs e)
        {
            if (e != null)
            {
                sentGesturePerformedToRegisteredSpecifiedFunctionProxies(sender, e);
            }
        }

        void interactionManager_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e != null)
            {
                sentButtonReleasedToRegisteredSpecifiedFunctionProxies(sender, e);
            }
        }

        void interactionManager_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e != null)
            {
                sentButtonPressedToRegisteredSpecifiedFunctionProxies(sender, e);
            }
        }

        void interactionManager_ButtonCombinationReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e != null && e.ReleasedGenericKeys != null && e.ReleasedGenericKeys.Count > 0 && (e.PressedGenericKeys == null || e.PressedGenericKeys.Count < 1))
            {
                if (interactionManager.Mode == InteractionMode.Braille)
                {
                    interpretBrailleKeyboardCommand(e.ReleasedGenericKeys);
                }

                sentButtonCombinationReleasedToRegisteredSpecifiedFunctionProxies(sender, ref e);
            }
        }


        private void interactionManager_FunctionCall(object sender, FunctionCallInteractionEventArgs e)
        {
            if (e != null && !String.IsNullOrEmpty(e.Function))
            {
                bool canceled;
                sentFunctionCallToRegisteredSpecifiedFunctionProxies(sender, ref e, out canceled);
            }
        }


        #endregion
    }
}