using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrailleIO.Interface;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Interface for generalizing the generic raw data from <see cref="BrailleIO.Interface.IBrailleIOAdapter"/>
    /// </summary>
    public interface IBrailleIOButtonMediator
    {
        /// <summary>
        /// Gets all adapter types this mediator is related to.
        /// </summary>
        /// <returns>a list of adapter class types this mediator is related to</returns>
        List<Type> GetRelatedAdapterTypes();

        /// <summary>
        /// Gets all pressed general buttons.
        /// </summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>a list of pressed general buttons</returns>
        List<BrailleIO_DeviceButton> GetAllPressedGeneralButtons(BrailleIO_DeviceButtonStates keys);
        
        /// <summary>
        /// Gets all released general buttons.
        /// </summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>a list of released general buttons</returns>
        List<BrailleIO_DeviceButton> GetAllReleasedGeneralButtons(BrailleIO_DeviceButtonStates keys);
        
        /// <summary>
        /// Gets all pressed general buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of pressed general buttons</returns>
        List<BrailleIO_DeviceButton> GetAllPressedGeneralButtons(System.EventArgs args);
        
        /// <summary>
        /// Gets all released general buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of released general buttons</returns>
        List<BrailleIO_DeviceButton> GetAllReleasedGeneralButtons(System.EventArgs args);
        
        /// <summary>
        /// Gets all pressed generic buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of pressed and interpreted generic buttons</returns>
        List<String> GetAllPressedGenericButtons(System.EventArgs args);
        
        /// <summary>
        /// Gets all released generic buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of released and interpreted generic buttons</returns>
        List<String> GetAllReleasedGenericButtons(System.EventArgs args);

        /// <summary>
        /// Gets the gesture.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        object GetGesture(System.EventArgs args);
    }
}
