using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Containers.Grids;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#if UseWPF
using System.Windows.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
    public class ContentPresenter : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.ContentPresenter;
        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGContentPresenter(Window);
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element) => base.ApplyDerivedSettings(Parent, Element);
    }

    public class HeaderedContentPresenter : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.HeaderedContentPresenter;

        public Element Header { get; set; }

        [Category("Layout")]
        public Dock? HeaderPosition { get; set; }
        [Category("Layout")]
        public int? Spacing { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGHeaderedContentPresenter(Window, null, null);
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGHeaderedContentPresenter ContentPresenter = Element as MGHeaderedContentPresenter;

            if (Header != null)
                ContentPresenter.Header = Header.ToElement<MGElement>(ContentPresenter.ParentWindow, ContentPresenter);
            if (HeaderPosition.HasValue)
                ContentPresenter.HeaderPosition = HeaderPosition.Value;
            if (Spacing.HasValue)
                ContentPresenter.Spacing = Spacing.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            if (Header != null)
                yield return Header;
        }
    }

    public class ContextualContentPresenter : Element
    {
        [Category("Behavior")]
        public bool Value { get; set; }

        public Element TrueContent { get; set; } = null;
        public Element FalseContent { get; set; } = null;

        public override MGElementType ElementType => MGElementType.ContextualContentPresenter;
        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGContextualContentPresenter(Window, Value, null, null);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGWindow Window = Element.SelfOrParentWindow;
            MGContextualContentPresenter ContextualContentPresenter = Element as MGContextualContentPresenter;

            if (TrueContent != null)
                ContextualContentPresenter.TrueContent = TrueContent.ToElement<MGElement>(Window, ContextualContentPresenter);
            if (FalseContent != null)
                ContextualContentPresenter.FalseContent = FalseContent.ToElement<MGElement>(Window, ContextualContentPresenter);
            ContextualContentPresenter.Value = Value;
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            if (TrueContent != null)
                yield return TrueContent;
            if (FalseContent != null)
                yield return FalseContent;
        }
    }

    public class Border : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Border;

        [Category("Border")]
        public BorderBrush BorderBrush { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        public Thickness? BorderThickness { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGBorder(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGBorder Border = Element as MGBorder;

            if (BorderBrush != null)
                Border.BorderBrush = BorderBrush.ToBorderBrush(Desktop);
            if (BorderThickness.HasValue)
                Border.BorderThickness = BorderThickness.Value.ToThickness();

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class Button : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Button;

        [Category("Border")]
        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        /// <summary>The name of the delegate to invoke when clicking this <see cref="Button"/>.<br/>
        /// This name should exist in <see cref="MGWindow.NamedActions"/>, otherwise nothing will be invoked when clicking this <see cref="Button"/><para/>
        /// See also: <see cref="MGWindow.AddNamedAction(string, Action{MGElement})"/></summary>
        [Category("Behavior")]
        public string CommandName { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGButton(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGButton Button = Element as MGButton;
            Border.ApplySettings(Button, Button.BorderComponent.Element);

            if (CommandName != null)
            {
                MGWindow Window = Button.ParentWindow;
                if (Window.NamedActions.TryGetValue(CommandName, out var Command))
                {
                    Button.AddCommandHandler((btn, e) => { Command(Button); });
                }
                else
                {
                    //  Listen for when the command with the given name is registered to the window
                    void WindowActionRegistered(object sender, string name)
                    {
                        if (name == CommandName)
                        {
                            Button.AddCommandHandler((btn, e) => { Window.NamedActions[CommandName](Button); });
                            Window.OnActionRegistered -= WindowActionRegistered;
                        }
                    }

                    Window.OnActionRegistered += WindowActionRegistered;
                }
            }

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class ChatBox : Element
    {
        public override MGElementType ElementType => MGElementType.ChatBox;

        [Category("Border")]
        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Behavior")]
        public int? MaxMessageLength { get; set; }
        [Category("Behavior")]
        public int? MaxMessages { get; set; }

        [Category("Appearance")]
        public string TimestampFormat { get; set; }

        public TextBlock CurrentUserTextBlock { get; set; } = new();
        public TextBox InputTextBox { get; set; } = new();
        public Button SendButton { get; set; } = new();
        public Separator Separator { get; set; } = new();
        public ListBox MessagesContainer { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGChatBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGChatBox ChatBox = Element as MGChatBox;
            Border.ApplySettings(ChatBox, ChatBox.BorderComponent.Element);
            CurrentUserTextBlock.ApplySettings(ChatBox, ChatBox.CurrentUserTextBlock);
            InputTextBox.ApplySettings(ChatBox, ChatBox.InputTextBox);
            SendButton.ApplySettings(ChatBox, ChatBox.SendButton);
            Separator.ApplySettings(ChatBox, ChatBox.Separator);

            MessagesContainer.ItemType = ChatBox.MessagesContainer.GetType().GenericTypeArguments[0];
            MessagesContainer.ApplySettings(ChatBox, ChatBox.MessagesContainer);

            if (MaxMessageLength.HasValue)
                ChatBox.MaxMessageLength = MaxMessageLength.Value;
            if (MaxMessages.HasValue)
                ChatBox.MaxMessages = MaxMessages.Value;
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return CurrentUserTextBlock;
            yield return InputTextBox;
            yield return SendButton;
            yield return Separator;
            yield return MessagesContainer;
        }
    }

    public class CheckBox : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.CheckBox;

        public Button Button { get; set; } = new();

        [Category("Layout")]
        public int? CheckBoxComponentSize { get; set; }
        [Category("Layout")]
        public int? SpacingWidth { get; set; }
        [Category("Appearance")]
        public XAMLColor? CheckMarkColor { get; set; }

        [Category("Behavior")]
        public bool? IsThreeState { get; set; }
        [Category("Behavior")]
        public bool? IsChecked { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGCheckBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGCheckBox CheckBox = Element as MGCheckBox;
            Button.ApplySettings(CheckBox, CheckBox.ButtonComponent.Element);

            if (CheckBoxComponentSize.HasValue)
                CheckBox.CheckBoxComponentSize = CheckBoxComponentSize.Value;
            if (SpacingWidth.HasValue)
                CheckBox.SpacingWidth = SpacingWidth.Value;
            if (CheckMarkColor.HasValue)
                CheckBox.CheckMarkColor = CheckMarkColor.Value.ToXNAColor();
            if (IsThreeState.HasValue)
                CheckBox.IsThreeState = IsThreeState.Value;
            if (IsChecked.HasValue)
                CheckBox.IsChecked = IsChecked.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;
            yield return Button;
        }
    }

#if UseWPF
    [ContentProperty(nameof(Items))]
#endif
    public class ComboBox : MultiContentHost
    {
        public override MGElementType ElementType => MGElementType.ComboBox;

        /// <summary>The generic type that will be used when instantiating <see cref="MGComboBox{TItemType}"/>.<para/>
        /// To set this value from a XAML string, you must define the namespace the type belongs to, then use the x:Type Markup Extension<br/>
        /// (See: <see href="https://learn.microsoft.com/en-us/dotnet/desktop/xaml-services/xtype-markup-extension"/>)<para/>
        /// Example:
        /// <code>&lt;ComboBox xmlns:System="clr-namespace:System;assembly=mscorlib" ItemType="{x:Type System:Double}" /&gt;</code><para/>
        /// Default value: <code>typeof(object)</code></summary>
        [Category("Data")]
        public Type ItemType { get; set; } = typeof(object);

        [Category("Border")]
        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        [Category("Appearance")]
        public ContentPresenter DropdownArrow { get; set; } = new();
        [Category("Appearance")]
        public XAMLColor? DropdownArrowColor { get; set; }

        [Category("Data")]
        public List<object> Items { get; set; } = new();

        [Category("Data")]
        public ContentTemplate DropdownItemTemplate { get; set; }
        [Category("Data")]
        public ContentTemplate SelectedItemTemplate { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            Type GenericType = typeof(MGComboBox<>).MakeGenericType(new Type[] { ItemType });
            object Element = Activator.CreateInstance(GenericType, new object[] { Window });
            return Element as MGElement;
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            Type GenericType = typeof(MGComboBox<>).MakeGenericType(new Type[] { ItemType });
            MethodInfo Method = GenericType.GetMethod(nameof(MGComboBox<object>.LoadSettings), BindingFlags.Instance | BindingFlags.NonPublic);
            Method.Invoke(Element, new object[] { this });
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Border;
            yield return DropdownArrow;

            if (DropdownItemTemplate?.Content != null)
                yield return DropdownItemTemplate.Content;
            if (SelectedItemTemplate?.Content != null)
                yield return SelectedItemTemplate.Content;
        }
    }

    public class Expander : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Expander;

        public ToggleButton ExpanderToggleButton { get; set; } = new();

        [Category("Layout")]
        public int? ExpanderButtonSize { get; set; }
        [Category("Border")]
        public BorderBrush ExpanderButtonBorderBrush { get; set; }
        [Category("Border")]
        public Thickness? ExpanderButtonBorderThickness { get; set; }
        [Category("Appearance")]
        public FillBrush ExpanderButtonBackgroundBrush { get; set; }
        [Category("Appearance")]
        public XAMLColor? ExpanderDropdownArrowColor { get; set; }
        [Category("Layout")]
        public int? ExpanderDropdownArrowSize { get; set; }

        [Category("Layout")]
        public int? HeaderSpacingWidth { get; set; }
        public Element Header { get; set; }

        [Category("Behavior")]
        public bool? IsExpanded { get; set; }
        [Category("Behavior")]
        public Visibility? ExpandedVisibility { get; set; }
        [Category("Behavior")]
        public Visibility? CollapsedVisibility { get; set; }

        public StackPanel HeadersPanel { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGExpander(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGExpander Expander = Element as MGExpander;
            ExpanderToggleButton.ApplySettings(Expander, Expander.ExpanderToggleButton);
            HeadersPanel.ApplySettings(Expander, Expander.HeadersPanelComponent.Element);

            if (ExpanderButtonSize.HasValue)
                Expander.ExpanderButtonSize = ExpanderButtonSize.Value;
            if (ExpanderButtonBorderBrush != null)
                Expander.ExpanderButtonBorderBrush = ExpanderButtonBorderBrush.ToBorderBrush(Desktop);
            if (ExpanderButtonBorderThickness.HasValue)
                Expander.ExpanderButtonBorderThickness = ExpanderButtonBorderThickness.Value.ToThickness();
            if (ExpanderButtonBackgroundBrush != null)
                Expander.ExpanderButtonBackgroundBrush.NormalValue = ExpanderButtonBackgroundBrush.ToFillBrush(Desktop);
            if (ExpanderDropdownArrowColor.HasValue)
                Expander.ExpanderDropdownArrowColor = ExpanderDropdownArrowColor.Value.ToXNAColor();
            if (ExpanderDropdownArrowSize.HasValue)
                Expander.ExpanderDropdownArrowSize = ExpanderDropdownArrowSize.Value;

            if (HeaderSpacingWidth.HasValue)
                Expander.HeaderSpacingWidth = HeaderSpacingWidth.Value;
            if (Header != null)
                Expander.Header = Header.ToElement<MGElement>(Element.SelfOrParentWindow, Parent);

            if (IsExpanded.HasValue)
                Expander.IsExpanded = IsExpanded.Value;
            if (ExpandedVisibility.HasValue)
                Expander.ExpandedVisibility = ExpandedVisibility.Value;
            if (CollapsedVisibility.HasValue)
                Expander.CollapsedVisibility = CollapsedVisibility.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return ExpanderToggleButton;

            if (Header != null)
                yield return Header;

            yield return HeadersPanel;
        }
    }

    public class GroupBox : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.GroupBox;

        [Category("Border")]
        public UniformBorderBrush BorderBrush { get; set; }
        [Category("Border")]
        public Thickness? BorderThickness { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public Expander Expander { get; set; } = new();
        public ContentPresenter HeaderPresenter { get; set; } = new();

        [Category("Behavior")]
        public bool? IsExpandable { get; set; }

        public Element Header { get; set; }

        [Category("Layout")]
        public int? HeaderHorizontalMargin { get; set; }
        [Category("Layout")]
        public int? HeaderHorizontalPadding { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGGroupBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGGroupBox GroupBox = Element as MGGroupBox;

            Expander.ApplySettings(GroupBox, GroupBox.Expander);
            HeaderPresenter.ApplySettings(GroupBox, GroupBox.HeaderPresenter);

            if (BorderBrush != null)
                GroupBox.BorderBrush = (MGUniformBorderBrush)BorderBrush.ToBorderBrush(Desktop);
            if (BorderThickness.HasValue)
                GroupBox.BorderThickness = BorderThickness.Value.ToThickness();
            if (IsExpandable.HasValue)
                GroupBox.IsExpandable = IsExpandable.Value;
            if (Header != null)
                GroupBox.Header = Header.ToElement<MGElement>(Element.SelfOrParentWindow, Parent);
            if (HeaderHorizontalMargin.HasValue)
                GroupBox.HeaderHorizontalMargin = HeaderHorizontalMargin.Value;
            if (HeaderHorizontalPadding.HasValue)
                GroupBox.HeaderHorizontalPadding = HeaderHorizontalPadding.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Expander;
            yield return HeaderPresenter;

            if (Header != null)
                yield return Header;
        }
    }

    public class Image : Element
    {
        public override MGElementType ElementType => MGElementType.Image;

        [Category("Data")]
        public string TextureName { get; set; }
        [Category("Data")]
        public string RegionName { get; set; }
        [Category("Data")]
        public Texture2D Texture { get; set; }

        [Category("Appearance")]
        public XAMLColor? TextureColor { get; set; }
        public XAMLRectangle? SourceRect { get; set; }
        [Category("Layout")]
        public Stretch? Stretch { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (!string.IsNullOrEmpty(RegionName))
                return new MGImage(Window, RegionName);
            else
            {
                MGDesktop Desktop = Window.GetDesktop();
                if (!string.IsNullOrEmpty(TextureName))
                    return new MGImage(Window, Desktop.NamedTextures[TextureName], SourceRect?.ToRectangle());
                else
                    return new MGImage(Window, Texture, SourceRect?.ToRectangle(), TextureColor?.ToXNAColor(), Stretch ?? UI.Stretch.Uniform);
            }
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGImage Image = Element as MGImage;

            if (TextureColor.HasValue)
                Image.TextureColor = TextureColor.Value.ToXNAColor();
            if (SourceRect.HasValue)
                Image.SetTexture(Image.Texture, SourceRect.Value.ToRectangle());
            if (Stretch.HasValue)
                Image.Stretch = Stretch.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class InputConsumer : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.InputConsumer;

        [Category("Behavior")]
        public bool? HandlesMousePresses { get; set; }
        [Category("Behavior")]
        public bool? HandlesMouseReleases { get; set; }
        [Category("Behavior")]
        public bool? HandlesMouseDrags { get; set; }
        [Category("Behavior")]
        public bool? HandlesMouseScroll { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGInputConsumer(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGInputConsumer InputConsumer = Element as MGInputConsumer;

            if (HandlesMousePresses.HasValue)
                InputConsumer.HandlesMousePresses = HandlesMousePresses.Value;
            if (HandlesMouseReleases.HasValue)
                InputConsumer.HandlesMouseReleases = HandlesMouseReleases.Value;
            if (HandlesMouseDrags.HasValue)
                InputConsumer.HandlesMouseDrags = HandlesMouseDrags.Value;
            if (HandlesMouseScroll.HasValue)
                InputConsumer.HandlesMouseScroll = HandlesMouseScroll.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class PasswordBox : TextBox
    {
        public override MGElementType ElementType => MGElementType.PasswordBox;

        [Category("Appearance")]
        public char? PasswordCharacter { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGPasswordBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGPasswordBox PasswordBox = Element as MGPasswordBox;

            if (PasswordCharacter.HasValue)
                PasswordBox.PasswordCharacter = PasswordCharacter.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class ProgressBar : Element
    {
        public override MGElementType ElementType => MGElementType.ProgressBar;

        [Category("Border")]
        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        [Category("Value")]
        public TextBlock ValueTextBlock { get; set; } = new();
        [Category("Value")]
        public bool? ShowValue { get; set; }
        [Category("Value")]
        public string ValueDisplayFormat { get; set; }
        [Category("Value")]
        public string NumberFormat { get; set; }

        [Category("Value")]
        public float? Minimum { get; set; }
        [Category("Value")]
        public float? Maximum { get; set; }
        [Category("Value")]
        public float? Value { get; set; }

        [Category("Layout")]
        public int? Size { get; set; }

        [Category("Appearance")]
        public FillBrush CompletedBrush { get; set; }
        [Category("Appearance")]
        public FillBrush IncompleteBrush { get; set; }

        [Category("Layout")]
        public Orientation? Orientation { get; set; }
        [Category("Layout")]
        public bool? IsReversed { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGProgressBar(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGProgressBar ProgressBar = Element as MGProgressBar;
            Border.ApplySettings(Parent, ProgressBar.BorderComponent.Element);
            ValueTextBlock.ApplySettings(Parent, ProgressBar.ValueComponent.Element);

            if (ShowValue.HasValue)
                ProgressBar.ShowValue = ShowValue.Value;
            if (ValueDisplayFormat != null)
                ProgressBar.ValueDisplayFormat = ValueDisplayFormat;
            if (NumberFormat != null)
                ProgressBar.NumberFormat = NumberFormat;

            if (Minimum.HasValue)
                ProgressBar.Minimum = Minimum.Value;
            if (Maximum.HasValue)
                ProgressBar.Maximum = Maximum.Value;
            if (Value.HasValue)
                ProgressBar.Value = Value.Value;

            if (Size.HasValue)
                ProgressBar.Size = Size.Value;

            if (CompletedBrush != null)
                ProgressBar.CompletedBrush.NormalValue = CompletedBrush.ToFillBrush(Desktop);
            if (IncompleteBrush != null)
                ProgressBar.IncompleteBrush.NormalValue = IncompleteBrush.ToFillBrush(Desktop);

            if (Orientation.HasValue)
                ProgressBar.Orientation = Orientation.Value;
            if (IsReversed.HasValue)
                ProgressBar.IsReversed = IsReversed.Value;
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return ValueTextBlock;
        }
    }

    public class RadioButton : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.RadioButton;

        public Button Button { get; set; } = new();

        [Category("Behavior")]
        public string GroupName { get; set; }

        [Category("Layout")]
        public int? BubbleComponentSize { get; set; }
        [Category("Border")]
        public XAMLColor? BubbleComponentBorderColor { get; set; }
        [Category("Border")]
        public float? BubbleComponentBorderThickness { get; set; }
        [Category("Appearance")]
        public XAMLColor? BubbleComponentBackground { get; set; }
        [Category("Appearance")]
        public XAMLColor? BubbleCheckedColor { get; set; }

        [Category("Layout")]
        public int? SpacingWidth { get; set; }

        [Category("Appearance")]
        public XAMLColor? HoveredHighlightColor { get; set; }
        [Category("Appearance")]
        public float? PressedDarkenIntensity { get; set; }

        [Category("Behavior")]
        public bool? IsChecked { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGRadioButton(Window, GroupName ?? "");

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGRadioButton RadioButton = Element as MGRadioButton;
            Button.ApplySettings(Parent, RadioButton.ButtonComponent.Element);

            if (BubbleComponentSize.HasValue)
                RadioButton.BubbleComponentSize = BubbleComponentSize.Value;
            if (BubbleComponentBorderColor.HasValue)
                RadioButton.BubbleComponentBorderColor = BubbleComponentBorderColor.Value.ToXNAColor();
            if (BubbleComponentBorderThickness.HasValue)
                RadioButton.BubbleComponentBorderThickness = BubbleComponentBorderThickness.Value;
            if (BubbleComponentBackground.HasValue)
                RadioButton.BubbleComponentBackground = new VisualStateColorBrush(BubbleComponentBackground.Value.ToXNAColor());
            if (BubbleCheckedColor.HasValue)
                RadioButton.BubbleCheckedColor = BubbleCheckedColor.Value.ToXNAColor();
            if (SpacingWidth.HasValue)
                RadioButton.SpacingWidth = SpacingWidth.Value;
            if (IsChecked.HasValue)
                RadioButton.IsChecked = IsChecked.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Button;
        }
    }

    public class RatingControl : Element
    {
        public override MGElementType ElementType => MGElementType.RatingControl;

        [Category("Appearance")]
        public RatingItemShape? ItemShape { get; set; }
        [Category("Layout")]
        public int? ItemSize { get; set; }
        [Category("Layout")]
        public int? Spacing { get; set; }

        [Category("Value")]
        public float? Minimum { get; set; }
        [Category("Value")]
        public float? Maximum { get; set; }
        [Category("Value")]
        public float? Value { get; set; }

        [Category("Value")]
        public bool? UseDiscreteValues { get; set; }
        [Category("Value")]
        public float? DiscreteValueInterval { get; set; }

        [Category("Appearance")]
        public int? UnfilledShapeStrokeThickness { get; set; }
        [Category("Appearance")]
        public XAMLColor? UnfilledShapeStrokeColor { get; set; }
        [Category("Appearance")]
        public XAMLColor? UnfilledShapeFillColor { get; set; }

        [Category("Appearance")]
        public int? FilledShapeStrokeThickness { get; set; }
        [Category("Appearance")]
        public XAMLColor? FilledShapeStrokeColor { get; set; }
        [Category("Appearance")]
        public XAMLColor? FilledShapeFillColor { get; set; }

        [Category("Appearance")]
        public int? PreviewShapeStrokeThickness { get; set; }
        [Category("Appearance")]
        public XAMLColor? PreviewShapeStrokeColor { get; set; }
        [Category("Appearance")]
        public XAMLColor? PreviewShapeFillColor { get; set; }

        [Category("Behavior")]
        public bool? IsReadonly { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGRatingControl(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGRatingControl RatingControl = Element as MGRatingControl;

            if (ItemShape.HasValue)
                RatingControl.ItemShape = ItemShape.Value;
            if (ItemSize.HasValue)
                RatingControl.ItemSize = ItemSize.Value;
            if (Spacing.HasValue)
                RatingControl.Spacing = Spacing.Value;
            if (Minimum.HasValue || Maximum.HasValue)
                RatingControl.SetRange(Minimum ?? RatingControl.Minimum, Maximum ?? RatingControl.Maximum);
            if (Value.HasValue)
                RatingControl.SetValue(Value.Value);
            if (UseDiscreteValues.HasValue)
                RatingControl.UseDiscreteValues = UseDiscreteValues.Value;
            if (DiscreteValueInterval.HasValue)
                RatingControl.DiscreteValueInterval = DiscreteValueInterval.Value;

            if (UnfilledShapeStrokeThickness.HasValue)
                RatingControl.UnfilledShapeStrokeThickness = UnfilledShapeStrokeThickness.Value;
            if (UnfilledShapeStrokeColor.HasValue)
                RatingControl.UnfilledShapeStrokeColor = UnfilledShapeStrokeColor.Value.ToXNAColor();
            if (UnfilledShapeFillColor.HasValue)
                RatingControl.UnfilledShapeFillColor = UnfilledShapeFillColor.Value.ToXNAColor();

            if (FilledShapeStrokeThickness.HasValue)
                RatingControl.FilledShapeStrokeThickness = FilledShapeStrokeThickness.Value;
            if (FilledShapeStrokeColor.HasValue)
                RatingControl.FilledShapeStrokeColor = FilledShapeStrokeColor.Value.ToXNAColor();
            if (FilledShapeFillColor.HasValue)
                RatingControl.FilledShapeFillColor = FilledShapeFillColor.Value.ToXNAColor();

            if (PreviewShapeStrokeThickness.HasValue)
                RatingControl.PreviewShapeStrokeThickness = PreviewShapeStrokeThickness.Value;
            if (PreviewShapeStrokeColor.HasValue)
                RatingControl.PreviewShapeStrokeColor = PreviewShapeStrokeColor.Value.ToXNAColor();
            if (PreviewShapeFillColor.HasValue)
                RatingControl.PreviewShapeFillColor = PreviewShapeFillColor.Value.ToXNAColor();

            if (IsReadonly.HasValue)
                RatingControl.IsReadonly = IsReadonly.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class Rectangle : Element
    {
        public override MGElementType ElementType => MGElementType.Rectangle;

        [Category("Border")]
        public XAMLColor? Stroke { get; set; }
        [Category("Border")]
        public int? StrokeThickness { get; set; }
        [Category("Appearance")]
        public FillBrush Fill { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGRectangle(Window, Width ?? 16, Height ?? 16, Stroke?.ToXNAColor() ?? Color.Transparent, StrokeThickness ?? 1, Fill?.ToFillBrush(Window.Desktop));

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGRectangle Rectangle = Element as MGRectangle;

            if (Stroke.HasValue)
                Rectangle.Stroke = Stroke.Value.ToXNAColor();
            if (StrokeThickness.HasValue)
                Rectangle.StrokeThickness = StrokeThickness.Value;
            if (Fill != null)
                Rectangle.Fill = Fill.ToFillBrush(Desktop);
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class ResizeGrip : Element
    {
        public override MGElementType ElementType => MGElementType.ResizeGrip;

        [Category("Appearance")]
        public XAMLColor? Foreground { get; set; }
        [Category("Appearance")]
        public int? MaxDots { get; set; }
        [Category("Layout")]
        public int? Spacing { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => throw new InvalidOperationException($"Unsupported feature - cannot instantiate {nameof(MGResizeGrip)} through XAML.");

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGResizeGrip ResizeGrip = Element as MGResizeGrip;

            if (Foreground.HasValue)
                ResizeGrip.Foreground = new(Foreground.Value.ToXNAColor());
            if (MaxDots.HasValue)
                ResizeGrip.MaxDots = MaxDots.Value;
            if (Spacing.HasValue)
                ResizeGrip.Spacing = Spacing.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class ScrollViewer : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.ScrollViewer;

        [Category("Appearance")]
        public ScrollBarVisibility? VerticalScrollBarVisibility { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public ScrollBarVisibility? VSBVisibility { get => VerticalScrollBarVisibility; set => VerticalScrollBarVisibility = value; }

        [Category("Appearance")]
        public ScrollBarVisibility? HorizontalScrollBarVisibility { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public ScrollBarVisibility? HSBVisibility { get => HorizontalScrollBarVisibility; set => HorizontalScrollBarVisibility = value; }

        [Category("Layout")]
        public float? VerticalOffset { get; set; }
        [Category("Layout")]
        public float? HorizontalOffset { get; set; }

        [Category("Appearance")]
        public FillBrush ScrollBarUnfocusedOuterBrush { get; set; }
        [Category("Appearance")]
        public FillBrush ScrollBarFocusedOuterBrush { get; set; }
        [Category("Appearance")]
        public FillBrush ScrollBarUnfocusedInnerBrush { get; set; }
        [Category("Appearance")]
        public FillBrush ScrollBarFocusedInnerBrush { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGScrollViewer(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGScrollViewer ScrollViewer = Element as MGScrollViewer;

            if (VerticalScrollBarVisibility.HasValue)
                ScrollViewer.VerticalScrollBarVisibility = VerticalScrollBarVisibility.Value;
            if (HorizontalScrollBarVisibility.HasValue)
                ScrollViewer.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility.Value;
            if (VerticalOffset.HasValue)
                ScrollViewer.VerticalOffset = VerticalOffset.Value;
            if (HorizontalOffset.HasValue)
                ScrollViewer.HorizontalOffset = HorizontalOffset.Value;

            if (ScrollBarUnfocusedOuterBrush != null)
                ScrollViewer.ScrollBarOuterBrush.NormalValue = ScrollBarUnfocusedOuterBrush.ToFillBrush(Desktop);
            if (ScrollBarFocusedOuterBrush != null)
                ScrollViewer.ScrollBarOuterBrush.SelectedValue = ScrollBarFocusedOuterBrush.ToFillBrush(Desktop);

            if (ScrollBarUnfocusedInnerBrush != null)
                ScrollViewer.ScrollBarInnerBrush.NormalValue = ScrollBarUnfocusedInnerBrush.ToFillBrush(Desktop);
            if (ScrollBarFocusedInnerBrush != null)
                ScrollViewer.ScrollBarInnerBrush.SelectedValue = ScrollBarFocusedInnerBrush.ToFillBrush(Desktop);

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class Separator : Element
    {
        public override MGElementType ElementType => MGElementType.Separator;

        [Category("Layout")]
        public Orientation? Orientation { get; set; }
        [Category("Layout")]
        public int? Size { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGSeparator(Window, Orientation ?? UI.Orientation.Horizontal);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGSeparator Separator = Element as MGSeparator;

            if (Orientation.HasValue)
                Separator.Orientation = Orientation.Value;
            if (Size.HasValue)
                Separator.Size = Size.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class Slider : Element
    {
        public override MGElementType ElementType => MGElementType.Slider;

        [Category("Value")]
        public float? Minimum { get; set; }
        [Category("Value")]
        public float? Maximum { get; set; }
        [Category("Value")]
        public float? Value { get; set; }

        [Category("Value")]
        public bool? UseDiscreteValues { get; set; }
        [Category("Value")]
        public float? DiscreteValueInterval { get; set; }

        [Category("Layout")]
        public int? NumberLineSize { get; set; }
        [Category("Border")]
        public Thickness? NumberLineBorderThickness { get; set; }
        [Category("Border")]
        public BorderBrush NumberLineBorderBrush { get; set; }
        [Category("Appearance")]
        public FillBrush NumberLineFillBrush { get; set; }

        [Category("Appearance")]
        public float? TickFrequency { get; set; }
        [Category("Appearance")]
        public bool? DrawTicks { get; set; }
        [Category("Layout")]
        public int? TickWidth { get; set; }
        [Category("Layout")]
        public int? TickHeight { get; set; }
        [Category("Border")]
        public Thickness? TickBorderThickness { get; set; }
        [Category("Border")]
        public BorderBrush TickBorderBrush { get; set; }
        [Category("Appearance")]
        public FillBrush TickFillBrush { get; set; }

        [Category("Layout")]
        public int? ThumbWidth { get; set; }
        [Category("Layout")]
        public int? ThumbHeight { get; set; }
        [Category("Border")]
        public Thickness? ThumbBorderThickness { get; set; }
        [Category("Border")]
        public BorderBrush ThumbBorderBrush { get; set; }
        [Category("Appearance")]
        public FillBrush ThumbFillBrush { get; set; }

        [Category("Layout")]
        public Orientation? Orientation { get; set; }

        [Category("Appearance")]
        public FillBrush Foreground { get; set; }

        [Category("Behavior")]
        public bool? AcceptsMouseScrollWheel { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGSlider(Window, Minimum ?? 0, Maximum ?? 100, Value ?? Minimum ?? 0);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGSlider Slider = Element as MGSlider;

            if (Minimum.HasValue || MaxHeight.HasValue)
                Slider.SetRange(Minimum ?? Slider.Minimum, Maximum ?? Slider.Maximum);
            if (Value.HasValue)
                Slider.SetValue(Value.Value);

            if (UseDiscreteValues.HasValue)
                Slider.UseDiscreteValues = UseDiscreteValues.Value;
            if (DiscreteValueInterval.HasValue)
                Slider.DiscreteValueInterval = DiscreteValueInterval.Value;

            if (NumberLineSize.HasValue)
                Slider.NumberLineSize = NumberLineSize.Value;
            if (NumberLineBorderThickness.HasValue)
                Slider.NumberLineBorderThickness = NumberLineBorderThickness.Value.ToThickness();
            if (NumberLineBorderBrush != null)
                Slider.NumberLineBorderBrush = NumberLineBorderBrush.ToBorderBrush(Desktop);
            if (NumberLineFillBrush != null)
                Slider.NumberLineFillBrush = NumberLineFillBrush.ToFillBrush(Desktop);

            if (TickFrequency.HasValue)
                Slider.TickFrequency = TickFrequency.Value;
            if (DrawTicks.HasValue)
                Slider.DrawTicks = DrawTicks.Value;
            if (TickWidth.HasValue)
                Slider.TickWidth = TickWidth.Value;
            if (TickHeight.HasValue)
                Slider.TickHeight = TickHeight.Value;
            if (TickBorderThickness.HasValue)
                Slider.TickBorderThickness = TickBorderThickness.Value.ToThickness();
            if (TickBorderBrush != null)
                Slider.TickBorderBrush = TickBorderBrush.ToBorderBrush(Desktop);
            if (TickFillBrush != null)
                Slider.TickFillBrush = TickFillBrush.ToFillBrush(Desktop);

            if (ThumbWidth.HasValue)
                Slider.ThumbWidth = ThumbWidth.Value;
            if (ThumbHeight.HasValue)
                Slider.ThumbHeight = ThumbHeight.Value;
            if (ThumbBorderThickness.HasValue)
                Slider.ThumbBorderThickness = ThumbBorderThickness.Value.ToThickness();
            if (ThumbBorderBrush != null)
                Slider.ThumbBorderBrush = ThumbBorderBrush.ToBorderBrush(Desktop);
            if (ThumbFillBrush != null)
                Slider.ThumbFillBrush = ThumbFillBrush.ToFillBrush(Desktop);

            if (Orientation.HasValue)
                Slider.Orientation = Orientation.Value;

            if (Foreground != null)
                Slider.Foreground = Foreground.ToFillBrush(Desktop);

            if (AcceptsMouseScrollWheel.HasValue)
                Slider.AcceptsMouseScrollWheel = AcceptsMouseScrollWheel.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class Spacer : Element
    {
        public override MGElementType ElementType => MGElementType.Spacer;
        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGSpacer(Window, Width ?? 0, Height ?? 0);
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element) { }
        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class Spoiler : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Spoiler;

        public Button Button { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush UnspoiledBorderBrush { get => Button.BorderBrush; set => Button.BorderBrush = value; }
        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? UnspoiledBorderThickness { get => Button.BorderThickness; set => Button.BorderThickness = value; }
        [Category("Appearance")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public FillBrush UnspoiledBackgroundBrush { get => Button.Background; set => Button.Background = value; }

        [Category("Data")]
        public string UnspoiledText { get; set; }
        [Category("Layout")]
        public HorizontalAlignment? UnspoiledTextAlignment { get; set; }

        [Category("Behavior")]
        public bool? IsRevealed { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGSpoiler(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGSpoiler Spoiler = Element as MGSpoiler;
            Button.ApplySettings(Parent, Spoiler.ButtonComponent.Element);

            if (UnspoiledText != null)
                Spoiler.UnspoiledText = UnspoiledText;
            if (UnspoiledTextAlignment.HasValue)
                Spoiler.UnspoiledTextAlignment = UnspoiledTextAlignment.Value;
            if (IsRevealed.HasValue)
                Spoiler.IsRevealed = IsRevealed.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Button;
        }
    }

    public class Stopwatch : Element
    {
        public override MGElementType ElementType => MGElementType.Stopwatch;

        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        [Category("Value")]
        public TextBlock Value { get; set; } = new();
        [Category("Value")]
        public string ValueDisplayFormat { get; set; }
        [Category("Value")]
        public TimeSpan? Elapsed { get; set; }

        [Category("Behavior")]
        public double? TimeScale { get; set; }
        [Category("Behavior")]
        public bool? IsRunning { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGStopwatch(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGStopwatch StopWatch = Element as MGStopwatch;
            Border.ApplySettings(Parent, StopWatch.BorderComponent.Element);
            Value.ApplySettings(Parent, StopWatch.ValueComponent.Element);

            if (ValueDisplayFormat != null)
                StopWatch.ValueDisplayFormat = ValueDisplayFormat;
            if (Elapsed.HasValue)
                StopWatch.Elapsed = Elapsed.Value;
            if (TimeScale.HasValue)
                StopWatch.TimeScale = TimeScale.Value;
            if (IsRunning.HasValue)
            {
                if (IsRunning.Value)
                    StopWatch.Start();
                else
                    StopWatch.Stop();
            }
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return Value;
        }
    }

#if UseWPF
    [ContentProperty(nameof(Tabs))]
#endif
    public class TabControl : Element
    {
        public override MGElementType ElementType => MGElementType.TabControl;

        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        [Category("Appearance")]
        public StackPanel HeadersPanel { get; set; } = new();
        [Category("Appearance")]
        public FillBrush HeaderAreaBackground { get; set; }

        [Category("Appearance")]
        public Button SelectedTabHeaderTemplate { get; set; }
        [Category("Appearance")]
        public Button UnselectedTabHeaderTemplate { get; set; }

        [Category("Data")]
        public List<TabItem> Tabs { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGTabControl(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGTabControl TabControl = Element as MGTabControl;
            Border.ApplySettings(Parent, TabControl.BorderComponent.Element);
            HeadersPanel.ApplySettings(TabControl, TabControl.HeadersPanelComponent.Element);

            if (HeaderAreaBackground != null)
                TabControl.HeaderAreaBackground.NormalValue = HeaderAreaBackground.ToFillBrush(Desktop);

            if (SelectedTabHeaderTemplate != null)
            {
                TabControl.SelectedTabHeaderTemplate = (TabItem) =>
                {
                    MGButton Button = new(TabItem.SelfOrParentWindow, x => TabItem.IsTabSelected = true);
                    TabControl.ApplyDefaultSelectedTabHeaderStyle(Button);
                    SelectedTabHeaderTemplate.ApplySettings(TabItem, Button);

                    //  When a Tab is selected, the wrapper Button is implcitly set to IsSelected=true.
                    //  If the user specifies a Background but not a SelectedBackground, they probably
                    //  meant to specify a SelectedBackground since the regular Background would do nothing
                    if (SelectedTabHeaderTemplate.Background != null && SelectedTabHeaderTemplate.SelectedBackground == null)
                        Button.BackgroundBrush.SelectedValue = Button.BackgroundBrush.NormalValue;
                    //  Same as above but for TextForeground
                    if (SelectedTabHeaderTemplate.TextForeground != null && SelectedTabHeaderTemplate.SelectedTextForeground == null)
                        Button.DefaultTextForeground.SelectedValue = Button.DefaultTextForeground.NormalValue;

                    return Button;
                };
            }

            if (UnselectedTabHeaderTemplate != null)
            {
                TabControl.UnselectedTabHeaderTemplate = (TabItem) =>
                {
                    MGButton Button = new(TabItem.SelfOrParentWindow, x => TabItem.IsTabSelected = true);
                    TabControl.ApplyDefaultUnselectedTabHeaderStyle(Button);
                    UnselectedTabHeaderTemplate.ApplySettings(TabItem, Button);
                    return Button;
                };
            }

            foreach (TabItem Child in Tabs)
            {
                _ = Child.ToElement<MGTabItem>(TabControl.ParentWindow, TabControl);
            }
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return HeadersPanel;

            foreach (TabItem Tab in Tabs)
                yield return Tab;
        }
    }

    public class TabItem : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.TabItem;

        public Element Header { get; set; }

        [Category("Behavior")]
        public bool? IsTabSelected { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGTabControl TabControl)
            {
                MGElement HeaderElement = Header?.ToElement<MGElement>(Window, null);
                MGElement ContentElement = Content?.ToElement<MGElement>(Window, null);
                return TabControl.AddTab(HeaderElement, ContentElement);
            }
            else
                throw new InvalidOperationException($"The {nameof(Parent)} {nameof(MGElement)} of an {nameof(MGTabItem)} should be of type {nameof(MGTabControl)}");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGTabItem TabItem = Element as MGTabItem;

            if (IsTabSelected.HasValue)
                TabItem.IsTabSelected = IsTabSelected.Value;

            //base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            if (Header != null)
                yield return Header;
        }
    }

#if UseWPF
    [ContentProperty(nameof(Text))]
#endif
    public class TextBlock : Element
    {
        public override MGElementType ElementType => MGElementType.TextBlock;

        [Category("Appearance")]
        public string FontFamily { get; set; }
        [Category("Appearance")]
        public int? FontSize { get; set; }

        [Category("Appearance")]
        public bool? IsBold { get; set; }
        [Category("Appearance")]
        public bool? IsItalic { get; set; }
        [Category("Appearance")]
        public bool? IsUnderlined { get; set; }

        [Category("Appearance")]
        public bool? IsShadowed { get; set; }
        [Category("Appearance")]
        public Size? ShadowOffset { get; set; }
        [Category("Appearance")]
        public XAMLColor? ShadowColor { get; set; }

        [Category("Behavior")]
        public bool? AllowsInlineFormatting { get; set; }

        [Category("Appearance")]
        public XAMLColor? Foreground { get; set; }
        public string Text { get; set; }
        [Category("Layout")]
        public int? MinLines { get; set; }
        [Category("Layout")]
        public int? MaxLines { get; set; }
        [Category("Layout")]
        public bool? WrapText { get; set; }
        [Category("Layout")]
        public float? LinePadding { get; set; }
        [Category("Layout")]
        public HorizontalAlignment? TextAlignment { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGTextBlock(Window, Text, Foreground?.ToXNAColor(), FontSize ?? null, AllowsInlineFormatting ?? true);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGTextBlock TextBlock = Element as MGTextBlock;

            if (FontFamily != null || FontSize.HasValue)
                TextBlock.TrySetFont(FontFamily ?? TextBlock.FontFamily, FontSize ?? TextBlock.FontSize);

            if (IsBold.HasValue)
                TextBlock.IsBold = IsBold.Value;
            if (IsItalic.HasValue)
                TextBlock.IsItalic = IsItalic.Value;
            if (IsUnderlined.HasValue)
                TextBlock.IsUnderlined = IsUnderlined.Value;

            if (IsShadowed.HasValue)
                TextBlock.IsShadowed = IsShadowed.Value;
            if (ShadowOffset.HasValue)
                TextBlock.ShadowOffset = new Point(ShadowOffset.Value.Width, ShadowOffset.Value.Height);
            if (ShadowColor.HasValue)
                TextBlock.ShadowColor = ShadowColor.Value.ToXNAColor();

            if (AllowsInlineFormatting.HasValue)
                TextBlock.AllowsInlineFormatting = AllowsInlineFormatting.Value;

            if (Foreground.HasValue)
                TextBlock.Foreground.NormalValue = Foreground.Value.ToXNAColor();
            if (Text != null)
                TextBlock.Text = Text;
            if (MinLines.HasValue)
                TextBlock.MinLines = MinLines.Value;
            if (MaxLines.HasValue)
                TextBlock.MaxLines = MaxLines.Value;
            if (WrapText.HasValue)
                TextBlock.WrapText = WrapText.Value;
            if (LinePadding.HasValue)
                TextBlock.LinePadding = LinePadding.Value;
            if (TextAlignment.HasValue)
                TextBlock.TextAlignment = TextAlignment.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class TextBox : Element
    {
        public override MGElementType ElementType => MGElementType.TextBox;

        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public TextBlock TextBlock { get; set; } = new();

        public string Text { get; set; }
        [Category("Layout")]
        public bool? WrapText { get; set; }
        [Category("Layout")]
        public int? MinLines { get; set; }
        [Category("Layout")]
        public int? MaxLines { get; set; }

        public TextBlock Placeholder { get; set; } = new();
        public string PlaceholderText { get; set; }

        public TextBlock CharacterCounter { get; set; } = new();
        [Category("Behavior")]
        public int? CharacterLimit { get; set; }
        [Category("Appearance")]
        public bool? ShowCharacterCount { get; set; }
        [Category("Appearance")]
        public string LimitedCharacterCountFormatString { get; set; }
        [Category("Appearance")]
        public string LimitlessCharacterCountFormatString { get; set; }

        [Category("Appearance")]
        public XAMLColor? FocusedSelectionForegroundColor { get; set; }
        [Category("Appearance")]
        public XAMLColor? FocusedSelectionBackgroundColor { get; set; }
        [Category("Appearance")]
        public XAMLColor? UnfocusedSelectionForegroundColor { get; set; }
        [Category("Appearance")]
        public XAMLColor? UnfocusedSelectionBackgroundColor { get; set; }

        [Category("Behavior")]
        public int? UndoRedoHistorySize { get; set; }

        [Category("Behavior")]
        public bool? IsReadonly { get; set; }

        [Category("Behavior")]
        public bool? AcceptsReturn { get; set; }
        [Category("Behavior")]
        public bool? AcceptsTab { get; set; }

        [Category("Behavior")]
        public bool? IsHeldKeyRepeated { get; set; }
        [Category("Behavior")]
        public TimeSpan? InitialKeyRepeatDelay { get; set; }
        [Category("Behavior")]
        public TimeSpan? KeyRepeatInterval { get; set; }
        [Category("Behavior")]
        public TextEntryMode? TextEntryMode { get; set; }

        public ResizeGrip ResizeGrip { get; set; } = new();
        [Category("Layout")]
        public bool? IsUserResizable { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGTextBox(Window, null);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGTextBox TextBox = Element as MGTextBox;
            Border.ApplySettings(Parent, TextBox.BorderComponent.Element);
            TextBlock.ApplySettings(Parent, TextBox.TextBlockComponent.Element);
            Placeholder.ApplySettings(Parent, TextBox.PlaceholderTextBlockComponent.Element);
            CharacterCounter.ApplySettings(Parent, TextBox.CharacterCountComponent.Element);
            ResizeGrip.ApplySettings(Parent, TextBox.ResizeGripComponent.Element);

            if (Text != null)
                TextBox.SetText(Text);
            if (WrapText.HasValue)
                TextBox.WrapText = WrapText.Value;
            if (MinLines.HasValue)
                TextBox.MinLines = MinLines.Value;
            if (MaxLines.HasValue)
                TextBox.MaxLines = MaxLines.Value;

            if (PlaceholderText != null)
                TextBox.PlaceholderText = PlaceholderText;

            if (CharacterLimit.HasValue)
                TextBox.CharacterLimit = CharacterLimit.Value;
            if (ShowCharacterCount.HasValue)
                TextBox.ShowCharacterCount = ShowCharacterCount.Value;
            if (LimitedCharacterCountFormatString != null)
                TextBox.LimitedCharacterCountFormatString = LimitedCharacterCountFormatString;
            if (LimitlessCharacterCountFormatString != null)
                TextBox.LimitlessCharacterCountFormatString = LimitlessCharacterCountFormatString;

            if (FocusedSelectionForegroundColor != null)
                TextBox.FocusedSelectionForegroundColor = FocusedSelectionForegroundColor.Value.ToXNAColor();
            if (FocusedSelectionBackgroundColor != null)
                TextBox.FocusedSelectionBackgroundColor = FocusedSelectionBackgroundColor.Value.ToXNAColor();
            if (UnfocusedSelectionForegroundColor != null)
                TextBox.UnfocusedSelectionForegroundColor = UnfocusedSelectionForegroundColor.Value.ToXNAColor();
            if (UnfocusedSelectionBackgroundColor != null)
                TextBox.UnfocusedSelectionBackgroundColor = UnfocusedSelectionBackgroundColor.Value.ToXNAColor();

            if (UndoRedoHistorySize.HasValue)
                TextBox.UndoRedoHistorySize = UndoRedoHistorySize.Value;

            if (IsReadonly.HasValue)
                TextBox.IsReadonly = IsReadonly.Value;

            if (AcceptsReturn.HasValue)
                TextBox.AcceptsReturn = AcceptsReturn.Value;
            if (AcceptsTab.HasValue)
                TextBox.AcceptsTab = AcceptsTab.Value;

            if (IsHeldKeyRepeated.HasValue)
                TextBox.IsHeldKeyRepeated = IsHeldKeyRepeated.Value;
            if (InitialKeyRepeatDelay.HasValue)
                TextBox.InitialKeyRepeatDelay = InitialKeyRepeatDelay.Value;
            if (KeyRepeatInterval.HasValue)
                TextBox.KeyRepeatInterval = KeyRepeatInterval.Value;
            if (TextEntryMode.HasValue)
                TextBox.TextEntryMode = TextEntryMode.Value;

            if (IsUserResizable.HasValue)
                TextBox.IsUserResizable = IsUserResizable.Value;
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return TextBlock;
            yield return Placeholder;
            yield return CharacterCounter;
            yield return ResizeGrip;
        }
    }

    public class Timer : Element
    {
        public override MGElementType ElementType => MGElementType.Timer;

        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        [Category("Value")]
        public TextBlock Value { get; set; } = new();
        [Category("Value")]
        public string ValueDisplayFormat { get; set; }
        [Category("Value")]
        public TimeSpan? RemainingDuration { get; set; }

        [Category("Behavior")]
        public bool? AllowsNegativeDuration { get; set; }

        [Category("Behavior")]
        public double? TimeScale { get; set; }

        [Category("Behavior")]
        public bool? IsPaused { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGTimer(Window, RemainingDuration ?? TimeSpan.FromSeconds(60.0), IsPaused ?? true, AllowsNegativeDuration ?? false);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGTimer Timer = Element as MGTimer;
            Border.ApplySettings(Parent, Timer.BorderComponent.Element);
            Value.ApplySettings(Parent, Timer.ValueComponent.Element);

            if (ValueDisplayFormat != null)
                Timer.ValueDisplayFormat = ValueDisplayFormat;
            if (RemainingDuration.HasValue)
                Timer.RemainingDuration = RemainingDuration.Value;

            //if (AllowsNegativeDuration.HasValue)
            //    Timer.AllowsNegativeDuration = AllowsNegativeDuration.Value;

            if (TimeScale.HasValue)
                Timer.TimeScale = TimeScale.Value;
            if (IsPaused.HasValue)
            {
                if (IsPaused.Value)
                    Timer.Pause();
                else
                    Timer.Resume();
            }
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return Value;
        }
    }

    public class ToggleButton : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.ToggleButton;

        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        [Category("Appearance")]
        public FillBrush CheckedBackgroundBrush { get; set; }
        [Category("Appearance")]
        public XAMLColor? CheckedTextForeground { get; set; }
        [Category("Behavior")]
        public bool? IsChecked { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGToggleButton(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGToggleButton ToggleButton = Element as MGToggleButton;
            Border.ApplySettings(Parent, ToggleButton.BorderComponent.Element);

            if (CheckedBackgroundBrush != null)
                ToggleButton.CheckedBackgroundBrush = CheckedBackgroundBrush.ToFillBrush(Desktop);
            if (CheckedTextForeground.HasValue)
                ToggleButton.CheckedTextForeground = CheckedTextForeground.Value.ToXNAColor();
            if (IsChecked.HasValue)
                ToggleButton.IsChecked = IsChecked.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Border;
        }
    }

    [TypeConverter(typeof(ToolTipStringConverter))]
    public class ToolTip : Window
    {
        public override MGElementType ElementType => MGElementType.ToolTip;

        [Category("Behavior")]
        public bool? ShowOnDisabled { get; set; }

        [Category("Layout")]
        public Size? DrawOffset { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGToolTip(Window, Parent, Width ?? 0, Height ?? 0);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGToolTip ToolTip = Element as MGToolTip;

            if (ShowOnDisabled.HasValue)
                ToolTip.ShowOnDisabled = ShowOnDisabled.Value;
            if (DrawOffset.HasValue)
                ToolTip.DrawOffset = new Point(DrawOffset.Value.Width, DrawOffset.Value.Height);

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class ToolTipStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                ToolTip ToolTip = new();
                ToolTip.Padding = new(6, 3);
                ToolTip.Content = new TextBlock() { Text = stringValue };
                ToolTip.Background = new SolidFillBrush(new XAMLColor(56, 56, 56, 212));
                ToolTip.TextForeground = new XAMLColor(240, 240, 240);
                return ToolTip;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

    public class Window : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Window;

        [Category("Layout")]
        public int? Left { get; set; }
        [Category("Layout")]
        public int? Top { get; set; }

        [Category("Layout")]
        public SizeToContent? SizeToContent { get; set; }

        [Category("Layout")]
        public float? Scale { get; set; }

        public ResizeGrip ResizeGrip { get; set; } = new();
        [Category("Layout")]
        public bool? IsUserResizable { get; set; }

        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public Window ModalWindow { get; set; }
        public List<Window> NestedWindows { get; set; } = new();

        [Category("Title")]
        public DockPanel TitleBar { get; set; } = new();

        [Category("Title")]
        public TextBlock TitleBarTextBlock { get; set; } = new();
        [Category("Title")]
        public string TitleText { get; set; }
        [Category("Title")]
        public bool? IsTitleBarVisible { get; set; }

        [Category("Title")]
        public Button CloseButton { get; set; } = new();
        [Category("Title")]
        public bool? IsCloseButtonVisible { get; set; }

        [Category("Behavior")]
        public bool? CanCloseWindow { get; set; }

        [Category("Behavior")]
        public bool? AllowsClickThrough { get; set; }

        [Category("Behavior")]
        public bool? IsDraggable { get; set; }

        [Category("Appearance")]
        public WindowStyle? WindowStyle { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            int WindowWidth = Math.Clamp(Width ?? 0, MinWidth ?? 0, MaxWidth ?? int.MaxValue);
            int WindowHeight = Math.Clamp(Height ?? 0, MinHeight ?? 0, MaxHeight ?? int.MaxValue);
            MGWindow Instance = new(Window, Left ?? 0, Top ?? 0, WindowWidth, WindowHeight, Window.Theme);
            foreach (Window Nested in NestedWindows)
                Instance.AddNestedWindow(Nested.ToElement<MGWindow>(Window, Window));
            return Instance;
        }

        public MGWindow ToElement(MGDesktop Desktop, MGTheme Theme)
        {
            int WindowWidth = Math.Clamp(Width ?? 0, MinWidth ?? 0, MaxWidth ?? int.MaxValue);
            int WindowHeight = Math.Clamp(Height ?? 0, MinHeight ?? 0, MaxHeight ?? int.MaxValue);
            MGWindow Window = new(Desktop, Left ?? 0, Top ?? 0, WindowWidth, WindowHeight, Theme);
            ApplySettings(null, Window);
            return Window;
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGWindow Window = Element as MGWindow;
            ResizeGrip.ApplySettings(Window, Window.ResizeGripComponent.Element);
            TitleBar.ApplySettings(Window, Window.TitleBarComponent.Element);
            TitleBarTextBlock.ApplySettings(Window, Window.TitleBarTextBlockElement);
            CloseButton.ApplySettings(Window, Window.CloseButtonElement);

            if (IsUserResizable.HasValue)
                Window.IsUserResizable = IsUserResizable.Value;

            if (ModalWindow != null)
                Window.ModalWindow = ModalWindow.ToElement<MGWindow>(Window, Window);

            if (TitleText != null)
                Window.TitleText = TitleText;

            if (CanCloseWindow.HasValue)
                Window.CanCloseWindow = CanCloseWindow.Value;

            if (AllowsClickThrough.HasValue)
                Window.AllowsClickThrough = AllowsClickThrough.Value;

            if (IsDraggable.HasValue)
                Window.IsDraggable = IsDraggable.Value;

            if (TitleBar.Children.Any())
            {
                MGDockPanel TitleBarDP = Window.TitleBarComponent.Element;
                using (TitleBarDP.AllowChangingContentTemporarily())
                {
                    foreach (MGElement Child in TitleBarDP.Children)
                        Child.Visibility = UI.Visibility.Collapsed;
                    foreach (Element Child in TitleBar.Children)
                        TitleBarDP.TryAddChild(Child.ToElement<MGElement>(Window, TitleBarDP), Child.Dock);
                }
            }

            base.ApplyDerivedSettings(Parent, Element);

            if (WindowStyle != null)
                Window.WindowStyle = WindowStyle.Value;

            //  Apply these properties after setting WindowStyle because they may have been overwrriten when setting WindowStyle to WindowStyle.None
            if (Padding != null)
                Window.Padding = this.Padding.Value.ToThickness();
            if (IsTitleBarVisible.HasValue)
                Window.IsTitleBarVisible = IsTitleBarVisible.Value;
            if (IsCloseButtonVisible.HasValue)
                Window.IsCloseButtonVisible = IsCloseButtonVisible.Value;
            ApplyBackground(Element);
            Border.ApplySettings(Window, Window.BorderComponent.Element);

            if (SizeToContent != null)
                Window.ApplySizeToContent(SizeToContent.Value, 10, 10, null, null, false);
            else if (!Width.HasValue && !Height.HasValue)
                Window.ApplySizeToContent(UI.SizeToContent.WidthAndHeight, 10, 10, null, null, false);
            else if (!Width.HasValue)
                Window.ApplySizeToContent(UI.SizeToContent.Width, 10, 10, null, null, false);
            else if (!Height.HasValue)
                Window.ApplySizeToContent(UI.SizeToContent.Height, 10, 10, null, null, false);

            if (Scale != null)
                Window.Scale = Scale.Value;
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return ResizeGrip;
            yield return Border;

            if (ModalWindow != null)
                yield return ModalWindow;

            foreach (Window Window in NestedWindows)
                yield return Window;

            yield return TitleBar;
            yield return TitleBarTextBlock;
            yield return CloseButton;
        }
    }

    public class XAMLDesigner : Element
    {
        public override MGElementType ElementType => MGElementType.XAMLDesigner;

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGXAMLDesigner(Window);

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGXAMLDesigner Designer = Element as MGXAMLDesigner;

        }
    }
}
