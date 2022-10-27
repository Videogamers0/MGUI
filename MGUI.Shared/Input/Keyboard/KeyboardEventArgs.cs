using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MGUI.Shared.Input.Keyboard
{
    public class BaseKeyPressedEventArgs : HandledByEventArgs<IKeyboardHandlerHost>
    {
        private static readonly Regex Alphanumeric = new(@"^[A-Za-z0-9]$");
        public bool IsAlphanumeric() => IsPrintableKey && Alphanumeric.IsMatch(PrintableValue);

        public KeyboardTracker Tracker { get; }

        public DateTime PressedAt { get; }
        public Keys Key { get; }
        public bool IsPrintableKey { get; }
        public string PrintableValue { get; }

        public BaseKeyPressedEventArgs(KeyboardTracker Tracker, Keys Key, string PrintableValue)
            : base()
        {
            this.Tracker = Tracker;
            this.PressedAt = DateTime.Now;
            this.Key = Key;
            this.IsPrintableKey = !string.IsNullOrEmpty(PrintableValue);
            this.PrintableValue = PrintableValue;
        }
    }

    public class BaseKeyReleasedEventArgs : HandledByEventArgs<IKeyboardHandlerHost>
    {
        private static readonly Regex Alphanumeric = new(@"^[A-Za-z0-9]$");
        public bool IsAlphanumeric() => IsPrintableKey && Alphanumeric.IsMatch(PrintableValue);

        public KeyboardTracker Tracker { get; }

        public BaseKeyPressedEventArgs PressedArgs { get; }

        public DateTime ReleasedAt { get; }

        public Keys Key { get; }
        public bool IsPrintableKey { get; }
        public string PrintableValue { get; }

        public TimeSpan HeldDuration => ReleasedAt.Subtract(PressedArgs.PressedAt);

        public BaseKeyReleasedEventArgs(KeyboardTracker Tracker, BaseKeyPressedEventArgs PressedArgs, Keys Key, string PrintableValue)
            : base()
        {
            this.Tracker = Tracker;
            this.PressedArgs = PressedArgs;
            this.ReleasedAt = DateTime.Now;
            this.Key = Key;
            this.IsPrintableKey = !string.IsNullOrEmpty(PrintableValue);
            this.PrintableValue = PrintableValue;
        }
    }

    public class BaseKeyClickedEventArgs : HandledByEventArgs<IKeyboardHandlerHost>
    {
        private static readonly Regex Alphanumeric = new(@"^[A-Za-z0-9]$");
        public bool IsAlphanumeric() => IsPrintableKey && Alphanumeric.IsMatch(PrintableValue);

        public KeyboardTracker Tracker { get; }

        public BaseKeyReleasedEventArgs ReleasedArgs { get; }
        public BaseKeyPressedEventArgs PressedArgs => ReleasedArgs.PressedArgs;

        public Keys Key { get; }
        public bool IsPrintableKey { get; }
        public string PrintableValue { get; }

        public BaseKeyClickedEventArgs(KeyboardTracker Tracker, BaseKeyReleasedEventArgs ReleasedArgs, Keys Key, string PrintableValue)
            : base()
        {
            this.Tracker = Tracker;
            this.ReleasedArgs = ReleasedArgs;
            this.Key = Key;
            this.IsPrintableKey = !string.IsNullOrEmpty(PrintableValue);
            this.PrintableValue = PrintableValue;
        }
    }
}
