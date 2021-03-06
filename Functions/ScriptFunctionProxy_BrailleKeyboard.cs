﻿using BrailleIO.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace tud.mci.tangram.TangramLector
{
    /// <summary>
    /// Class that interprets input commands from the interaction manager 
    /// and forward them to listeners who handle those interactions.
    /// 
    /// This particular partial class interprets braille keyboard button combinations.
    /// </summary>
    public partial class ScriptFunctionProxy
    {

        #region Modifier marks --- TODO: find a better solution

        static int CtrlModifier { get; set; }
        static int AltModifier { get; set; }
        static int ShiftModifier { get; set; }
        static int WinModifier { get; set; }


        #endregion


        //TODO: get the table by config or a default
        public readonly Control.BrailleKeyboard BrailleKeyboard =
            new Control.BrailleKeyboard(Control.BrailleKeyboard.GetCurrentDllDirectory() +
                @"/config/tables/de-chardefs8.cti");

        //TODO: handle unimplemented commands
        private void interpretBrailleKeyboardCommand(List<String> keys)
        {
            if (keys.Count < 11)
            {
                BrailleCommand command = BrailleCommand.None;
                String c = null;
                List<String> restKeys = keys;

                #region Length 1
                if (keys.Count == 1)
                {
                    if (keys.Contains("k8")) { fireBrailleKeybordCommandEvent(BrailleCommand.Return, c); return; }
                    else if (keys.Contains("lr") || keys.Contains("rl")) { fireBrailleKeybordKeyEvent(" "); return; }
                    else if (keys.Contains("l")) { fireBrailleKeybordCommandEvent(BrailleCommand.Unkown, "l"); return; }
                }

                #endregion

                if (keys.Count > 1)
                {
                    #region known commands with the space chars

                    if (keys.Count == 2 && keys.Contains("lr") && keys.Contains("rl")) // check if backspace command
                    {
                        fireBrailleKeybordCommandEvent(BrailleCommand.Backspace, c);
                        return;
                    }
                    if (keys.Count == 2 && keys.Contains("l") && keys.Contains("lr")) // check if "show Braille focus in GUI" command
                    {
                        fireBrailleKeybordCommandEvent(BrailleCommand.Unkown, "l+lr");
                        return;
                    }

                    // all commands containing the space modifier
                    if (keys.Contains("lr") || keys.Contains("rl") && !(keys.Contains("lr") && keys.Contains("rl")))
                    {
                        restKeys.Remove("lr");
                        restKeys.Remove("rl");

                        switch (restKeys.Count)
                        {
                            #region Command length 2
                            case 1:
                                switch (restKeys[0])
                                {
                                    case "k7":
                                        fireBrailleKeybordCommandEvent(BrailleCommand.ESC, c);
                                        return;
                                    case "k1":
                                        fireBrailleKeybordCommandEvent(BrailleCommand.CursorLeft, c);
                                        return;
                                    case "k4":
                                        fireBrailleKeybordCommandEvent(BrailleCommand.CursorRight, c);
                                        return;
                                    case "k2":
                                        fireBrailleKeybordCommandEvent(BrailleCommand.CursorUp, c);
                                        return;
                                    case "k5":
                                        fireBrailleKeybordCommandEvent(BrailleCommand.CursorDown, c);
                                        return;
                                    case "k3":
                                        fireBrailleKeybordCommandEvent(BrailleCommand.PageUp, c);
                                        return;
                                    case "k6":
                                        fireBrailleKeybordCommandEvent(BrailleCommand.PageDown, c);
                                        return;
                                    default:
                                        break;
                                }
                                break;
                            #endregion

                            #region Command length 3
                            case 2:
                                if (restKeys.Intersect(new List<String> { "k5", "k6" }).ToList().Count == 2)
                                {
                                    fireBrailleKeybordCommandEvent(BrailleCommand.Del, c);
                                    return;
                                }
                                else if (restKeys.Intersect(new List<String> { "k1", "k2" }).ToList().Count == 2)
                                {
                                    fireBrailleKeybordCommandEvent(BrailleCommand.Pos1, c);
                                    return;
                                }
                                else if (restKeys.Intersect(new List<String> { "k4", "k5" }).ToList().Count == 2)
                                {
                                    fireBrailleKeybordCommandEvent(BrailleCommand.End, c);
                                    return;
                                }
                                break;
                            #endregion

                            #region Command length 4
                            case 3:
                                if (restKeys.Intersect(new List<String> { "k4", "k5", "k6" }).ToList().Count == 3)
                                {
                                    fireBrailleKeybordCommandEvent(BrailleCommand.Tab, c);
                                    return;
                                }
                                else if (restKeys.Intersect(new List<String> { "k1", "k2", "k3" }).ToList().Count == 3)
                                {
                                    fireBrailleKeybordCommandEvent(BrailleCommand.ShiftTab, c);
                                    return;
                                }
                                break;
                            #endregion

                            default:
                                break;
                        }
                        restKeys = keys;
                    }
                    #endregion

                    //check if there is a Ctr. command
                    if (keys.Contains("k7") && keys.Contains("k8"))
                    {
                        //if(keys.Count == 5)
                        //{
                        //    // check if backspace command
                        //    if (keys.Contains("k1") && keys.Contains("k2") && keys.Contains("k5") && keys.Contains("k7") && keys.Contains("k8"))
                        //    {
                        //        fireBrailleKeybordCommandEvent(BrailleCommand.Backspace, c);
                        //        return;
                        //    }
                        //}
                        command = BrailleCommand.Ctr;
                        restKeys.Remove("k7");
                        restKeys.Remove("k8");
                    }
                }

                //try to get a ordered braille dot string
                string dotPattern = "";

                if (restKeys.Contains("k1")) dotPattern += "1";
                if (restKeys.Contains("k2")) dotPattern += "2";
                if (restKeys.Contains("k3")) dotPattern += "3";
                if (restKeys.Contains("k4")) dotPattern += "4";
                if (restKeys.Contains("k5")) dotPattern += "5";
                if (restKeys.Contains("k6")) dotPattern += "6";
                if (restKeys.Contains("k7")) dotPattern += "7";
                if (restKeys.Contains("k8")) dotPattern += "8";

                c = BrailleKeyboard != null ? BrailleKeyboard.GetCharFromDots(dotPattern) : null;

                if (restKeys.Count > dotPattern.Length) return;

                if (command != BrailleCommand.None)
                {
                    fireBrailleKeybordCommandEvent(command, c);
                }
                else
                {
                    fireBrailleKeybordKeyEvent(c);
                }
                //System.Diagnostics.Debug.WriteLine("Dot pattern: " + dotPattern + " is '" + c + "'");
            }
        }


        private bool interpretBrailleKeyboardCommand(ButtonReleasedEventArgs e)
        {

            CtrlModifier = 40960; // (int)(BrailleIO_BrailleKeyboardButton.k7 | BrailleIO_BrailleKeyboardButton.k8);
            AltModifier = -1;
            ShiftModifier = -2;
            WinModifier = -3;


            // TODO: make the character mapping

            BrailleIO_BrailleKeyboardButton btns;
            // check for modifier
            BrailleCommand command = filterCommand(e.ReleasedBrailleKeyboardKeys, out btns);

            // get the char
            String braille = getBrailleStringFormKeyCombination(btns);

            // fire event
            if (command != BrailleCommand.None)
            {
                fireBrailleKeybordCommandEvent(command, braille);
                return true;
            }
            else if (!String.IsNullOrEmpty(braille))
            {
                fireBrailleKeybordKeyEvent(braille);
                return true;
            }
            return false;
        }

        private bool interpretBrailleKeyboardCommand(FunctionCallInteractionEventArgs e)
        {


            if (e != null && !String.IsNullOrEmpty(e.Function) && !e.AreButtonsPressed())
            {
                // TODO: make this global, configurable and only once


                //BrailleCommand command = BrailleCommand.None;
                String c = null;
                // List<String> restKeys = keys;

                /////// stand alone functions
                switch (e.Function)
                {
                    case "bkb_return":
                        fireBrailleKeybordCommandEvent(BrailleCommand.Return, c);
                        return true;

                    case "bkb_space":
                        fireBrailleKeybordKeyEvent(" ");
                        return true;

                    case "bkb_backspace":
                        fireBrailleKeybordCommandEvent(BrailleCommand.Backspace, c);
                        return true;

                    case "bkb_esc":
                        fireBrailleKeybordCommandEvent(BrailleCommand.ESC, c);
                        return true;

                    case "bkb_left":
                        fireBrailleKeybordCommandEvent(BrailleCommand.CursorLeft, c);
                        return true;

                    case "bkb_right":
                        fireBrailleKeybordCommandEvent(BrailleCommand.CursorRight, c);
                        return true;

                    case "bkb_up":
                        fireBrailleKeybordCommandEvent(BrailleCommand.CursorUp, c);
                        return true;

                    case "bkb_down":
                        fireBrailleKeybordCommandEvent(BrailleCommand.CursorDown, c);
                        return true;

                    case "bkb_pageup":
                        fireBrailleKeybordCommandEvent(BrailleCommand.PageUp, c);
                        return true;

                    case "bkb_pagedown":
                        fireBrailleKeybordCommandEvent(BrailleCommand.PageDown, c);
                        return true;

                    case "bkb_del":
                        fireBrailleKeybordCommandEvent(BrailleCommand.Del, c);
                        return true;

                    case "bkb_pos1":
                        fireBrailleKeybordCommandEvent(BrailleCommand.Pos1, c);
                        return true;

                    case "bkb_end":
                        fireBrailleKeybordCommandEvent(BrailleCommand.End, c);
                        return true;

                    case "bkb_tab":
                        fireBrailleKeybordCommandEvent(BrailleCommand.Tab, c);
                        return true;

                    case "bkb_shiftTab":
                        fireBrailleKeybordCommandEvent(BrailleCommand.ShiftTab, c);
                        return true;

                    default:
                        break;
                }


                return interpretBrailleKeyboardCommand(e as ButtonReleasedEventArgs);

                //System.Diagnostics.Debug.WriteLine("Dot pattern: " + dotPattern + " is '" + c + "'");


            }
            return false;
        }


        #region Event Throwing

        /// <summary>
        /// Occurs when a single BrailleKeyboard letter was entered.
        /// </summary>
        public event EventHandler<BrailleKeyboardEventArgs> BrailleKeyboardKey;
        /// <summary>
        /// Occurs when a complex braille keyboard command was entered.
        /// </summary>
        public event EventHandler<BrailleKeyboardCommandEventArgs> BrailleKeyboardCommand;

        private void fireBrailleKeybordKeyEvent(String c)
        {
            Logger.Instance.Log(LogPriority.MIDDLE, this, "[BRAILLE KEYBOARD] BrailleKeyboardKey: '" + c + "'");
            System.Diagnostics.Debug.WriteLine("::: BrailleKeyboard: '" + c + "'");
            try
            {
                if (BrailleKeyboardKey != null && !String.IsNullOrEmpty(c))
                {
                    BrailleKeyboardKey.DynamicInvoke(this, new BrailleKeyboardEventArgs(c));
                }
            }
            catch { }
        }
        private void fireBrailleKeybordCommandEvent(BrailleCommand command, String c)
        {
            Logger.Instance.Log(LogPriority.MIDDLE, this, "[BRAILLE KEYBOARD] BrailleKeyboardCommand: " + command.ToString() + " + '" + c + "'");
            System.Diagnostics.Debug.WriteLine("::: BrailleKeyboardCommand: " + command.ToString() + " + '" + c + "'");
            try
            {
                if (BrailleKeyboardCommand != null && command != BrailleCommand.None)
                {
                    BrailleKeyboardCommand.DynamicInvoke(this, new BrailleKeyboardCommandEventArgs(command, c));
                }
            }
            catch { }
        }

        #endregion

        #region utils

        string getBrailleStringFormKeyCombination(BrailleIO_BrailleKeyboardButton kbbtn)
        {
            string result = String.Empty;

            if (kbbtn != BrailleIO_BrailleKeyboardButton.None &&
                !kbbtn.HasFlag(BrailleIO_BrailleKeyboardButton.Unknown))
            {
                string dotPattern = String.Empty;

                if (kbbtn.HasFlag(BrailleIO_BrailleKeyboardButton.k1)) dotPattern += "1";
                if (kbbtn.HasFlag(BrailleIO_BrailleKeyboardButton.k2)) dotPattern += "2";
                if (kbbtn.HasFlag(BrailleIO_BrailleKeyboardButton.k3)) dotPattern += "3";
                if (kbbtn.HasFlag(BrailleIO_BrailleKeyboardButton.k4)) dotPattern += "4";
                if (kbbtn.HasFlag(BrailleIO_BrailleKeyboardButton.k5)) dotPattern += "5";
                if (kbbtn.HasFlag(BrailleIO_BrailleKeyboardButton.k6)) dotPattern += "6";
                if (kbbtn.HasFlag(BrailleIO_BrailleKeyboardButton.k7)) dotPattern += "7";
                if (kbbtn.HasFlag(BrailleIO_BrailleKeyboardButton.k8)) dotPattern += "8";

                result = BrailleKeyboard != null ? BrailleKeyboard.GetCharFromDots(dotPattern) : null;
            }

            return result;
        }

        BrailleCommand filterCommand(BrailleIO_BrailleKeyboardButton kbbtn,
            out BrailleIO_BrailleKeyboardButton filtered)
        {
            filtered = kbbtn;
            BrailleCommand cmd = BrailleCommand.None;
            int btncode = (int)kbbtn;

            if ((btncode & CtrlModifier) == CtrlModifier)
            {
                cmd |= BrailleCommand.Ctr;
                btncode &= ~CtrlModifier;
            }

            if ((btncode & AltModifier) == AltModifier)
            {
                cmd |= BrailleCommand.Alt;
                btncode &= ~AltModifier;
            }

            if ((btncode & ShiftModifier) == ShiftModifier)
            {
                cmd |= BrailleCommand.Shift;
                btncode &= ~ShiftModifier;
            }

            if ((btncode & WinModifier) == WinModifier)
            {
                cmd |= BrailleCommand.Win;
                btncode &= ~WinModifier;
            }

            filtered = (BrailleIO_BrailleKeyboardButton)btncode;

            return cmd;
        }


        #endregion


    }

    #region enums

    public enum BrailleCommand
    {
        None,
        Unkown,
        Return,
        Space,
        Backspace,
        Del,
        ESC,
        Ctr,
        CursorLeft,
        CursorRight,
        CursorUp,
        CursorDown,
        PageUp,
        PageDown,
        Pos1,
        End,
        Tab,
        ShiftTab,
        Shift,
        Alt,
        Win
    }

    #endregion

    #region EvnetArgs

    /// <summary>
    /// Event arguments for a single Braille Letter input.
    /// </summary>
    public class BrailleKeyboardEventArgs : EventArgs
    {
        /// <summary>
        /// The entered Braille character
        /// </summary>
        public readonly String Character;
        /// <summary>
        /// Initializes a new instance of the <see cref="BrailleKeyboardEventArgs"/> class.
        /// </summary>
        /// <param name="c">The enterd Braille letter.</param>
        public BrailleKeyboardEventArgs(String c) { this.Character = c; }
    }


    /// <summary>
    /// Event arguments for a complex Braille keyboard command input.
    /// </summary>
    public class BrailleKeyboardCommandEventArgs : BrailleKeyboardEventArgs
    {
        /// <summary>
        /// The Braille keyboard command entered
        /// </summary>
        public readonly BrailleCommand Command;
        /// <summary>
        /// Initializes a new instance of the <see cref="BrailleKeyboardCommandEventArgs"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="c">The corresponding Braille letter to the command.</param>
        public BrailleKeyboardCommandEventArgs(BrailleCommand command, String c)
            : base(c)
        {
            Command = command;
        }
    }

    #endregion
}