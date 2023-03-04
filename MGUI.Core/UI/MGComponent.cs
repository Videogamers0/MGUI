using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace MGUI.Core.UI
{
    public enum ComponentUpdatePriority
    {
        BeforeContents,
        AfterContents
    }

    public enum ComponentDrawPriority
    {
        BeforeBackground,
        BeforeSelf,
        BeforeContents,
        AfterContents
    }

    public abstract class MGComponentBase
    {
        /// <summary>Consider using <see cref="MGComponent{TElementType}.Element"/> instead.</summary>
        public readonly MGElement BaseElement;

        public readonly ComponentUpdatePriority UpdatePriority;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool UpdateBeforeContents => UpdatePriority == ComponentUpdatePriority.BeforeContents;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool UpdateAfterContents => UpdatePriority == ComponentUpdatePriority.AfterContents;

        public readonly ComponentDrawPriority DrawPriority;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool DrawBeforeBackground => DrawPriority == ComponentDrawPriority.BeforeBackground;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool DrawBeforeSelf => DrawPriority == ComponentDrawPriority.BeforeSelf;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool DrawBeforeContents => DrawPriority == ComponentDrawPriority.BeforeContents;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool DrawAfterContents => DrawPriority == ComponentDrawPriority.AfterContents;

        public bool IsWidthSharedWithContent { get; internal set; }
        public bool IsHeightSharedWithContent { get; internal set; }

        public bool ConsumesLeftSpace { get; internal set; }
        public bool ConsumesTopSpace { get; internal set; }
        public bool ConsumesRightSpace { get; internal set; }
        public bool ConsumesBottomSpace { get; internal set; }

        public bool ConsumesAnySpace => ConsumesLeftSpace || ConsumesTopSpace || ConsumesRightSpace || ConsumesBottomSpace;

        public readonly bool UsesOwnersPadding;

        protected readonly Func<Rectangle, Thickness, Rectangle> ArrangeMethod;

        protected MGComponentBase(MGElement Element, ComponentUpdatePriority UpdatePriority, ComponentDrawPriority DrawPriority,
            bool IsWidthSharedWithContent, bool IsHeightSharedWithContent, bool ConsumesLeftSpace, bool ConsumesTopSpace, bool ConsumesRightSpace, bool ConsumesBottomSpace, bool UsesOwnersPadding,
            Func<Rectangle, Thickness, Rectangle> Arrange)
        {
            this.BaseElement = Element;

            this.UpdatePriority = UpdatePriority;
            this.DrawPriority = DrawPriority;

            this.IsWidthSharedWithContent = IsWidthSharedWithContent;
            this.IsHeightSharedWithContent = IsHeightSharedWithContent;

            this.ConsumesLeftSpace = ConsumesLeftSpace;
            this.ConsumesTopSpace = ConsumesTopSpace;
            this.ConsumesRightSpace = ConsumesRightSpace;
            this.ConsumesBottomSpace = ConsumesBottomSpace;

            this.UsesOwnersPadding = UsesOwnersPadding;

            this.ArrangeMethod = Arrange;
        }

        public static MGComponent<MGBorder> Create(MGBorder Border)
            => new(Border, ComponentUpdatePriority.AfterContents, ComponentDrawPriority.BeforeSelf,
          false, false, true, true, true, true, false, (AvailableBounds, ComponentSize) => AvailableBounds);

        public static MGComponent<MGResizeGrip> Create(MGResizeGrip ResizeGrip)
            => new(ResizeGrip, ComponentUpdatePriority.BeforeContents, ComponentDrawPriority.AfterContents,
                  true, true, false, false, false, false, false,
                  (AvailableBounds, ComponentSize) => MGElement.ApplyAlignment(AvailableBounds, HorizontalAlignment.Right, VerticalAlignment.Bottom, ComponentSize.Size));

        public Rectangle Arrange(Rectangle Bounds, Thickness ElementSize) => ArrangeMethod(Bounds, ElementSize);

        public Thickness Arrange(Thickness Size)
        {
            int Left;
            int Right;
            if (ConsumesLeftSpace && ConsumesRightSpace)
            {
                Left = Size.Left;
                Right = Size.Right;
            }
            else if (ConsumesLeftSpace)
            {
                Left = Size.Width;
                Right = 0;
            }
            else if (ConsumesRightSpace)
            {
                Left = 0;
                Right = Size.Width;
            }
            else
            {
                Left = 0;
                Right = 0;
            }

            int Top;
            int Bottom;
            if (ConsumesTopSpace && ConsumesBottomSpace)
            {
                Top = Size.Top;
                Bottom = Size.Bottom;
            }
            else if (ConsumesTopSpace)
            {
                Top = Size.Height;
                Bottom = 0;
            }
            else if (ConsumesBottomSpace)
            {
                Top = 0;
                Bottom = Size.Height;
            }
            else
            {
                Top = 0;
                Bottom = 0;
            }

            return new(Left, Top, Right, Bottom);
        }
    }

    /// <summary>Represents an <see cref="MGElement"/> that is part of another <see cref="MGElement"/>, but isn't explicitly its content.<para/>
    /// For example, an <see cref="MGCheckBox"/> consists of a checkable rectangular button that is rendered in-line with the checkbox's content.</summary>
    public class MGComponent<TElementType> : MGComponentBase
        where TElementType : MGElement
    {
        /// <summary>The underlying <see cref="MGElement"/> of this <see cref="MGComponent{TElementType}"/>.<para/>
        /// Be careful editing properties of this element. Some changes could have unintended consequences to the parent element's layout. Modify at your own risk.<para/>
        /// For example, if you set <see cref="MGWindow.TitleBarComponent"/>'s <see cref="MGElement.HorizontalAlignment"/> to <see cref="HorizontalAlignment.Center"/>,
        /// the title wouldn't span the entire window width anymore, so the title bar's background color wouldn't stretch either.
        /// On the other hand, if you set <see cref="MGWindow.TitleBarTextBlockElement"/>'s <see cref="MGTextBlock.TextAlignment"/> to <see cref="HorizontalAlignment.Center"/>, 
        /// it would still look as intended.</summary>
        public readonly TElementType Element;

        /// <param name="UsesOwnersPadding">If true, then the available bounds will be reduced by the owner's padding before measuring this component.</param>
        /// <param name="Arrange">Parameters: A rectangle of the available bounds, and the size of the component. Output: The actual bounds the component should occupy.</param>
        public MGComponent(TElementType Element, bool IsWidthSharedWithContent, bool IsHeightSharedWithContent, 
            bool ConsumesLeftSpace, bool ConsumesTopSpace, bool ConsumesRightSpace, bool ConsumesBottomSpace, bool UsesOwnersPadding,
            Func<Rectangle, Thickness, Rectangle> Arrange)
            : this(Element, ComponentUpdatePriority.AfterContents, ComponentDrawPriority.BeforeContents,
                  IsWidthSharedWithContent, IsHeightSharedWithContent, ConsumesLeftSpace, ConsumesTopSpace, ConsumesRightSpace, ConsumesBottomSpace, UsesOwnersPadding, Arrange)
        {
        
        }

        /// <param name="UpdatePriority">Recommended value: <see cref="ComponentUpdatePriority.AfterContents"/></param>
        /// <param name="DrawPriority">Recommended value: <see cref="ComponentDrawPriority.BeforeContents"/></param>
        /// <param name="UsesOwnersPadding">If true, then the available bounds will be reduced by the owner's padding before measuring this component.</param>
        /// <param name="Arrange">Parameters: A rectangle of the available bounds, and the size of the component. Output: The actual bounds the component should occupy.</param>
        public MGComponent(TElementType Element, ComponentUpdatePriority UpdatePriority, ComponentDrawPriority DrawPriority, 
            bool IsWidthSharedWithContent, bool IsHeightSharedWithContent, bool ConsumesLeftSpace, bool ConsumesTopSpace, bool ConsumesRightSpace, bool ConsumesBottomSpace, bool UsesOwnersPadding,
            Func<Rectangle, Thickness, Rectangle> Arrange)
            : base(Element, UpdatePriority, DrawPriority, IsWidthSharedWithContent, IsHeightSharedWithContent, ConsumesLeftSpace, ConsumesTopSpace, ConsumesRightSpace, ConsumesBottomSpace, UsesOwnersPadding, Arrange)
        {
            this.Element = Element;

        }

        public override string ToString() => $"{nameof(MGComponent<TElementType>)}: {UpdatePriority} / {DrawPriority}: {Element}";
    }
}
