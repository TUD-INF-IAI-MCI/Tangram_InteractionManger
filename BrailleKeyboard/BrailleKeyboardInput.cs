using System;
using System.Collections.Generic;
using System.Linq;
using tud.mci.LanguageLocalization;using tud.mci.tangram.audio;
using System.Windows.Forms;

namespace tud.mci.tangram.TangramLector.Control
{
    public class BrailleKeyboardInput : ILocalizable
    {
        #region Members

        ScriptFunctionProxy proxy = ScriptFunctionProxy.Instance;
        readonly AudioRenderer audioRenderer = AudioRenderer.Instance;

        String _input = "";
        readonly Object _inputLock = new Object();       

        /// <summary>
        /// The interpreted input string.
        /// </summary>
        public String Input
        {
            get { return _input == null ? _input = String.Empty : _input; }
            private set
            {
                lock (_inputLock)
                {
                    if (!_input.Equals(value))
                    {                                           
                        _input = value;
                        fireBrailleInputChangedEvent();
                    }
                }
            }
        }

        public String Changed
        {
            get;
            private set;
        }

        private int _caret;
        /// <summary>
        /// Gets or sets the caret position.
        /// </summary>
        /// <value>The caret.</value>
        public int Caret
        {
            get { return _caret; }
            private set { if (_caret != value) { _caret = value; fireBrailleInputCaretMovedEvent(); } }
        }

        private Selection _selection;
        /// <summary>
        /// Gets or sets the selection.
        /// </summary>
        /// <value>The selection.</value>
        public Selection Selection
        {
            get { return _selection; }
            private set { if (!_selection.Equals(value)) { _selection = value; fireBrailleInputSelectionChangedEvent(); } }
        }

        readonly List<Action<CaretMoving>> caretMovingCallbacks = new List<Action<CaretMoving>>();

        readonly LL LL = new LL (Properties.Resources.Language);

        #endregion

        #region Constructor 

        public BrailleKeyboardInput(System.Globalization.CultureInfo culture = null)
        {
            if (proxy != null)
            {
                proxy.BrailleKeyboardKey += new EventHandler<BrailleKeyboardEventArgs>(proxy_BrailleKeybordKey);
                proxy.BrailleKeyboardCommand += new EventHandler<BrailleKeyboardCommandEventArgs>(proxy_BrailleKeybordCommand);
            }

            if (culture != null && LL != null)
            {
                LL.SetStandardCulture(culture);
            }

        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Reset input and caret
        /// </summary>
        public void Reset()
        {
            Input = String.Empty;
            Caret = 0;
        }

        /// <summary>
        /// Moves the caret.
        /// </summary>
        /// <param name="offset">The offset to the actual position.</param>
        /// <returns>the new caret position</returns>
        public int MoveCaret(int offset)
        {
            int old_caret = Caret;
            Caret = Math.Min(Math.Max(0, Caret + offset), Input.Length);
            if (Caret == old_caret) audioRenderer.PlayWaveImmediately(StandardSounds.End);
            else if(!String.IsNullOrEmpty(Input) && Input.Length > Caret) audioRenderer.PlaySoundImmediately(Input.ElementAt(Caret).ToString());
            return Caret;
        }

        /// <summary>
        /// Moves the the caret to the given index.
        /// </summary>
        /// <param name="index">The new index.</param>
        /// <returns>the new caret position</returns>
        public int MoveCaretToIndex(int index)
        {
            int old_caret = Caret;
            Caret = Math.Min(Math.Max(0, index), Input.Length);
            if (Caret == old_caret) audioRenderer.PlayWaveImmediately(StandardSounds.End);
            else if (!String.IsNullOrEmpty(Input) && Input.Length > Caret) audioRenderer.PlaySoundImmediately(Input.ElementAt(Caret).ToString());
            return Caret;
        }

        /// <summary>
        /// Clears the input and the caret.
        /// </summary>
        /// <returns><code>true</code> if the reset was succesfull</returns>
        public bool ClearInput()
        {
            Input = String.Empty;
            Caret = 0;
            Changed = String.Empty;
            return true;
        }

        /// <summary>
        /// Append the given text at the end Input string.
        /// </summary>
        /// <param name="text">The text to append.</param>
        /// <returns>the new Input string</returns>
        public string InsertStringToInput(String text)
        {
            if (Caret >= Input.Length) Caret += text.Length;
            Changed = text;
            return Input += text;
        }

        /// <summary>
        /// Insert the given string to the Input string at the set index.
        /// </summary>
        /// <param name="text">The text to insert.</param>
        /// <param name="startIndex">The index where to insert the text.</param>
        /// <returns>the new Input string</returns>
        public string InsertStringToInput(String text, int startIndex)
        {
            startIndex = Math.Min(Math.Max(0, startIndex), Input.Length);
            if (Caret > startIndex) Caret += startIndex;
            Changed = text;
            if (!String.IsNullOrEmpty(text)) { Input = Input.Insert(startIndex, text); }
            
            return Input;
        }

        /// <summary>
        /// Sets the Input string and the caret to the end of the string.
        /// </summary>
        /// <param name="text">The new text.</param>
        /// <returns>the new Input string</returns>
        public string SetInput(String text)
        {
            if (text == null) text = String.Empty;
            Caret = text.Length;
            Changed = text;
            return Input = text;
        }

        /// <summary>
        /// Removes the last part of the Input string.
        /// </summary>
        /// <param name="index">The start index to remove till the end of the Input string.</param>
        /// <returns>the new Input string</returns>
        public string RemoveFromInput(int index)
        {
            if (Caret > index) Caret = Math.Max(0, index);
            Changed = String.Empty;
            return Input = Input.Remove(Math.Min(Math.Max(0, index), Input.Length));
        }

        /// <summary>
        /// Removes the last chars from input.
        /// </summary>
        /// <param name="count">The count of chars to remove from the end of the Input string.</param>
        /// <returns>the new Input string</returns>
        public string RemoveLastCharsFromInput(int count) //TODO: test this
        {
            int index = Math.Max((Input.Length - count), 0);
            if (Caret > index) Caret = index + 1;
            Changed = String.Empty;
            string removedLetter = Input.ElementAt(index).ToString();
            audioRenderer.PlaySoundImmediately(LL.GetTrans("tangram.interaction_manager.bki.delete_letter",removedLetter));
            return Input = Input.Remove(index);
        }

        /// <summary>
        /// Removes the given count of chars, which are previous to the Caret position, from input.
        /// </summary>
        /// <param name="count">The count of chars to remove from the Input string.</param>
        /// <returns>the new Input string</returns>
        public string RemoveCharsPreviousToCaret(int count)
        {
            int index_start = Math.Max((Caret - count), 0);
            int index_end = Math.Max((Caret), 0);

            return Input = RemoveSubstringFromInput(index_start, index_end);
        }

        /// <summary>
        /// Removes a subpart form the Input string.
        /// </summary>
        /// <param name="startIndex">The start index is the first index which should be removed.</param>
        /// <param name="endindex">The end index is the first index which should not be removed.</param>
        /// <returns>the new Input string</returns>
        public string RemoveSubstringFromInput(int startIndex, int endindex)//TODO: test this
        {
            startIndex = Math.Min(Math.Max(startIndex, 0), Input.Length);
            endindex = Math.Min(Math.Max(0, endindex), Input.Length);
            Changed = String.Empty;
            
            // set new input
            string input = Input.Substring(0, startIndex);
            if (endindex < Input.Length)
            {
                input = input + Input.Substring(endindex);
            }

            // move caret
            if (Caret > startIndex)
            {
                if (Caret < endindex) { Caret = startIndex + 1; }
                else { Caret -= Math.Abs(endindex - startIndex); }
            }

            // play deleted letter
            string removed = LL.GetTrans("tangram.interaction_manager.bki.letter_count", (endindex - startIndex).ToString());
            if (startIndex == endindex-1)
            {
                removed = Input.ElementAt(startIndex).ToString();
            }
            audioRenderer.PlaySoundImmediately(LL.GetTrans("tangram.interaction_manager.bki.delete_letter", removed));

            Input = input;
            return input;
        }

        /// <summary>
        /// Registers a callback function that can effect the caret position on moving commands.
        /// The callback should adapt the position of the caret to the displayed lines a.s.o.
        /// The Caret can be effected by functions such as <see cref="MoveCaretToIndex"/>.
        /// </summary>
        /// <param name="callback">The callback function.</param>
        /// <returns></returns>
        public bool RegisterCaretMovingCallback(Action<CaretMoving> callback)
        {
            bool result = false;
            if (callback != null)
            {
                caretMovingCallbacks.Add(callback);
            }
            return result;
        }

        #endregion

        #region Events

        public event EventHandler<InputChangedEventArgs> BrailleInputChanged;
        public event EventHandler<CaretMovedEvent> BrailleInputCaretMoved;
        public event EventHandler<SelectionChangedEventArgs> BrailleInputSelectionChanged;

        private void fireBrailleInputChangedEvent()
        {
            try
            {
                Logger.Instance.Log(LogPriority.MIDDLE, this, "[BRAILLE KEYBOARD INPUT] Braille input changed: '" + Input + "'");
                if (BrailleInputChanged != null)
                {
                    BrailleInputChanged.DynamicInvoke(this, new InputChangedEventArgs(Input,Changed));
                }
            }
            catch { }
        }
        private void fireBrailleInputCaretMovedEvent()
        {
            try
            {
                Logger.Instance.Log(LogPriority.MIDDLE, this, "[BRAILLE KEYBOARD INPUT] Braille input Caret moved to " + Caret);
                if (BrailleInputCaretMoved != null)
                {
                    BrailleInputCaretMoved.DynamicInvoke(this, new CaretMovedEvent(Input, Changed,Caret));
                }
            }
            catch { }
        }
        private void fireBrailleInputSelectionChangedEvent()
        {
            try
            {
                Logger.Instance.Log(LogPriority.MIDDLE, this, "[BRAILLE KEYBOARD INPUT] Braille input selection changed: " + Selection);
                if (BrailleInputSelectionChanged != null)
                {
                    BrailleInputSelectionChanged.DynamicInvoke(this, new SelectionChangedEventArgs(Input, Selection));
                }
            }
            catch { }
        }

        void proxy_BrailleKeybordCommand(object sender, BrailleKeyboardCommandEventArgs e)
        {
            if (e != null && e.Command != BrailleCommand.None)
            {
                if (e.Command != BrailleCommand.Ctr && e.Command != BrailleCommand.Unkown && !String.IsNullOrEmpty(e.Character))
                {
                    audioRenderer.PlayWaveImmediately(StandardSounds.Critical);
                    return;
                }

                switch (e.Command)
                {
                    case BrailleCommand.Unkown:
                        if (e.Character.Equals("l")) InteractionManager.Instance.AbortAudio();
                        break;
                    case BrailleCommand.Return:
                        EnterBrailleKey("\r\n");
                        break;
                    case BrailleCommand.Space:
                        break;
                    case BrailleCommand.Backspace:
                        RemoveCharsPreviousToCaret(1);
                        break;
                    case BrailleCommand.Del:
                        deleteCharAtPosition(Caret);
                        break;
                    case BrailleCommand.ESC:
                        break;
                    case BrailleCommand.Ctr:
                        handleControlSequence(e.Character);
                        break;
                    case BrailleCommand.CursorLeft:
                        moveCaret(1, CaretMoving.Left);
                        break;
                    case BrailleCommand.CursorRight:
                        moveCaret(1, CaretMoving.Right);
                        break;
                    case BrailleCommand.CursorUp:
                        moveCaret(1, CaretMoving.Up);
                        break;
                    case BrailleCommand.CursorDown:
                        moveCaret(1, CaretMoving.Down);
                        break;
                    case BrailleCommand.PageUp:
                        audioRenderer.PlayWaveImmediately(StandardSounds.Error);
                        break;
                    case BrailleCommand.PageDown:
                        audioRenderer.PlayWaveImmediately(StandardSounds.Error);
                        break;
                    case BrailleCommand.Pos1:
                        moveCaret(1, CaretMoving.Pos1);
                        break;
                    case BrailleCommand.End:
                        moveCaret(1, CaretMoving.Ende);
                        break;
                    case BrailleCommand.Tab:
                        EnterBrailleKey("\t");
                        break;
                    case BrailleCommand.ShiftTab:
                        removeLastTab();
                        break;
                    default:
                        break;
                }
            }
        }

        void proxy_BrailleKeybordKey(object sender, BrailleKeyboardEventArgs e)
        {
            if (e != null && e.Character != null)
            {
                EnterBrailleKey(e.Character);
            }
        }

        #endregion

        #region text handling

        static string[] stringSeparators = new string[] { " ", ",", ".", "!", "?", ":", ";", "-", "\r\n", "\t" }; //TODO: extend this or make it better

        private void EnterBrailleKey(String c)
        {
            InsertStringToInput(c, Caret);
            Caret += c.Length;

            if (c.Equals("\r\n")) c = "return";
            else if (c.Equals("\t")) c = "tab";
            audioRenderer.PlaySoundImmediately(c);

            if (String.IsNullOrWhiteSpace(c)) //FIXME: make this also for the other separators
            {
                string[] words = Input.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                if (words != null && words.Length > 0)
                {
                    audioRenderer.PlaySoundImmediately(words[words.Length - 1]);
                }
            }

        }

        /// <summary>
        /// Removes the last or first tab.
        /// </summary>
        private void removeLastTab()
        {
            if (Input.EndsWith("\t"))
            {
                RemoveLastCharsFromInput(1);
            }
            else if (Input.StartsWith("\t"))
            {
                RemoveSubstringFromInput(0, 1);
            }
            else { audioRenderer.PlayWaveImmediately(StandardSounds.Error); }
        }

        /// <summary>
        /// Handles the control sequence.
        /// </summary>
        /// <param name="p">The parameter character.</param>
        private void handleControlSequence(string p)
        {
            if (p.Length == 1)
            {
                switch (p)
                {
                    case "v":
                        insertLastTextFormClipboard();
                        break;
                    default:
                        audioRenderer.PlayWaveImmediately(StandardSounds.Error);
                        break;
                }
            }
            else
            {
                audioRenderer.PlayWaveImmediately(StandardSounds.Error);
            }
        }

        /// <summary>
        /// Inserts the last text form the clipboard.
        /// </summary>
        private void insertLastTextFormClipboard()
        {
            if (Clipboard.ContainsText())
            {
                InsertStringToInput(Clipboard.GetText());
            }
            else
            {
                audioRenderer.PlayWaveImmediately(StandardSounds.Error);
            }
        }


        private void deleteCharAtPosition(int pos)
        {
            if (pos < Input.Length)
            {
                RemoveSubstringFromInput(pos, pos + 1);
            }
            else
            {
                audioRenderer.PlayWaveImmediately(StandardSounds.Error);
            }
        }


        private void speakInput()
        {
            audioRenderer.PlaySoundImmediately(Input);
        }

        #endregion

        #region Caret handling

        private void moveCaret(int steps, CaretMoving moving)
        {
            if (caretMovingCallbacks.Count > 0)
            {
                foreach (Action<CaretMoving> action in caretMovingCallbacks)
                {
                    try
                    {
                        action.DynamicInvoke(moving);
                    }
                    catch { }
                }
            }
            else
            {
                switch (moving)
                {
                    case CaretMoving.None:
                        break;
                    case CaretMoving.Left:
                        MoveCaret(-1);
                        break;
                    case CaretMoving.Right:
                        MoveCaret(1);
                        break;
                    case CaretMoving.Up:
                        MoveCaretToIndex(Caret - 36);
                        //TODO: only hack !!! find correct number and make number dependent on device
                        break;
                    case CaretMoving.Down:
                        MoveCaretToIndex(Caret + 36);
                        //TODO: only hack !!! find correct number and make number dependent on device
                        break;
                    case CaretMoving.Pos1:
                        MoveCaretToIndex(0);
                        break;
                    case CaretMoving.Ende:
                        MoveCaretToIndex(Input.Length);
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region ILocalizable

        void ILocalizable.SetLocalizationCulture(System.Globalization.CultureInfo culture)
        {
            if (LL != null) LL.SetStandardCulture(culture);
        }

        #endregion

    }

    #region Event Args
    public class InputChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The String entered by the keyboard
        /// </summary>
        public readonly String Input;
        /// <summary>
        /// The String is the changed input, e.g. the enter character.
        /// On Deletion this is always empty.
        /// </summary>
        public readonly String Changed;
        public InputChangedEventArgs(String input, String changed) : base() { this.Input = input; this.Changed = changed; }
        
    }

    public class CaretMovedEvent : InputChangedEventArgs
    {
        /// <summary>
        /// The position of the caret (cursor) inside the entered string.
        /// </summary>
        public readonly int CaretPosition;
        public CaretMovedEvent(String input, String changed, int caretPos)
            : base(input, changed)
        {
            CaretPosition = (Math.Min(Math.Max(0, caretPos), String.IsNullOrEmpty(Input) ? 0 : input.Length));
        }
    }

    public class SelectionChangedEventArgs : InputChangedEventArgs
    {
        /// <summary>
        /// The selection marked by the user.
        /// </summary>
        public readonly Selection Selection;
        public SelectionChangedEventArgs(String input, Selection selection)
            : base(input,String.Empty)
        {
            Selection = selection;
        }
    }

    #endregion

    #region Structs
    public struct Selection
    {
        /// <summary>
        /// The start index inside the string
        /// </summary>
        public int Start;
        /// <summary>
        /// The end index inside the string
        /// </summary>
        public int End;
        /// <summary>
        /// The selected string 
        /// </summary>
        public String Selected;

        /// <summary>
        /// Initializes a new instance of the <see cref="Selection"/> struct.
        /// </summary>
        /// <param name="start">The start index inside the string.</param>
        /// <param name="end">The end index inside the string.</param>
        /// <param name="selection">The selected string.</param>
        public Selection(int start, int end, string selection)
        {
            Start = Math.Max(0, start);
            End = Math.Min(Start, end);
            Selected = selection;
        }

        public override bool Equals(object obj)
        {
            if (obj is Selection)
            {
                Selection s2 = ((Selection)obj);
                return Start.Equals(s2.Start) && End.Equals(s2.End) && Selected.Equals(s2.Selected);
            }
            return false;
        }

        public override int GetHashCode() { return base.GetHashCode(); }
    }
    #endregion

    #region Enums

    public enum CaretMoving
    {
        None,
        Left,
        Right,
        Up,
        Down,
        Pos1,
        Ende
    }

    #endregion

}
