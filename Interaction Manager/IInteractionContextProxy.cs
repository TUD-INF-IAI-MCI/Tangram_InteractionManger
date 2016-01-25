
namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Interface for a sortable and activatable interaction event interpreter
    /// </summary>
    public interface IInteractionContextProxy : IInteractionEventProxy
    {
        /// <summary>
        /// registers this instance to events
        /// </summary>
        /// <param name="iaEventSource">a source for interaction events.</param>
        void RegisterToEvents(IInteractionEventProxy iaEventSource);
        /// <summary>
        /// Unregisters this instance from events.
        /// </summary>
        /// <param name="iaEventSource">a source for interaction events.</param>
        void UnregisterFromEvents(IInteractionEventProxy iaEventSource);
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IInteractionContextProxy"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        bool Active { get; set; }
        /// <summary>
        /// Gets or sets the z-index of this instance.As higher the index as earlier it is called in the event queue.
        /// </summary>
        /// <value>The z-index.</value>
        int ZIndex { get; set; }
    }
}
