using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MGUI.Shared.Input.Keyboard
{
    public interface IKeyboardHandlerHost
    {
        public bool CanReceiveKeyboardInput() => true;
        public bool HasKeyboardFocus() => true;
    }

    /// <summary>Detects changes to the keyboard state between the previous and current Update ticks, but does not invoke the events. Instead, events are subscribed to and fired by <see cref="KeyboardHandler"/>.<br/>
    /// See: <see cref="KeyboardTracker.CreateHandler{T}(T, double?, bool, bool)"/>, <see cref="KeyboardTracker.CreateHandler{T}(T, InputUpdatePriority, bool, bool)"/><para/>
    /// This class is automatically instantiated when creating an instance of <see cref="InputTracker"/>. See: <see cref="InputTracker.Keyboard"/></summary>
    public class KeyboardTracker
    {
        public static readonly ReadOnlyCollection<Keys> AllKeys = Enum.GetValues(typeof(Keys)).Cast<Keys>().ToList().AsReadOnly();

        /// <summary>The maximum amount of time that can pass between a key press and key release 
        /// to still be registed as a Click event (<see cref="KeyboardHandler.Clicked"/>)</summary>
        public TimeSpan ClickTimeThreshold { get; set; } = TimeSpan.FromMilliseconds(300);

        public InputTracker InputTracker { get; }

        private List<KeyboardHandler> _Handlers { get; }
        /// <summary>To create a <see cref="KeyboardHandler"/>, use <see cref="CreateHandler{T}(T, double?, bool, bool)"/> or <see cref="CreateHandler{T}(T, InputUpdatePriority, bool, bool)"/></summary>
        public IReadOnlyList<KeyboardHandler> Handlers => _Handlers;

        internal void RemoveHandler(KeyboardHandler Handler) => _Handlers.Remove(Handler);

        public KeyboardState PreviousState { get; private set; }
        public KeyboardState CurrentState { get; private set; }

        internal KeyboardTracker(InputTracker InputTracker)
        {
            this.InputTracker = InputTracker;
            this._Handlers = new();
        }

        /// <param name="UpdatePriority">The priority with which the handler receives keyboard events. A higher priority means this handler will have the first chance to receive and handle events.</param>
        /// <param name="AlwaysHandlesEvents">If true, the handler will always set <see cref="HandledByEventArgs{THandlerType}.HandledBy"/> to <paramref name="Owner"/> when receiving the keyboard event.</param>
        /// <param name="InvokeEvenIfHandled">If true, the handler will still receive the keyboard event even if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is true.</param>
        public KeyboardHandler CreateHandler<T>(T Owner, InputUpdatePriority UpdatePriority, bool AlwaysHandlesEvents = false, bool InvokeEvenIfHandled = false)
            where T : IKeyboardHandlerHost => CreateHandler(Owner, (int)UpdatePriority, AlwaysHandlesEvents, InvokeEvenIfHandled);

        /// <param name="UpdatePriority">The priority with which the handler receives keyboard events. A higher priority means this handler will have the first chance to receive and handle events.<para/>
        /// If null, the caller is expected to manually call <see cref="KeyboardHandler.ManualUpdate"/> themselves.<br/>
        /// If not null, <see cref="KeyboardHandler.AutoUpdate"/> will automatically be invoked when updating this <see cref="KeyboardTracker"/><para/>
        /// Minimum Value = 0. Maximum Value = 100. Recommended to use preset values from <see cref="InputUpdatePriority"/>.</param>
        /// <param name="AlwaysHandlesEvents">If true, the handler will always set <see cref="HandledByEventArgs{THandlerType}.HandledBy"/> to <paramref name="Owner"/> when receiving the keyboard event.</param>
        /// <param name="InvokeEvenIfHandled">If true, the handler will still receive the keyboard event even if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is true.</param>
        public KeyboardHandler CreateHandler<T>(T Owner, double? UpdatePriority, bool AlwaysHandlesEvents = false, bool InvokeEvenIfHandled = false)
            where T : IKeyboardHandlerHost
        {
            KeyboardHandler Handler = new(this, Owner, UpdatePriority, AlwaysHandlesEvents, InvokeEvenIfHandled);
            _Handlers.Add(Handler);
            return Handler;
        }

        #region Events
        /// <summary>The most recent Key Press events that have occurred, even if they occurred on a prior Update tick. These values are set back to null when the key is released.</summary>
        private readonly Dictionary<Keys, BaseKeyPressedEventArgs> RecentKeyPressedEvents = AllKeys.ToDictionary(x => x, x => null as BaseKeyPressedEventArgs);

        private readonly Dictionary<Keys, BaseKeyPressedEventArgs> _CurrentKeyPressedEvents = AllKeys.ToDictionary(x => x, x => null as BaseKeyPressedEventArgs);
        /// <summary>The Key Press events that occurred on the current Update tick, or null if the key wasn't just pressed on the current Update tick.</summary>
        public IReadOnlyDictionary<Keys, BaseKeyPressedEventArgs> CurrentKeyPressedEvents => _CurrentKeyPressedEvents;

        private readonly Dictionary<Keys, BaseKeyReleasedEventArgs> _CurrentKeyReleasedEvents = AllKeys.ToDictionary(x => x, x => null as BaseKeyReleasedEventArgs);
        /// <summary>The Key Release events that occurred on the current Update tick, or null if the key wasn't just released on the current Update tick.</summary>
        public IReadOnlyDictionary<Keys, BaseKeyReleasedEventArgs> CurrentKeyReleasedEvents => _CurrentKeyReleasedEvents;

        private readonly Dictionary<Keys, BaseKeyClickedEventArgs> _CurrentKeyClickedEvents = AllKeys.ToDictionary(x => x, x => null as BaseKeyClickedEventArgs);
        /// <summary>The Key click events that occurred on the current Update tick, or null if the key wasn't just clicked on the current Update tick.</summary>
        public IReadOnlyDictionary<Keys, BaseKeyClickedEventArgs> CurrentKeyClickedEvents => _CurrentKeyClickedEvents;
        #endregion Events

        public bool IsPressed(Keys Key) => RecentKeyPressedEvents[Key] != null;
        public bool IsAnyPressed(params Keys[] Keys) => Keys.Any(x => IsPressed(x));
        public bool IsAllPressed(params Keys[] Keys) => Keys.All(x => IsPressed(x));

        public bool IsCapsLockOn => CurrentState.CapsLock;
        public bool IsNumLockOn => CurrentState.NumLock;

        public bool IsShiftDown => IsAnyPressed(Keys.LeftShift, Keys.RightShift);
        public bool IsControlDown => IsAnyPressed(Keys.LeftControl, Keys.RightControl);
        public bool IsAltDown => IsAnyPressed(Keys.LeftAlt, Keys.RightAlt);

        internal void Update(UpdateBaseArgs BA)
        {
            PreviousState = CurrentState;
            CurrentState = BA.KeyboardState;

            foreach (Keys Key in AllKeys)
            {
                _CurrentKeyPressedEvents[Key] = null;
                _CurrentKeyReleasedEvents[Key] = null;
                _CurrentKeyClickedEvents[Key] = null;
            }

            List<Keys> PreviousKeys = PreviousState.GetPressedKeys().ToList();
            List<Keys> CurrentKeys = CurrentState.GetPressedKeys().ToList();

            //  Detect keys that were just pressed
            foreach (Keys Key in CurrentKeys)
            {
                if (!PreviousKeys.Contains(Key))
                {
                    string KeyValue = KeyToTextInputString(Key);
                    BaseKeyPressedEventArgs PressedArgs = new(this, Key, KeyValue);
                    RecentKeyPressedEvents[Key] = PressedArgs;
                    _CurrentKeyPressedEvents[Key] = PressedArgs;
                }
            }

            //  Detect keys that were just released
            foreach (Keys Key in PreviousKeys)
            {
                if (!CurrentKeys.Contains(Key))
                {
                    string KeyValue = KeyToTextInputString(Key);

                    if (RecentKeyPressedEvents.TryGetValue(Key, out BaseKeyPressedEventArgs PressedArgs) && PressedArgs != null)
                    {
                        BaseKeyReleasedEventArgs ReleasedArgs = new(this, PressedArgs, Key, KeyValue);
                        _CurrentKeyReleasedEvents[Key] = ReleasedArgs;

                        //  Detect keys that were clicked
                        if (ReleasedArgs.HeldDuration <= ClickTimeThreshold)
                        {
                            BaseKeyClickedEventArgs ClickedArgs = new(this, ReleasedArgs, Key, KeyValue);
                            _CurrentKeyClickedEvents[Key] = ClickedArgs;
                        }

                        RecentKeyPressedEvents[Key] = null;
                    }
                }
            }
        }

        /// <summary>Should be invoked exactly once per Update tick.<para/>
        /// This method will invoke any pending keyboard events on its <see cref="Handlers"/> where <see cref="KeyboardHandler.IsManualUpdate"/> is false.</summary>
        public void UpdateHandlers()
        {
            IOrderedEnumerable<KeyboardHandler> SortedHandlers = Handlers.Where(x => !x.IsManualUpdate).OrderByDescending(x => x.UpdatePriority.Value);
            foreach (IGrouping<double, KeyboardHandler> Group in SortedHandlers.GroupBy(x => x.UpdatePriority.Value).OrderByDescending(x => x.Key))
            {
                foreach (KeyboardHandler Handler in Group)
                {
                    Handler.AutoUpdate();
                }
            }
        }

        private record struct PrintableKeyString(Keys Key, string ShiftValue, string NonShiftValue)
        {
            public string Value(bool IsShiftDown) => IsShiftDown ? ShiftValue : NonShiftValue;
        }

        private static readonly ReadOnlyCollection<PrintableKeyString> Letters = Enumerable.Range((int)Keys.A, (int)Keys.Z - (int)Keys.A + 1).Select(x => (Keys)x)
            .Select(x => new PrintableKeyString(x, x.ToString().ToUpper(), x.ToString().ToLower())).ToList().AsReadOnly();

        private static readonly Dictionary<Keys, PrintableKeyString> PrintableKeyValues = new List<PrintableKeyString>()
        {
            new(Keys.Space, " ", " "),

            new(Keys.D1, "!", "1"),
            new(Keys.D2, "@", "2"),
            new(Keys.D3, "#", "3"),
            new(Keys.D4, "$", "4"),
            new(Keys.D5, "%", "5"),
            new(Keys.D6, "^", "6"),
            new(Keys.D7, "&", "7"),
            new(Keys.D8, "*", "8"),
            new(Keys.D9, "(", "9"),
            new(Keys.D0, ")", "0"),

            new(Keys.NumPad1, "1", "1"),
            new(Keys.NumPad2, "2", "2"),
            new(Keys.NumPad3, "3", "3"),
            new(Keys.NumPad4, "4", "4"),
            new(Keys.NumPad5, "5", "5"),
            new(Keys.NumPad6, "6", "6"),
            new(Keys.NumPad7, "7", "7"),
            new(Keys.NumPad8, "8", "8"),
            new(Keys.NumPad9, "9", "9"),
            new(Keys.NumPad0, "0", "0"),

            new(Keys.OemTilde, "~", "`"),

            new(Keys.OemMinus, "_", "-"),
            new(Keys.OemPlus, "+", "="),
            new(Keys.OemOpenBrackets, "{", "["),
            new(Keys.OemCloseBrackets, "}", "]"),
            new(Keys.OemPipe, "|", @"\"),
            new(Keys.OemSemicolon, ":", ";"),
            new(Keys.OemQuotes, "\"", "'"),
            new(Keys.OemComma, "<", ","),
            new(Keys.OemPeriod, ">", "."),
            new(Keys.OemQuestion, "?", "/"),

            new(Keys.Multiply, "*", "*"),
            new(Keys.Add, "+", "+"),
            new(Keys.Subtract, "-", "-"),
            new(Keys.Divide, "/", "/"),

            new(Keys.Decimal, ".", ".")
        }.Union(Letters).ToDictionary(x => x.Key, x => x);

        private static readonly ReadOnlyCollection<Keys> FunctionKeys = Enumerable.Range((int)Keys.F1, (int)Keys.F24 - (int)Keys.F1 + 1).Select(x => (Keys)x).ToList().AsReadOnly();

        private static readonly HashSet<Keys> NonPrintableKeys = new List<Keys>()
        {
            Keys.None,
            Keys.Back,
            Keys.CapsLock,
            Keys.Escape,
            Keys.PageUp,
            Keys.PageDown,
            Keys.End,
            Keys.Home,
            Keys.Left, Keys.Up, Keys.Right, Keys.Down,
            Keys.Select,
            Keys.Print,
            Keys.Execute,
            Keys.PrintScreen,
            Keys.Insert,
            Keys.Delete,
            Keys.Help,
            Keys.LeftWindows, Keys.RightWindows,
            Keys.Apps,
            Keys.Sleep,
            Keys.NumLock,
            Keys.Scroll,
            Keys.LeftShift, Keys.RightShift,
            Keys.LeftControl, Keys.RightControl,
            Keys.LeftAlt, Keys.RightAlt,
            Keys.BrowserBack, Keys.BrowserForward, Keys.BrowserRefresh, Keys.BrowserStop, Keys.BrowserSearch, Keys.BrowserFavorites, Keys.BrowserHome,
            Keys.VolumeMute, Keys.VolumeDown, Keys.VolumeUp,
            Keys.MediaNextTrack, Keys.MediaPreviousTrack, Keys.MediaStop, Keys.MediaPlayPause, Keys.SelectMedia,
            Keys.LaunchMail, Keys.LaunchApplication1, Keys.LaunchApplication2,
            Keys.ProcessKey,
            Keys.Attn,
            Keys.Crsel, Keys.Exsel,
            Keys.EraseEof,
            Keys.Play, Keys.Pause,
            Keys.Zoom,
            Keys.OemClear,
            Keys.ChatPadGreen, Keys.ChatPadOrange,
            Keys.ImeConvert, Keys.ImeNoConvert,
            Keys.Kana, Keys.Kanji,
            Keys.OemCopy,
            Keys.OemEnlW
        }.Union(FunctionKeys).ToHashSet();

        //  I have no idea what these keys are
        private static readonly HashSet<Keys> UnknownKeys = new List<Keys>()
        {
            Keys.Separator,
            Keys.Oem8,
            Keys.OemBackslash,
            Keys.Pa1,
            Keys.OemAuto,
        }.ToHashSet();

        /// <param name="EnterValue">Note: Windows typically uses "\r\n", Mac '\r', Linux '\n'.</param>
        public string KeyToTextInputString(Keys Key, string NonPrintableKeyValue = null, string TabValue = "    ", string EnterValue = "\n")
        {
            #region All Keys
            /*
             * None = 0,                Back = 8,                   Tab = 9,            Enter = 13,             CapsLock = 20,          Escape = 27,
             * Space = 0x20,            PageUp = 33,                PageDown = 34,      End = 35,               Home = 36,              Left = 37,
             * Up = 38,                 Right = 39,                 Down = 40,          Select = 41,            Print = 42,             Execute = 43,
             * PrintScreen = 44,        Insert = 45,                Delete = 46,        Help = 47,              D0 = 48,                D1 = 49,
             * D2 = 50,                 D3 = 51,                    D4 = 52,            D5 = 53,                D6 = 54,                D7 = 55,
             * D8 = 56,                 D9 = 57,                    A = 65,             B = 66,                 C = 67,                 D = 68,
             * E = 69,                  F = 70,                     G = 71,             H = 72,                 I = 73,                 J = 74,
             * K = 75,                  L = 76,                     M = 77,             N = 78,                 O = 79,                 P = 80,
             * Q = 81,                  R = 82,                     S = 83,             T = 84,                 U = 85,                 V = 86,
             * W = 87,                  X = 88,                     Y = 89,             Z = 90,                 LeftWindows = 91,       RightWindows = 92,
             * Apps = 93,               Sleep = 95,                 NumPad0 = 96,       NumPad1 = 97,           NumPad2 = 98,           NumPad3 = 99,
             * NumPad4 = 100,           NumPad5 = 101,              NumPad6 = 102,      NumPad7 = 103,          NumPad8 = 104,          NumPad9 = 105,
             * Multiply = 106,          Add = 107,                  Separator = 108,    Subtract = 109,         Decimal = 110,          Divide = 111,
             * F1 = 112,                F2 = 113,                   F3 = 114,           F4 = 115,               F5 = 116,               F6 = 117,
             * F7 = 118,                F8 = 119,                   F9 = 120,           F10 = 121,              F11 = 122,              F12 = 123,
             * F13 = 124,               F14 = 125,                  F15 = 126,          F16 = 0x7F,             F17 = 0x80,             F18 = 129,
             * F19 = 130,               F20 = 131,                  F21 = 132,          F22 = 133,              F23 = 134,              F24 = 135,
             * NumLock = 144,           Scroll = 145,               LeftShift = 160,    RightShift = 161,       LeftControl = 162,      RightControl = 163,
             * LeftAlt = 164,           RightAlt = 165,             BrowserBack = 166,  BrowserForward = 167,   BrowserRefresh = 168,   BrowserStop = 169,
             * BrowserSearch = 170,     BrowserFavorites = 171,     BrowserHome = 172,  VolumeMute = 173,       VolumeDown = 174,       VolumeUp = 175,
             * MediaNextTrack = 176,    MediaPreviousTrack = 177,   MediaStop = 178,    MediaPlayPause = 179,   LaunchMail = 180,       SelectMedia = 181,
             * LaunchApplication1 = 182,LaunchApplication2 = 183,   OemSemicolon = 186, OemPlus = 187,          OemComma = 188,         OemMinus = 189,
             * OemPeriod = 190,         OemQuestion = 191,          OemTilde = 192,     OemOpenBrackets = 219,  OemPipe = 220,          OemCloseBrackets = 221,
             * OemQuotes = 222,         Oem8 = 223,                 OemBackslash = 226, ProcessKey = 229,       Attn = 246,             Crsel = 247,
             * Exsel = 248,             EraseEof = 249,             Play = 250,         Zoom = 251,             Pa1 = 253,              OemClear = 254,
             * ChatPadGreen = 202,      ChatPadOrange = 203,        Pause = 19,         ImeConvert = 28,        ImeNoConvert = 29,      Kana = 21,
             * Kanji = 25,              OemAuto = 243,              OemCopy = 242,      OemEnlW = 244
             */
            #endregion All Keys

            bool IsShiftDown = this.IsShiftDown;
            if (IsCapsLockOn)
            {
                bool IsLetter = Key >= Keys.A && Key <= Keys.Z;
                if (IsLetter)
                    IsShiftDown = !IsShiftDown;
            }

            if (PrintableKeyValues.TryGetValue(Key, out PrintableKeyString PrintableValue))
                return PrintableValue.Value(IsShiftDown);
            else if (NonPrintableKeys.Contains(Key))
                return NonPrintableKeyValue;
            else if (Key == Keys.Enter)
                return EnterValue;
            else if (Key == Keys.Tab)
                return TabValue;
            else
                throw new NotImplementedException($"{nameof(Keys)}.{Key}");
        }
    }
}
