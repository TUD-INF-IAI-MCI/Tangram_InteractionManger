using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

        #region Buttons

        #region general

        /// <summary>
        /// Gets all pressed general buttons.
        /// </summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>a list of pressed general buttons (Flag)</returns>
        BrailleIO_DeviceButton GetAllPressedGeneralButtons(BrailleIO_DeviceButtonStates keys);

        /// <summary>
        /// Gets all pressed general buttons.
        /// </summary>
        /// <param name="keys">All currently pressed general keys.</param>
        /// <returns>a list of pressed general buttons (Flag)</returns>
        BrailleIO_DeviceButton GetAllPressedGeneralButtons(BrailleIO_DeviceButton keys);

        /// <summary>
        /// Gets all pressed general buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of pressed general buttons (Flag)</returns>
        BrailleIO_DeviceButton GetAllPressedGeneralButtons(System.EventArgs args);

        /// <summary>
        /// Gets all released general buttons.
        /// </summary>
        /// <param name="keys">All current keys states.</param>
        /// <returns>a list of released general buttons (Flag)</returns>
        BrailleIO_DeviceButton GetAllReleasedGeneralButtons(BrailleIO_DeviceButtonStates keys);

        /// <summary>
        /// Gets all released general buttons.
        /// </summary>
        /// <param name="keys">All currently released general keys.</param>
        /// <returns>a list of released general buttons (Flag)</returns>
        BrailleIO_DeviceButton GetAllReleasedGeneralButtons(BrailleIO_DeviceButton keys);

        /// <summary>
        /// Gets all released general buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of released general buttons (Flag)</returns>
        BrailleIO_DeviceButton GetAllReleasedGeneralButtons(System.EventArgs args);

        #endregion

        #region Braille keyboard

        /// <summary>
        /// Gets all pressed Braille keyboard buttons.
        /// </summary>
        /// <param name="keys">All current Braille keyboard keys states.</param>
        /// <returns>a list of pressed Braille keyboard buttons (Flag)</returns>
        BrailleIO_BrailleKeyboardButton GetAllPressedBrailleKeyboardButtons(BrailleIO_BrailleKeyboardButtonStates keys);

        /// <summary>
        /// Gets all pressed Braille keyboard buttons.
        /// </summary>
        /// <param name="keys">All currently pressed Braille keyboard keys.</param>
        /// <returns>a list of pressed Braille keyboard buttons (Flag)</returns>
        BrailleIO_BrailleKeyboardButton GetAllPressedBrailleKeyboardButtons(BrailleIO_BrailleKeyboardButton keys);

        /// <summary>
        /// Gets all pressed Braille keyboard buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of pressed Braille keyboard buttons (Flag)</returns>
        BrailleIO_BrailleKeyboardButton GetAllPressedBrailleKeyboardButtons(System.EventArgs args);

        /// <summary>
        /// Gets all released Braille keyboard buttons.
        /// </summary>
        /// <param name="keys">All current Braille keyboard keys states.</param>
        /// <returns>a list of released Braille keyboard buttons (Flag)</returns>
        BrailleIO_BrailleKeyboardButton GetAllReleasedBrailleKeyboardButtons(BrailleIO_BrailleKeyboardButtonStates keys);

        /// <summary>
        /// Gets all released Braille keyboard buttons.
        /// </summary>
        /// <param name="keys">All currently released Braille keyboard keys states.</param>
        /// <returns>a list of released Braille keyboard buttons (Flag)</returns>
        BrailleIO_BrailleKeyboardButton GetAllReleasedBrailleKeyboardButtons(BrailleIO_BrailleKeyboardButton keys);

        /// <summary>
        /// Gets all released Braille keyboard buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of released Braille keyboard buttons (Flag)</returns>
        BrailleIO_BrailleKeyboardButton GetAllReleasedBrailleKeyboardButtons(System.EventArgs args);

        #endregion
        
        #region additional buttons

        /// <summary>
        /// Gets all pressed additional buttons.
        /// </summary>
        /// <param name="keys">All current additional keys states.</param>
        /// <returns>An array of lists of pressed additional buttons (Flag)</returns>
        BrailleIO_AdditionalButton[] GetAllPressedAdditionalButtons(BrailleIO_AdditionalButtonStates[] keys);

        /// <summary>
        /// Gets all pressed additional buttons.
        /// </summary>
        /// <param name="keys">All currently pressed additional keys states.</param>
        /// <returns>An array of lists of pressed additional buttons (Flag)</returns>
        BrailleIO_AdditionalButton[] GetAllPressedAdditionalButtons(BrailleIO_AdditionalButton[] keys);

        /// <summary>
        /// Gets all pressed general buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>An array of lists of pressed additional buttons (Flag)</returns>
        BrailleIO_AdditionalButton[] GetAllPressedAdditionalButtons(System.EventArgs args);

        /// <summary>
        /// Gets all released general buttons.
        /// </summary>
        /// <param name="keys">All current additional keys states.</param>
        /// <returns>An array of lists of released additional buttons (Flag)</returns>
        BrailleIO_AdditionalButton[] GetAllReleasedAdditionalButtons(BrailleIO_AdditionalButtonStates[] keys);

        /// <summary>
        /// Gets all released general buttons.
        /// </summary>
        /// <param name="keys">All currently released additional keys.</param>
        /// <returns>An array of lists of released additional buttons (Flag)</returns>
        BrailleIO_AdditionalButton[] GetAllReleasedAdditionalButtons(BrailleIO_AdditionalButton[] keys);

        /// <summary>
        /// Gets all released general buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>An array of lists of released additional buttons (Flag)</returns>
        BrailleIO_AdditionalButton[] GetAllReleasedAdditionalButtons(System.EventArgs args);

        #endregion

        #region generic (proprietary)

        /// <summary>
        /// Gets all pressed generic buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of pressed and interpreted generic buttons</returns>
        List<String> GetAllPressedGenericButtons(System.EventArgs args);

        /// <summary>Gets all pressed generic buttons.</summary>
        /// <param name="raw">The raw event data.</param>
        /// <returns>a list of pressed and interpreted generic buttons</returns>
        List<String> GetAllPressedGenericButtons(OrderedDictionary raw);

        /// <summary>
        /// Gets all released generic buttons.
        /// </summary>
        /// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data and all current keys states.</param>
        /// <returns>a list of released and interpreted generic buttons</returns>
        List<String> GetAllReleasedGenericButtons(System.EventArgs args);

        /// <summary>
        /// Gets all released generic buttons.
        /// </summary>
        /// <param name="raw">The raw event data.</param>
        /// <returns>a list of released and interpreted generic buttons</returns>
        List<String> GetAllReleasedGenericButtons(OrderedDictionary raw);

        #endregion

        #endregion
    }
}
