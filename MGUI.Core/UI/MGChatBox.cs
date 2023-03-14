using MGUI.Core.UI.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using System.Collections.ObjectModel;
using MGUI.Core.UI.Text;
using MGUI.Core.UI.Brushes.Border_Brushes;
using System.Collections.Specialized;
using Microsoft.Xna.Framework.Input;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System.Diagnostics;

namespace MGUI.Core.UI
{
    public readonly record struct ChatBoxMessageData(string Username, DateTime Timestamp, string Message);

    public class MGChatBox : MGElement
    {
        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        public IBorderBrush BorderBrush
        {
            get => BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set => BorderElement.BorderThickness = value;
        }
        #endregion Border

        private MGComponent<MGDockPanel> MainContent { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
                    NPC(nameof(TimestampFormat));
                    TimestampFormatChanged?.Invoke(this, new(Previous, TimestampFormat));
                }
            }
        }

        /// <summary>Invoked when <see cref="TimestampFormat"/> changes.</summary>
        public event EventHandler<EventArgs<string>> TimestampFormatChanged;

        /// <summary>The <see cref="MGTextBlock"/> that displays the current user's name next to the <see cref="InputTextBox"/></summary>
        public MGTextBlock CurrentUserTextBlock { get; }
        /// <summary>The <see cref="MGTextBox"/> that the user may type a new message into</summary>
        public MGTextBox InputTextBox { get; }
        /// <summary>The <see cref="MGButton"/> that commits the current message that is typed into the <see cref="InputTextBox"/></summary>
        public MGButton SendButton { get; }

        /// <summary>A separator between the messages list and the bottom portion of the chatbox that contains the <see cref="InputTextBox"/> and <see cref="SendButton"/></summary>
        public MGSeparator Separator { get; }

        public MGListBox<ChatBoxMessageData> MessagesContainer { get; }

        public ObservableCollection<ChatBoxMessageData> Messages { get; }

        public int? MaxMessageLength
        {
            get => InputTextBox.CharacterLimit;
            set => InputTextBox.CharacterLimit = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _MaxMessages;
        /// <summary>The maximum number of items that can be stored in <see cref="Messages"/>.<br/>
        /// Once this limit is reached, the oldest item will be removed to make room for new items.</summary>
        public int MaxMessages
        {
            get => _MaxMessages;
            set
            {
                if (_MaxMessages != value)
                {
                    _MaxMessages = value;
                    ValidateNumMessages();
                    NPC(nameof(MaxMessages));
                }
            }
        }

        /// <param name="MaxMessageLength">The maximum number of characters that can be sent in a single message</param>
        public MGChatBox(MGWindow ParentWindow, int MaxMessageLength = 100, int MaxMessages = 25)
            : base(ParentWindow, MGElementType.ChatBox)
        {
            using (BeginInitializing())
            {
                //this.Padding = new(3);

                this.BorderElement = new(ParentWindow, new(1), MGUniformBorderBrush.Black);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);

                MGDockPanel DockPanel = new(ParentWindow);
                this.MainContent = new(DockPanel, false, false, true, true, false, false, true,
                    (AvailableBounds, ComponentSize) => AvailableBounds.GetCompressed(Padding));
                AddComponent(MainContent);

                TimestampFormat = @"'\\['HH:mm:ss']'";

                this.CurrentUserTextBlock = new(ParentWindow, $"{Environment.UserName}:");
                CurrentUserTextBlock.IsShadowed = true;
                CurrentUserTextBlock.Margin = new(0, 0, 2, 0);
                CurrentUserTextBlock.VerticalAlignment = VerticalAlignment.Center;
                CurrentUserTextBlock.ManagedParent = this;

                this.InputTextBox = new(ParentWindow, MaxMessageLength);
                InputTextBox.AcceptsReturn = false;
                InputTextBox.ManagedParent = this;
                InputTextBox.OnCharacterLimitChanged += (sender, e) => { NPC(nameof(MaxMessageLength)); };
                InputTextBox.KeyboardHandler.Pressed += (sender, e) =>
                {
                    if (e.Key == Keys.Enter)
                    {
                        SendMessage();
                        e.SetHandledBy(this);
                    }
                };

                this.SendButton = new(ParentWindow, btn => { SendMessage(); });
                SendButton.SetContent("Send");
                SendButton.ManagedParent = this;

                this.Separator = new(ParentWindow, Orientation.Horizontal, 1);
                Separator.BackgroundBrush.SetAll(MGSolidFillBrush.Black);
                Separator.Margin = new(0);
                Separator.ManagedParent = this;

                MGDockPanel Footer = new(ParentWindow);
                Footer.Margin = new(4, 2, 2, 2);
                Footer.TryAddChild(CurrentUserTextBlock, Dock.Left);
                Footer.TryAddChild(SendButton, Dock.Right);
                Footer.TryAddChild(InputTextBox, Dock.Left);
                Footer.ManagedParent = this;
                Footer.CanChangeContent = false;

                this.Messages = new();
                Messages.CollectionChanged += (sender, e) =>
                {
                    if (e.Action is NotifyCollectionChangedAction.Add)
                        IsMessagesCountRefreshPending = true;
                };
                this.MessagesContainer = new(ParentWindow);
                MessagesContainer.SetItemsSource(this.Messages);
                MessagesContainer.IsTitleVisible = false;
                MessagesContainer.SelectionMode = ListBoxSelectionMode.None;
                MessagesContainer.OuterBorderThickness = new(0);
                MessagesContainer.TitleBorderThickness = new(0);
                MessagesContainer.InnerBorderThickness = new(0);
                MessagesContainer.ItemsPanel.BorderThickness = new(0);
                MessagesContainer.MinHeight = 0;
                MessagesContainer.ItemContainerStyle = (contentPresenter) =>
                {
                    MessagesContainer.ApplyDefaultItemContainerStyle(contentPresenter);
                    contentPresenter.BorderThickness = new(0);
                };
                MessagesContainer.ManagedParent = this;
                MessagesContainer.ItemTemplate = item => new MGChatBoxMessage(ParentWindow, this, item);

                DockPanel.TryAddChild(Footer, Dock.Bottom);
                DockPanel.TryAddChild(Separator, Dock.Bottom);
                DockPanel.TryAddChild(MessagesContainer, Dock.Top);
                DockPanel.CanChangeContent = false;

                this.MaxMessages = MaxMessages;
            }
        }

        private bool IsMessagesCountRefreshPending = false;

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            try
            {
                if (IsMessagesCountRefreshPending)
                    ValidateNumMessages();
            }
            finally { IsMessagesCountRefreshPending = false; }

            base.UpdateSelf(UA);
        }

        private void ValidateNumMessages()
        {
            if (Messages != null)
            {
                while (Messages.Count > MaxMessages)
                    Messages.RemoveAt(0);
            }
        }

        private void SendMessage()
        {
            if (!string.IsNullOrEmpty(InputTextBox.Text))
            {
                SendMessage(InputTextBox.Text);
                InputTextBox.SetText("");
            }
        }

        public void SendMessage(string Message)
        {
            MGScrollViewer ScrollViewer = MessagesContainer.ScrollViewer;
            bool WasScrolledToBottom = ScrollViewer.VerticalOffset.IsAlmostEqual(ScrollViewer.MaxVerticalOffset);
            Messages.Add(new(Environment.UserName, DateTime.Now, Message));
            if (WasScrolledToBottom)
                ScrollViewer.QueueScrollToBottom();
            InputTextBox.RequestFocus();
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
                TimestampTextBlock.ManagedParent = this;
                this.UsernameTextBlock = new(ParentWindow, $"{Username}:");
                UsernameTextBlock.Margin = new(0, 0, Spacing, 0);
                UsernameTextBlock.ManagedParent = this;
                this.MessageTextBlock = new(ParentWindow, Message, AllowsInlineFormatting: false);
                MessageTextBlock.ManagedParent = this;

                DockPanel.TryAddChild(TimestampTextBlock, Dock.Left);
                DockPanel.TryAddChild(UsernameTextBlock, Dock.Left);
                DockPanel.TryAddChild(MessageTextBlock, Dock.Left);
                DockPanel.CanChangeContent = false;
            }
        }
    }
}
