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
using System.Windows.Markup;

namespace MGUI.Core.UI.XAML
{
    [ContentProperty(nameof(Columns))]
    public class XAMLListView : XAMLMultiContentHost
    {
        /// <summary>The generic type that will be used when instantiating <see cref="MGListView{TItemType}"/>.<para/>
        /// To set this value from a XAML string, you must define the namespace the type belongs to, then use the x:Type Markup Extension<br/>
        /// (See: <see href="https://learn.microsoft.com/en-us/dotnet/desktop/xaml-services/xtype-markup-extension"/>)<para/>
        /// Example:
        /// <code>&lt;ListView xmlns:System="clr-namespace:System;assembly=mscorlib" ItemType="{x:Type System:Double}" /&gt;</code><para/>
        /// Default value: typeof(string)</summary>
        public Type ItemType { get; set; } = typeof(string);

        public List<XAMLListViewColumn> Columns { get; set; } = new();
        public int? RowHeight { get; set; }

        public XAMLGrid HeaderGrid { get; set; } = new();
        public XAMLScrollViewer ScrollViewer { get; set; } = new();
        public XAMLGrid DataGrid { get; set; } = new();

        public GridSelectionMode? SelectionMode { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
        {
            Type GenericType = typeof(MGListView<>).MakeGenericType(new Type[] { ItemType });
            object Element = Activator.CreateInstance(GenericType, new object[] { Window });
            return Element as MGElement;
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            Type GenericType = typeof(MGListView<>).MakeGenericType(new Type[] { ItemType });
            MethodInfo Method = GenericType.GetMethod(nameof(MGListView<object>.LoadSettings), BindingFlags.Instance | BindingFlags.NonPublic);
            Method.Invoke(Element, new object[] { this, NamedTextures });
        }
    }

    [ContentProperty(nameof(HeaderContent))]
    public class XAMLListViewColumn
    {
        public XAMLListViewColumnWidth Width { get; set; }
        public XAMLElement HeaderContent { get; set; }
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
