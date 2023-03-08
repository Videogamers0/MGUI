using MGUI.Shared.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MGUI.Core.UI.XAML
{
#if UseWPF
    [ContentProperty(nameof(Content))]
#endif
    public class ContentTemplate
    {
        [Category("Data")]
        public Element Content { get; set; }

        /// <param name="ApplyBaseSettings">If not null, this action will be invoked before <see cref="Element.ApplySettings(MGElement, MGElement, bool)"/>
        /// executes on the created <see cref="MGElement"/>.</param>
        public MGElement GetContent(MGWindow Window, MGElement Parent, object DataContext, Action<MGElement> ApplyBaseSettings = null)
        {
            if (Content == null)
                return null;

#if NEVER // Old code for the 'PropertyBinding markup extension
            Element ContentCopy = Content.Copy();
            PreProcessBindings(ContentCopy, DataContext);
            MGElement Item = ContentCopy.ToElement(Window, Parent, ApplyBaseSettings);
            if (DataContext is INotifyPropertyChanged NPC)
                PostProcessBindings(Item, NPC);
#elif true
            Element ContentCopy = Content.Copy();
            PreProcessBindings(ContentCopy, DataContext);
            MGElement Item = ContentCopy.ToElement(Window, Parent, ApplyBaseSettings);
            if (DataContext is INotifyPropertyChanged NPC)
                PostProcessBindings(Item, NPC);
            Element.ProcessBindings(Item, true, DataContext);
#else
            Element ContentCopy = Content.Copy();
            MGElement Item = ContentCopy.ToElement(Window, Parent, ApplyBaseSettings);
            Element.ProcessBindings(Item, true, DataContext);
#endif

            return Item;
        }

        /// <summary>Finds and replaces bound property paths with the underlying value from the <paramref name="DataContext"/>.<para/>
        /// EX: If a <see cref="TextBlock"/> has a <see cref="PropertyBinding"/> that binds the <paramref name="DataContext"/>'s "Name" property to the <see cref="TextBlock"/>'s "Text" property,<br/>
        /// then this method will use reflection to attempt to retrieve the value of the "Name" property from the <paramref name="DataContext"/>,<br/>
        /// and put that value into the <see cref="TextBlock"/>'s "Text" property.<para/>
        /// This only handles <see cref="BindingMode.OneTime"/> bindings.</summary>
        private static void PreProcessBindings(Element Element, object DataContext)
        {
            if (Element == null || DataContext == null)
                return;

            //  Evaluate each binding
            foreach (PropertyBinding Binding in Element.PropertyBindings)
            {
                PropertyInfo TargetProperty = Element.GetType().GetProperty(Binding.TargetPropertyName);
                if (TargetProperty != null && TryGetPropertyValue(DataContext, Binding.Path, out object Value))
                {
                    //  Prioritize the underlying type in case the type is a Nullable
                    Type TargetType = Nullable.GetUnderlyingType(TargetProperty.PropertyType) ?? TargetProperty.PropertyType;
                    object TargetValue = Value == null ? null : Value.GetType().IsAssignableTo(TargetType) ? Value : Convert.ChangeType(Value, TargetType);
                    TargetProperty.SetValue(Element, TargetValue, null);
                }
            }

            foreach (Element Child in Element.GetChildren())
                PreProcessBindings(Child, DataContext);
        }

        private static void PostProcessBindings(MGElement RootElement, INotifyPropertyChanged DataContext)
        {
            if (RootElement == null || DataContext == null)
                return;

            foreach (MGElement Element in RootElement.TraverseVisualTree(true, true, MGElement.TreeTraversalMode.Preorder))
            {
                if (Element.OneWayPropertyBindings.Any())
                {
                    DataContext.PropertyChanged += (sender, e) =>
                    {
                        foreach (PropertyBinding Binding in Element.OneWayPropertyBindings)
                        {
                            if (e.PropertyName == Binding.Path)
                            {
                                PropertyInfo TargetProperty = Element.GetType().GetProperty(Binding.TargetPropertyName);
                                if (TargetProperty != null)
                                {
                                    PropertyInfo SourceProperty = DataContext.GetType().GetProperty(Binding.Path);

                                    //  Retrieve the updated value from the DataContext
                                    if (SourceProperty != null && TryGetPropertyValue(DataContext, Binding.Path, out object SourceValue))
                                    {
                                        //  Prioritize the underlying type in case the type is a Nullable
                                        Type TargetType = Nullable.GetUnderlyingType(TargetProperty.PropertyType) ?? TargetProperty.PropertyType;
                                        object TargetValue = SourceValue == null ? null : Convert.ChangeType(SourceValue, TargetType);

                                        //  Set the value on the MGElement
                                        TargetProperty.SetValue(Element, TargetValue, null);
                                    }
                                }

                                break;
                            }
                        }
                    };
                }
            }
        }

        private static bool TryGetPropertyValue<T>(object DataContext, string PropertyName, out T PropertyValue)
        {
            if (DataContext == null || string.IsNullOrEmpty(PropertyName))
            {
                PropertyValue = default;
                return false;
            }

            //  Special case: A path of "." just refers to the entire DataContext, rather than a property within the DataContext
            if (PropertyName == ".")
            {
                if (!DataContext.GetType().IsAssignableTo(typeof(T)))
                {
                    PropertyValue = default;
                    return false;
                }
                else
                {
                    PropertyValue = (T)DataContext;
                    return true;
                }
            }

            //  Get the property
            Type DataType = DataContext.GetType();
            PropertyInfo Property = DataType.GetProperty(PropertyName);
            if (Property == null)
            {
                PropertyValue = default;
                return false;
            }

            //  Validate the type
            if (!Property.PropertyType.IsAssignableTo(typeof(T)))
            {
                PropertyValue = default;
                return false;
            }

            //  Get the value
            PropertyValue = (T)Property.GetValue(DataContext);
            return true;
        }
    }

    public enum BindingMode
    {
        OneTime,
        OneWay
    }

    public readonly record struct PropertyBinding(string Path, BindingMode Mode, object FallbackValue, string TargetPropertyName)
    {
        /// <summary>A property path that indicates the value should be retrieved directly from the DataContext, rather than from a property within the DataContext.</summary>
        public const string SelfBindingPath = ".";
        public bool IsSelfBinding => Path == SelfBindingPath;
    }

#if UseWPF
    public class PropertyBindingExtension : MarkupExtension
    {
        public string Path { get; set; } = PropertyBinding.SelfBindingPath;
        public BindingMode Mode { get; set; } = BindingMode.OneTime;
        public object FallbackValue { get; set; }

        public PropertyBindingExtension() { }

        public override object ProvideValue(IServiceProvider Provider)
        {
            IProvideValueTarget ProvideValueTarget = (IProvideValueTarget)Provider.GetService(typeof(IProvideValueTarget));
            if (ProvideValueTarget.TargetProperty is PropertyInfo TargetProperty && ProvideValueTarget.TargetObject is Element TargetObject)
            {
                //  Save the binding info onto the target object so it can be evaluated later on (once we have a source DataContext to retrieve the data from)
                TargetObject.PropertyBindings.Add(new(Path, Mode, FallbackValue, TargetProperty.Name));

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
