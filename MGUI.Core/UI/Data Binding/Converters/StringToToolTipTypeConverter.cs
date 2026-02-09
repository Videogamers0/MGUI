using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.XAML;

#if UseWPF
using System.Windows.Markup;
using System.Windows.Data;
#else
using Portable.Xaml.Markup;
#endif

namespace MGUI.Core.UI.Data_Binding.Converters
{
    /// <summary>The default <see cref="TypeConverter"/> used to convert a string value to a ToolTip. For example:
    /// <code>ToolTip="{MGBinding Path=Description, Mode=OneWay}"</code>
    /// would take the string value from the DataContext's Description property, 
    /// and use this <see cref="TypeConverter"/> to generate an <see cref="MGToolTip"/> from it.<para/>
    /// See also: <see cref="StringToToolTipConverter"/> (which is an <see cref="IValueConverter"/>, not a <see cref="TypeConverter"/>)</summary>
    public class StringToToolTipTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
                return null;
            else if (value is string stringValue)
            {
                //  Instantiating an MGToolTip requires an MGWindow
                //  which must be retrieved from the context parameter
                if (context?.Instance is not MGElement Element)
                {
                    throw new InvalidOperationException($"Cannot convert from string to {nameof(MGToolTip)} " +
                        $"unless {nameof(ITypeDescriptorContext)}.{nameof(ITypeDescriptorContext.Instance)} is of type {nameof(MGElement)}");
                }

                return ToToolTip(Element, stringValue);
            }

            return base.ConvertFrom(context, culture, value);
        }

        internal static MGToolTip ToToolTip(MGElement Host, string Value)
        {
            if (Host == null || Value == null)
                return null;
            else
            {
                MGToolTip ToolTip = new(Host.SelfOrParentWindow, Host, 0, 0);
                ToolTip.Padding = new(6, 3);
                ToolTip.BackgroundBrush.NormalValue = new Color(56, 56, 56, 218).AsFillBrush();
                ToolTip.DefaultTextForeground.NormalValue = new(240, 240, 240);
                MGTextBlock Content = new(Host.SelfOrParentWindow, Value);
                ToolTip.SetContent(Content);

                ToolTip.ApplySizeToContent(SizeToContent.WidthAndHeight, 10, 10, null, null, false);

                DefaultStyle?.Invoke(ToolTip, Content);
                return ToolTip;
            }
        }

        /// <summary>An action that will be applied to the generated <see cref="MGToolTip"/> (and it's generated <see cref="MGTextBlock"/> Content) whenever a new <see cref="MGToolTip"/>
        /// is created from a string value via <see cref="ConvertFrom(ITypeDescriptorContext, CultureInfo, object)"/><para/>
        /// Use this action to change default values of the generated <see cref="MGToolTip"/>, such as:<br/>
        /// <see cref="MGWindow.BorderBrush"/><br/>
        /// <see cref="MGWindow.BorderThickness"/><br/>
        /// <see cref="MGElement.Padding"/><br/>
        /// <see cref="MGElement.BackgroundBrush"/><br/>
        /// <see cref="MGTextBlock.FontSize"/><br/>
        /// <see cref="MGTextBlock.Foreground"/><br/>
        /// <see cref="MGTextBlock.TextAlignment"/><br/>
        /// <see cref="MGTextBlock.IsBold"/><br/>
        /// etc.<para/>
        /// Warning - Do not set <see cref="MGTextBlock.Text"/></summary>
        public static Action<MGToolTip, MGTextBlock> DefaultStyle { get; set; } = (ToolTip, TextBlock) => { };
    }
}
