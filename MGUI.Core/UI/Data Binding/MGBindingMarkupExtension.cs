﻿using MGUI.Core.UI.Data_Binding;
using MGUI.Core.UI.XAML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

//  This markup extension is intentionally placed in MGUI.Core.UI.XAML namespace so that it can be
//  referenced in XAML without requiring any additional XML namespace prefixes.
namespace MGUI.Core.UI.XAML
{
#if UseWPF
    public class MGBindingExtension : MarkupExtension
    {
        /// <summary>The path to the source property. Separate nested object properties with a '.'.<para/>
        /// EX: "Location.City" retrieve the value of the "Location" property. Then look for the value of the "City" property on that inner object.</summary>
        public string Path { get; set; } = "";
        /// <summary>If specified, the binding will use <see cref="SourceObjectResolverElementName"/> to find the source object.<para/>
        /// If not specified, the binding will use <see cref="SourceObjectResolverSelf"/>.</summary>
        public string ElementName { get; set; } = null;
        public DataBindingMode Mode { get; set; } = DataBindingMode.OneWay;
        /// <summary>If not specified, defaults to <see cref="DataContextResolver.Self"/> when binding using <see cref="ElementName"/>,<br/>
        /// uses <see cref="DataContextResolver.DataContext"/> in all other cases.</summary>
        public DataContextResolver? DataContextResolver { get; set; } = null;

        /// <summary>Optional. Converts values of the source or target property before setting them to the other property.<para/>
        /// If <see cref="Mode"/> is <see cref="DataBindingMode.OneTime"/>, <see cref="DataBindingMode.OneWay"/>, or <see cref="DataBindingMode.TwoWay"/>,
        /// this converter must implement <see cref="IValueConverter.Convert(object, Type, object, System.Globalization.CultureInfo)"/>.<para/>
        /// If <see cref="Mode"/> is <see cref="DataBindingMode.OneWayToSource"/> or <see cref="DataBindingMode.TwoWay"/>,
        /// this converter must implement <see cref="IValueConverter.ConvertBack(object, Type, object, System.Globalization.CultureInfo)"/></summary>
        public IValueConverter Converter { get; set; } = null;
        /// <summary>Optional. A parameter to pass in when converting values via the given <see cref="Converter"/></summary>
        public object ConverterParameter { get; set; } = null;

        /// <summary>Optional. A default value to set the target property to if the source property of the binding could not be resolved.</summary>
        public object FallbackValue { get; set; } = null;

        /// <summary>This value is automatically determined by the name of the property that the binding is attached to.<para/>
        /// However, some properties on <see cref="XAML"/> objects (such as <see cref="Button"/>) don't have the same name as their corresponding property on the
        /// <see cref="UI"/> objects (such as <see cref="MGButton"/>), so you can override the TargetPropertyName if needed.</summary>
        public string TargetPropertyNameOverride { get; set; } = null;

        public ISourceObjectResolver SourceObjectResolver =>
            string.IsNullOrEmpty(ElementName) ? ISourceObjectResolver.FromSelf() : ISourceObjectResolver.FromElementName(ElementName);

        public MGBindingExtension() { }

        //  If binding using ElementName, user probably wants to bind directly to that object instead of to it's DataContext.
        private DataContextResolver ActualDataContextResolver =>
            DataContextResolver ?? (!string.IsNullOrEmpty(ElementName) ? Data_Binding.DataContextResolver.Self : Data_Binding.DataContextResolver.DataContext);

        private MGBinding ToBinding(string TargetPropertyName)
            => new(TargetPropertyName, Path, Mode, SourceObjectResolver, ActualDataContextResolver, Converter, ConverterParameter, FallbackValue);

        public override object ProvideValue(IServiceProvider Provider)
        {
            IProvideValueTarget ProvideValueTarget = (IProvideValueTarget)Provider.GetService(typeof(IProvideValueTarget));
            if (ProvideValueTarget.TargetProperty is PropertyInfo TargetProperty && ProvideValueTarget.TargetObject is Element TargetObject)
            {
                //  Save the binding info onto the target object so it can be evaluated later on (once we've instantiated the MGElement instance from the Element object)
                TargetObject.Bindings.Add(ToBinding(TargetPropertyNameOverride ?? TargetProperty.Name));

                //  Return the fallbackvalue or default for now. The actual value will be evaluated later
                if (FallbackValue != null && FallbackValue.GetType().IsAssignableTo(TargetProperty.PropertyType))
                    return FallbackValue;
                else
                    return GetDefaultValue(TargetProperty.PropertyType);
            }

            throw new NotImplementedException("Cannot provide a value when the underlying Type is unknown.");
        }

        private static object GetDefaultValue(Type Type) => Type.IsValueType ? Activator.CreateInstance(Type) : null;
    }
#endif
}