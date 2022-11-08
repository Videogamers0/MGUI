using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.XAML
{
    [TypeConverter(typeof(XAMLElementStringConverter))]
    public abstract class XAMLElement
    {
        public string Name { get; set; }

        public XAMLThickness? Margin { get; set; }
        public XAMLThickness? Padding { get; set; }

        public HorizontalAlignment? HorizontalAlignment { get; set; }
        public VerticalAlignment? VerticalAlignment { get; set; }
        public HorizontalAlignment? HorizontalContentAlignment { get; set; }
        public VerticalAlignment? VerticalContentAlignment { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public HorizontalAlignment? HA { get => HorizontalAlignment; set => HorizontalAlignment = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public VerticalAlignment? VA { get => VerticalAlignment; set => VerticalAlignment = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public HorizontalAlignment? HCA { get => HorizontalContentAlignment; set => HorizontalContentAlignment = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public VerticalAlignment? VCA { get => VerticalContentAlignment; set => VerticalContentAlignment = value; }

        public int? MinWidth { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        public int? PreferredWidth { get; set; }
        public int? PreferredHeight { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int? Width { get => PreferredWidth; set => PreferredWidth = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int? Height { get => PreferredHeight; set => PreferredHeight = value; }

        public XAMLToolTip ToolTip { get; set; }
        public XAMLContextMenu ContextMenu { get; set; }

        public bool? CanHandleInputsWhileHidden { get; set; }
        public bool? IsHitTestVisible { get; set; }

        public bool? IsSelected { get; set; }
        public bool? IsEnabled { get; set; }

        //public VisualStateBrush Background
        public XAMLFillBrush Background { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLFillBrush BG { get => Background; set => Background = value; }
        public XAMLColor? TextForeground { get; set; }

        public Visibility? Visibility { get; set; }

        public bool? ClipToBounds { get; set; }

        public float? Opacity { get; set; }

        /// <summary>Used by <see cref="XAMLDockPanel"/>'s children</summary>
        public Dock Dock { get; set; } = Dock.Top;
        /// <summary>Used by <see cref="XAMLGrid"/>'s children</summary>
        public int GridRow { get; set; } = 0;
        /// <summary>Used by <see cref="XAMLGrid"/>'s children</summary>
        public int GridColumn { get; set; } = 0;
        /// <summary>Used by <see cref="XAMLGrid"/>'s children</summary>
        public int GridRowSpan { get; set; } = 1;
        /// <summary>Used by <see cref="XAMLGrid"/>'s children</summary>
        public int GridColumnSpan { get; set; } = 1;
        /// <summary>Used by <see cref="XAMLGrid"/>'s children</summary>
        public bool GridAffectsMeasure { get; set; } = true;
        /// <summary>Used by <see cref="XAMLOverlayPanel"/>'s children</summary>
        public XAMLThickness Offset { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int Row { get => GridRow; set => GridRow = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int Column { get => GridColumn; set => GridColumn = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int RowSpan { get => GridRowSpan; set => GridRowSpan = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int ColumnSpan { get => GridColumnSpan; set => GridColumnSpan = value; }

        public Dictionary<string, object> AttachedProperties { get; set; } = new();

        public T ToElement<T>(MGWindow Window, MGElement Parent) 
            where T : MGElement
        {
            MGElement Element = CreateElementInstance(Window, Parent);
            ApplySettings(Parent, Element);
            return Element as T;
        }

        protected internal void ApplySettings(MGElement Parent, MGElement Element)
        {
            using (Element.BeginInitializing())
            {
                ApplyBaseSettings(Parent, Element);
                ApplyDerivedSettings(Parent, Element);
            }
        }

        private void ApplyBaseSettings(MGElement Parent, MGElement Element)
        {
            using (Element.BeginInitializing())
            {
                if (Name != null)
                    Element.Name = Name;

                if (Margin.HasValue)
                    Element.Margin = Margin.Value.ToThickness();
                if (Padding.HasValue)
                    Element.Padding = Padding.Value.ToThickness();

                if (HorizontalAlignment.HasValue)
                    Element.HorizontalAlignment = HorizontalAlignment.Value;
                if (VerticalAlignment.HasValue)
                    Element.VerticalAlignment = VerticalAlignment.Value;
                if (HorizontalContentAlignment.HasValue)
                    Element.HorizontalContentAlignment = HorizontalContentAlignment.Value;
                if (VerticalContentAlignment.HasValue)
                    Element.VerticalContentAlignment = VerticalContentAlignment.Value;

                if (MinWidth.HasValue)
                    Element.MinWidth = MinWidth.Value;
                if (MinHeight.HasValue)
                    Element.MinHeight = MinHeight.Value;
                if (MaxWidth.HasValue)
                    Element.MaxWidth = MaxWidth.Value;
                if (MaxHeight.HasValue)
                    Element.MaxHeight = MaxHeight.Value;

                if (PreferredWidth.HasValue)
                    Element.PreferredWidth = PreferredWidth.Value;
                if (PreferredHeight.HasValue)
                    Element.PreferredHeight = PreferredHeight.Value;

                if (ToolTip != null)
                    Element.ToolTip = ToolTip.ToElement<MGToolTip>(Element.SelfOrParentWindow, Element);
                if (ContextMenu != null)
                    Element.ContextMenu = ContextMenu.ToElement<MGContextMenu>(Element.SelfOrParentWindow, Element);

                if (CanHandleInputsWhileHidden.HasValue)
                    Element.CanHandleInputsWhileHidden = CanHandleInputsWhileHidden.Value;
                if (IsHitTestVisible.HasValue)
                    Element.IsHitTestVisible = IsHitTestVisible.Value;
                if (IsSelected.HasValue)
                    Element.IsSelected = IsSelected.Value;
                if (IsEnabled.HasValue)
                    Element.IsEnabled = IsEnabled.Value;

                if (Background != null)
                    Element.BackgroundBrush.NormalValue = Background.ToFillBrush();
                if (TextForeground.HasValue)
                    Element.DefaultTextForeground.NormalValue = TextForeground.Value.ToXNAColor();

                if (Visibility.HasValue)
                    Element.Visibility = Visibility.Value;

                if (ClipToBounds.HasValue)
                    Element.ClipToBounds = ClipToBounds.Value;

                if (Opacity.HasValue)
                    Element.Opacity = Opacity.Value;
            }
        }

        protected abstract MGElement CreateElementInstance(MGWindow Window, MGElement Parent);
        protected internal abstract void ApplyDerivedSettings(MGElement Parent, MGElement Element);
    }

    public class XAMLElementStringConverter : TypeConverter
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
                XAMLTextBlock TextBlock = new() { Text = stringValue };
                return TextBlock;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
