using Microsoft.Xna.Framework.Graphics;
using MGUI.Core.UI.Containers.Grids;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

#if UseWPF
using System.Windows.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
#if UseWPF
    [ContentProperty(nameof(Items))]
#endif
    public class XAMLListBox : XAMLMultiContentHost
    {
        public override MGElementType ElementType => MGElementType.ListBox;

        /// <summary>The generic type that will be used when instantiating <see cref="MGListBox{TItemType}"/>.<para/>
        /// To set this value from a XAML string, you must define the namespace the type belongs to, then use the x:Type Markup Extension<br/>
        /// (See: <see href="https://learn.microsoft.com/en-us/dotnet/desktop/xaml-services/xtype-markup-extension"/>)<para/>
        /// Example:
        /// <code>&lt;ListBox xmlns:System="clr-namespace:System;assembly=mscorlib" ItemType="{x:Type System:Double}" /&gt;</code><para/>
        /// Default value: <code>typeof(object)</code></summary>
        public Type ItemType { get; set; } = typeof(object);

        #region Borders
        public XAMLBorder OuterBorder { get; set; } = new();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLBorderBrush OuterBorderBrush { get => OuterBorder.BorderBrush; set => OuterBorder.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLBorderBrush OuterBB { get => OuterBorderBrush; set => OuterBorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLThickness? OuterBorderThickness { get => OuterBorder.BorderThickness; set => OuterBorder.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLThickness? OuterBT { get => OuterBorderThickness; set => OuterBorderThickness = value; }

        public XAMLBorder InnerBorder { get; set; } = new();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLBorderBrush InnerBorderBrush { get => InnerBorder.BorderBrush; set => InnerBorder.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLBorderBrush InnerBB { get => InnerBorderBrush; set => InnerBorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLThickness? InnerBorderThickness { get => InnerBorder.BorderThickness; set => InnerBorder.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLThickness? InnerBT { get => InnerBorderThickness; set => InnerBorderThickness = value; }

        public XAMLBorder TitleBorder { get; set; } = new();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLBorderBrush TitleBorderBrush { get => TitleBorder.BorderBrush; set => TitleBorder.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLBorderBrush TitleBB { get => TitleBorderBrush; set => TitleBorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLThickness? TitleBorderThickness { get => TitleBorder.BorderThickness; set => TitleBorder.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public XAMLThickness? TitleBT { get => TitleBorderThickness; set => TitleBorderThickness = value; }
        #endregion Borders

        public XAMLContentPresenter TitlePresenter { get; set; } = new();

        public bool? IsTitleVisible { get; set; }

        public XAMLScrollViewer ScrollViewer { get; set; } = new();
        public XAMLStackPanel ItemsPanel { get; set; } = new();

        public XAMLElement Header { get; set; }

        public List<object> Items { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            Type GenericType = typeof(MGListBox<>).MakeGenericType(new Type[] { ItemType });
            object Element = Activator.CreateInstance(GenericType, new object[] { Window });
            return Element as MGElement;
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            Type GenericType = typeof(MGListBox<>).MakeGenericType(new Type[] { ItemType });
            MethodInfo Method = GenericType.GetMethod(nameof(MGListBox<object>.LoadSettings), BindingFlags.Instance | BindingFlags.NonPublic);
            Method.Invoke(Element, new object[] { this });
        }

        protected internal override IEnumerable<XAMLElement> GetChildren()
        {
            foreach (XAMLElement Element in base.GetChildren())
                yield return Element;

            yield return OuterBorder;
            yield return InnerBorder;
            yield return TitleBorder;
            yield return TitlePresenter;
            yield return ScrollViewer;
            yield return ItemsPanel;

            if (Header != null)
                yield return Header;
        }
    }

#if UseWPF
    [ContentProperty(nameof(Columns))]
#endif
    public class XAMLListView : XAMLMultiContentHost
    {
        public override MGElementType ElementType => MGElementType.ListView;

        /// <summary>The generic type that will be used when instantiating <see cref="MGListView{TItemType}"/>.<para/>
        /// To set this value from a XAML string, you must define the namespace the type belongs to, then use the x:Type Markup Extension<br/>
        /// (See: <see href="https://learn.microsoft.com/en-us/dotnet/desktop/xaml-services/xtype-markup-extension"/>)<para/>
        /// Example:
        /// <code>&lt;ListView xmlns:System="clr-namespace:System;assembly=mscorlib" ItemType="{x:Type System:Double}" /&gt;</code><para/>
        /// Default value: <code>typeof(object)</code></summary>
        public Type ItemType { get; set; } = typeof(object);

        public List<XAMLListViewColumn> Columns { get; set; } = new();
        public int? RowHeight { get; set; }

        public XAMLGrid HeaderGrid { get; set; } = new();
        public XAMLScrollViewer ScrollViewer { get; set; } = new();
        public XAMLGrid DataGrid { get; set; } = new();

        public GridSelectionMode? SelectionMode { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            Type GenericType = typeof(MGListView<>).MakeGenericType(new Type[] { ItemType });
            object Element = Activator.CreateInstance(GenericType, new object[] { Window });
            return Element as MGElement;
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            Type GenericType = typeof(MGListView<>).MakeGenericType(new Type[] { ItemType });
            MethodInfo Method = GenericType.GetMethod(nameof(MGListView<object>.LoadSettings), BindingFlags.Instance | BindingFlags.NonPublic);
            Method.Invoke(Element, new object[] { this });
        }

        protected internal override IEnumerable<XAMLElement> GetChildren()
        {
            foreach (XAMLElement Element in base.GetChildren())
                yield return Element;

            yield return HeaderGrid;
            yield return ScrollViewer;
            yield return DataGrid;
        }
    }

#if UseWPF
    [ContentProperty(nameof(Header))]
#endif
    public class XAMLListViewColumn
    {
        public XAMLListViewColumnWidth Width { get; set; }
        public XAMLElement Header { get; set; }
    }

    [TypeConverter(typeof(XAMLListViewColumnWidthStringConverter))]
    public class XAMLListViewColumnWidth
    {
        public int? WidthPixels { get; set; }
        public double? WidthWeight { get; set; }

        public ListViewColumnWidth ToWidth()
        {
            if (WidthPixels.HasValue)
                return new ListViewColumnWidth(WidthPixels.Value);
            else if (WidthWeight.HasValue)
                return new ListViewColumnWidth(WidthWeight.Value);
            else
                throw new InvalidOperationException($"{nameof(XAMLListViewColumnWidth)} must define either an integral value for {nameof(WidthPixels)} or a double value for {nameof(WidthWeight)}");
        }

        public override string ToString() => $"{nameof(XAMLListViewColumnWidth)}: {(WidthPixels.HasValue ? WidthPixels + "px" : WidthWeight + "*")}";

        public static XAMLListViewColumnWidth Parse(string Value)
        {
            if (Value.EndsWith('*'))
            {
                string WeightString = Value.Substring(0, Value.Length - 1);
                double Weight = WeightString == string.Empty ? 1.0 : double.Parse(WeightString);
                return new XAMLListViewColumnWidth() { WidthWeight = Weight };
            }
            else
            {
                string PixelsString = Value.Replace("px", "", StringComparison.CurrentCultureIgnoreCase);
                int Pixels = int.Parse(PixelsString);
                return new XAMLListViewColumnWidth() { WidthPixels = Pixels };
            }
        }
    }

    public class XAMLListViewColumnWidthStringConverter : TypeConverter
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
                XAMLListViewColumnWidth Width = XAMLListViewColumnWidth.Parse(stringValue);
                return Width;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
