using MGUI.Core.UI.Data_Binding;
using MGUI.Core.UI.XAML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#if UseWPF
using System.Windows.Markup;
using System.Windows.Data;
#else
using Portable.Xaml.Markup;
using MGUI.Core.UI.Data_Binding.Converters;
#endif

//  This markup extension is intentionally placed in MGUI.Core.UI.XAML namespace so that it can be
//  referenced in XAML without requiring any additional XML namespace prefixes.
namespace MGUI.Core.UI.XAML
{
    public class MGBinding : MarkupExtension
    {
        /// <summary>The path to the source property. Separate nested object properties with a '.'.<para/>
        /// EX: "Location.City" retrieve the value of the "Location" property. Then look for the value of the "City" property on that inner object.</summary>
        public string Path { get; set; } = "";

        public DataBindingMode Mode { get; set; } = DataBindingMode.OneWay;

        /// <summary>If specified, the binding will use <see cref="SourceObjectResolverElementName"/> to find the source object.<para/>
        /// See also: <see cref="SourceObjectResolver"/></summary>
        public string ElementName { get; set; }

        /// <summary>If specified, the binding will use <see cref="SourceObjectResolverStaticResource"/> to find the source object.<para/>
        /// See also: <see cref="SourceObjectResolver"/></summary>
        public string ResourceName { get; set; }

        /// <summary>If not specified, defaults to <see cref="DataContextResolver.Self"/> when binding using <see cref="ElementName"/> or <see cref="ResourceName"/>.<br/>
        /// Uses <see cref="DataContextResolver.DataContext"/> in all other cases.</summary>
        public DataContextResolver? DataContextResolver { get; set; }

        /// <summary>Optional. Converts values of the source or target property before setting them to the other property.<para/>
        /// If <see cref="Mode"/> is <see cref="DataBindingMode.OneTime"/>, <see cref="DataBindingMode.OneWay"/>, or <see cref="DataBindingMode.TwoWay"/>,
        /// this converter must implement <see cref="IValueConverter.Convert(object, Type, object, System.Globalization.CultureInfo)"/>.<para/>
        /// If <see cref="Mode"/> is <see cref="DataBindingMode.OneWayToSource"/> or <see cref="DataBindingMode.TwoWay"/>,
        /// this converter must implement <see cref="IValueConverter.ConvertBack(object, Type, object, System.Globalization.CultureInfo)"/></summary>
        public IValueConverter Converter { get; set; } = null;
        /// <summary>Optional. A parameter to pass in when converting values via the given <see cref="Converter"/></summary>
        public object ConverterParameter { get; set; }

        /// <summary>Optional. A default value to set the target property to if the source property of the binding could not be resolved.</summary>
        public object FallbackValue { get; set; }

        /// <summary>Optional. Used to format values that are being converted to a string property value.</summary>
        public string StringFormat { get; set; }

        /// <summary>This value is automatically determined by the name of the property that the binding is attached to.<para/>
        /// However, some properties on <see cref="XAML"/> objects (such as <see cref="Button"/>) don't have the same name as their corresponding property on the
        /// <see cref="UI"/> objects (such as <see cref="MGButton"/>), so you can override the <see cref="BindingConfig.TargetPath"/> if needed.</summary>
        public string TargetPathOverride { get; set; }

        /// <summary>Uses <see cref="SourceObjectResolverSelf"/> if neither <see cref="ElementName"/> nor <see cref="ResourceName"/> are specified.</summary>
        public ISourceObjectResolver SourceObjectResolver
        {
            get
            {
                if (!string.IsNullOrEmpty(ElementName) && !string.IsNullOrEmpty(ResourceName))
                    throw new InvalidOperationException($"Invalid {nameof(MGBinding)}: You cannot specify values for both '{nameof(ElementName)}' and '{nameof(ResourceName)}'.");
                else if (!string.IsNullOrEmpty(ElementName))
                    return ISourceObjectResolver.FromElementName(ElementName);
                else if (!string.IsNullOrEmpty(ResourceName))
                    return ISourceObjectResolver.FromResourceName(ResourceName);
                else
                    return ISourceObjectResolver.FromSelf();
            }
        }

        public MGBinding() { }
        public MGBinding(string Path) { this.Path = Path; }

        //  If binding using ElementName, user probably wants to bind directly to that object instead of to it's DataContext.
        private DataContextResolver ActualDataContextResolver =>
            DataContextResolver ?? (!string.IsNullOrEmpty(ElementName) || !string.IsNullOrEmpty(ResourceName) ? Data_Binding.DataContextResolver.Self : Data_Binding.DataContextResolver.DataContext);

        private BindingConfig ToBinding(string TargetPropertyName)
            => new(TargetPropertyName, Path, Mode, SourceObjectResolver, ActualDataContextResolver, Converter, ConverterParameter, FallbackValue, StringFormat);

        public override object ProvideValue(IServiceProvider Provider)
        {
            IProvideValueTarget ProvideValueTarget = (IProvideValueTarget)Provider.GetService(typeof(IProvideValueTarget));
            if (ProvideValueTarget.TargetProperty is PropertyInfo TargetProperty && ProvideValueTarget.TargetObject is Element TargetObject)
            {
                //  Save the binding info onto the target object so it can be evaluated later on (once we've instantiated the MGElement instance from the Element object)
                TargetObject.Bindings.Add(ToBinding(TargetPathOverride ?? TargetProperty.Name));

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
}
