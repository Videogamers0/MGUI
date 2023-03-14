using MGUI.Core.UI.Containers;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    /// <summary>A wrapper class that provides mutually-exclusive visibility of 2 child elements.<para/>
    /// If <see cref="Value"/> is true, this element will display the <see cref="TrueContent"/>, else displays the <see cref="FalseContent"/></summary>
    public class MGContextualContentPresenter : MGSingleContentHost
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _Value;
        /// <summary>If true, the <see cref="TrueContent"/> will be displayed.<br/>
        /// If false, the <see cref="FalseContent"/> will be displayed.</summary>
        public bool Value
        {
            get => _Value;
            set
            {
                if (_Value != value)
                {
                    _Value = value;
                    NPC(nameof(Value));
                    OnValueChanged?.Invoke(this, Value);
                    UpdateDisplayedContent();
                }
            }
        }

        /// <summary>Invoked after <see cref="Value"/> changes.<para/>
        /// This event is invoked before <see cref="OnContentUpdated"/></summary>
        public event EventHandler<bool> OnValueChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _TrueContent;
        /// <summary>The content to display when <see cref="Value"/> is true.</summary>
        public MGElement TrueContent
        {
            get => _TrueContent;
            set
            {
                if (_TrueContent != value)
                {
                    TrueContent?.SetParent(null);
                    InvokeContentRemoved(TrueContent);
                    _TrueContent = value;
                    TrueContent?.SetParent(this);
                    InvokeContentAdded(TrueContent);
                    if (Value)
                        UpdateDisplayedContent();
                    NPC(nameof(TrueContent));
                    NPC(nameof(CurrentContent));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _FalseContent;
        /// <summary>The content to display when <see cref="Value"/> is false.</summary>
        public MGElement FalseContent
        {
            get => _FalseContent;
            set
            {
                if (_FalseContent != value)
                {
                    FalseContent?.SetParent(null);
                    InvokeContentRemoved(FalseContent);
                    _FalseContent = value;
                    FalseContent?.SetParent(this);
                    InvokeContentAdded(FalseContent);
                    if (!Value)
                        UpdateDisplayedContent();
                    NPC(nameof(FalseContent));
                    NPC(nameof(CurrentContent));
                }
            }
        }

        public MGElement CurrentContent => Value ? TrueContent : FalseContent;

        private void UpdateDisplayedContent()
        {
            MGElement DesiredContent = CurrentContent;
            if (Content != DesiredContent)
            {
                MGElement PreviousContent = Content;
                _Content = DesiredContent;

                if (PreviousContent != null)
                    PreviousContent.IsHitTestVisible = false;
                if (DesiredContent != null)
                    DesiredContent.IsHitTestVisible = true;

                LayoutChanged(this, true);
                OnContentUpdated?.Invoke(this, new(PreviousContent, CurrentContent));
            }
        }

        /// <summary>Invoked after the <see cref="CurrentContent"/> has been set to a new value.</summary>
        public event EventHandler<EventArgs<MGElement>> OnContentUpdated;

        protected override void SetContentVirtual(MGElement Value) => 
            throw new InvalidOperationException($"{nameof(MGContextualContentPresenter)}.{nameof(Content)} is automatically managed via " +
                $"{nameof(TrueContent)}, {nameof(FalseContent)}, and {nameof(Value)} properties and should not be explicitly set.");

        public MGContextualContentPresenter(MGWindow Window, bool Value, MGElement TrueContent = null, MGElement FalseContent = null)
            : base(Window, MGElementType.ContextualContentPresenter)
        {
            using (BeginInitializing())
            {
                this.CanChangeContent = false;

                this.Value = Value;
                this.TrueContent = TrueContent;
                this.FalseContent = FalseContent;
            }
        }

        public override IEnumerable<MGElement> GetVisualTreeChildren(bool IncludeInactive, bool IncludeActive)
        {
            if (IncludeInactive)
            {
                if (TrueContent != null && TrueContent != CurrentContent)
                    yield return TrueContent;
                if (FalseContent != null && FalseContent != CurrentContent)
                    yield return FalseContent;
            }

            if (IncludeActive)
            {
                MGElement Current = this.CurrentContent;
                if (Current != null)
                    yield return Current;
            }
        }
    }
}
