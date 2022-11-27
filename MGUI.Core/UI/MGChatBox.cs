using MGUI.Core.UI.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;

namespace MGUI.Core.UI
{
    public readonly record struct ChatBoxMessageData(string Username, DateTime Timestamp, string Message);

    public class MGChatBox : MGElement
    {
        private MGComponent<MGDockPanel> MainContent { get; }

        private string _TimestampFormat;
        /// <summary>A format string used to compute the text to display for each message's <see cref="DateTime"/> (<see cref="MGChatBoxMessage.Timestamp"/>)<para/>
        /// Default value: @"'\\['HH:mm:ss']'", which displays the hours/minutes/seconds surrounded by brackets, such as: "[14:33:20]"<para/>
        /// Note: Single quotes can be used as an escape character to insert any text you wish, such as:
        /// <code>@"'[color=Red] \\[ [b]' HH:mm:ss '[/b] ] [/color]'"</code><para/>
        /// Which would result in the entire timestamp text being red, with the hour/minutes/seconds surrounded by brackets and rendered in bold font.<para/>
        /// Note that the open bracket '[' is a special character that typically indicates the start of a text formatting code.<br/>
        /// If you want to use an open bracket literally, you must escape the inlined markdown by prefixing the open bracket with 2 backslash literals ('\\\\' or @"\\")</summary>
        public string TimestampFormat
        {
            get => _TimestampFormat;
            set
            {
                if (_TimestampFormat != value)
                {
                    string Previous = TimestampFormat;
                    _TimestampFormat = value;
                    TimestampFormatChanged?.Invoke(this, new(Previous, TimestampFormat));
                }
            }
        }

        /// <summary>Invoked when <see cref="TimestampFormat"/> changes.</summary>
        public event EventHandler<EventArgs<string>> TimestampFormatChanged;

        /// <summary>The <see cref="MGTextBox"/> that the user may type a new message into</summary>
        public MGTextBox InputTextBox { get; }
        /// <summary>The <see cref="MGButton"/> that commits the current message that is typed into the <see cref="InputTextBox"/></summary>
        public MGButton SendButton { get; }

        public MGChatBox(MGWindow ParentWindow)
            : base(ParentWindow, MGElementType.ChatBox)
        {
            using (BeginInitializing())
            {
                MGDockPanel DockPanel = new(ParentWindow);
                this.MainContent = new(DockPanel, false, false, true, true, false, false, true,
                    (AvailableBounds, ComponentSize) => AvailableBounds);
                AddComponent(MainContent);

                TimestampFormat = @"'\\['HH:mm:ss']'";

                DockPanel.TryAddChild(new MGChatBoxMessage(ParentWindow, this, new("ABC", DateTime.Now, "Hello World")), Dock.Left);

                //chatbox
                //      DockPanel
                //          DockPanel Dock=Bottom
                //              TextBlock Dock=Left (Contains your username)
                //              Button Dock=Right (Send message button)
                //              TextBox
                //          ScrollViewer - Alternatively, could use ListBox<string>
                //              StackPanel Orientation=Vertical
                //                  TextBlock, 1 per message
            }
        }
    }

    /// <summary>Represents a single message inside an <see cref="MGChatBox"/></summary>
    public class MGChatBoxMessage : MGElement
    {
        public MGChatBox ChatBox { get; }
        public ChatBoxMessageData MessageData { get; }

        public string Username => MessageData.Username;
        public DateTime Timestamp => MessageData.Timestamp;
        public string Message => MessageData.Message;

        private MGComponent<MGDockPanel> MainContent { get; }
        /// <summary>The <see cref="MGTextBlock"/> responsible for displaying the message's <see cref="Timestamp"/></summary>
        public MGTextBlock TimestampTextBlock { get; }
        /// <summary>The <see cref="MGTextBlock"/> responsible for displaying the author of the message (See: <see cref="Username"/>)</summary>
        public MGTextBlock UsernameTextBlock { get; }
        /// <summary>The <see cref="MGTextBlock"/> responsible for displaying the message's content (See: <see cref="Message"/>)</summary>
        public MGTextBlock MessageTextBlock { get; }

        public MGChatBoxMessage(MGWindow ParentWindow, MGChatBox ChatBox, ChatBoxMessageData Data)
            : base(ParentWindow, MGElementType.ChatBoxMessage)
        {
            using (BeginInitializing())
            {
                this.ChatBox = ChatBox;
                this.MessageData = Data;

                MGDockPanel DockPanel = new(ParentWindow);
                this.MainContent = new(DockPanel, false, false, true, true, false, false, true,
                    (AvailableBounds, ComponentSize) => AvailableBounds);
                AddComponent(MainContent);

                const int Spacing = 2;

                this.TimestampTextBlock = new(ParentWindow, Timestamp.ToString(ChatBox.TimestampFormat));
                TimestampTextBlock.Opacity = 0.75f;
                TimestampTextBlock.Margin = new(0, 0, Spacing, 0);
                this.UsernameTextBlock = new(ParentWindow, $"{Username}:");
                UsernameTextBlock.Margin = new(0, 0, Spacing, 0);
                this.MessageTextBlock = new(ParentWindow, Message);

                DockPanel.TryAddChild(TimestampTextBlock, Dock.Left);
                DockPanel.TryAddChild(UsernameTextBlock, Dock.Left);
                DockPanel.TryAddChild(MessageTextBlock, Dock.Left);
                DockPanel.CanChangeContent = false;
            }
        }
    }
}
