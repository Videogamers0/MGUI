using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace MGUI.Core.UI.XAML
{
    public enum DataContextResolver
    {
        /// <summary>Indicates that data should be read from a 'DataContext' property on the targeted object.</summary>
        DataContext,
        /// <summary>Indicates that data should be read directly from the targeted object.</summary>
        Self
    }

    public enum DataBindingMode
    {
        /// <summary>Indicates that the source object's property value should be read once while initializing the binding, and propagated to the target object's property value.<para/>
        /// The binding will not attempt to dynamically update the target object's property value if the source object's property value changes after initialization.</summary>
        OneTime,
        /// <summary>Indicates that the source object's property value should be propagated to the target object's property value.<para/>
        /// The binding will listen for changes to the source object's property value and attempt to dynamically update the target whenever a change is detected.<para/>
        /// This is the opposite binding direction of <see cref="OneWayToSource"/></summary>
        OneWay,
        /// <summary>Indicates that the target object's property value should be propagated to the source object's property value.<para/>
        /// The binding will listen for changes to the target object's property value and attempt to dynamically update the source whenever a change is detected.<para/>
        /// This is the opposite binding direction of <see cref="OneWay"/></summary>
        OneWayToSource,
        /// <summary>Indicates that the source object's property value should be kept in sync with the target object's property value.<para/>
        /// If either the source or the target value changes, the binding will dynamically update the other value immediately after.<para/>
        /// This mode effectively combines the functionality of <see cref="OneWay"/> and <see cref="OneWayToSource"/></summary>
        TwoWay
        //OneTimeToSource? The opposite of OneTime. Read Target object's property value during initialization, copy that value to the source object's property value.
    }

    #region Resolvers
    /// <summary>For concrete implementations, use:<br/>
    /// <see cref="SourceObjectResolverSelf"/><br/>
    /// <see cref="SourceObjectResolverElementName"/><br/>
    /// <see cref="SourceObjectResolverElementAncestor{T}"/><br/>
    /// <see cref="SourceObjectResolverDesktop"/></summary>
    public interface ISourceObjectResolver
    {
        public object ResolveSourceObject(object TargetObject);

        private static readonly SourceObjectResolverSelf SelfResolver = new();
        /// <summary>Indicates that the source object of the binding is the same as the Targeted object</summary>
        public static ISourceObjectResolver FromSelf() => SelfResolver;

        /// <summary>Indicates that the source object of the binding should be retrieved via <see cref="MGWindow.GetElementByName(string)"/><br/>
        /// (assuming the target object is of type <see cref="MGElement"/> and belongs to a <see cref="MGWindow"/>)</summary>
        public static ISourceObjectResolver FromElementName(string ElementName) => new SourceObjectResolverElementName(ElementName);

        /// <summary>Indicates that the source object of the binding should be retrieved by traversing up the visual tree 
        /// by a certain number of hierarchical levels and looking for a parent of a particular <typeparamref name="T"/> type.<br/>
        /// (assuming the target object is of type <see cref="MGElement"/>)</summary>
        public static ISourceObjectResolver FromElementAncestor<T>(int AncestorLevel = 1) where T : MGElement => new SourceObjectResolverElementAncestor<T>(AncestorLevel);
        public static ISourceObjectResolver FromElementAncestor(int AncestorLevel = 1) => FromElementAncestor<MGElement>(AncestorLevel);

        private static readonly SourceObjectResolverDesktop DesktopResolver = new();
        /// <summary>Indicates that the source object of the binding should be retrieved via <see cref="MGElement.GetDesktop"/><br/>
        /// (assuming the target object is of type <see cref="MGElement"/>)</summary>
        public static ISourceObjectResolver FromDesktop() => DesktopResolver;
    }

    /// <summary>Indicates that the source object of the binding is the same as the Targeted object</summary>
    public class SourceObjectResolverSelf : ISourceObjectResolver
    {
        public SourceObjectResolverSelf() { }
        public object ResolveSourceObject(object TargetObject) => TargetObject;
        public override string ToString() => $"{nameof(SourceObjectResolverSelf)}";
    }

    /// <summary>Indicates that the source object of the binding should be retrieved via <see cref="MGWindow.GetElementByName(string)"/><br/>
    /// (assuming the target object is of type <see cref="MGElement"/> and belongs to a <see cref="MGWindow"/>)</summary>
    public class SourceObjectResolverElementName : ISourceObjectResolver
    {
        public readonly string ElementName;

        public SourceObjectResolverElementName(string ElementName)
        {
            this.ElementName = ElementName ?? throw new ArgumentNullException(nameof(ElementName));
        }

        public object ResolveSourceObject(object TargetObject)
        {
            if (TargetObject is MGElement Element && Element.SelfOrParentWindow.TryGetElementByName(ElementName, out MGElement NamedElement))
                return NamedElement;
            else
                return null;
        }

        public override string ToString() => $"{nameof(SourceObjectResolverElementName)}: {ElementName}";
    }

    /// <summary>Indicates that the source object of the binding should be retrieved by traversing up the visual tree 
    /// by a certain number of hierarchical levels and looking for a parent of a particular <typeparamref name="T"/> type.<br/>
    /// (assuming the target object is of type <see cref="MGElement"/>)</summary>
    public class SourceObjectResolverElementAncestor<T> : ISourceObjectResolver
        where T : MGElement
    {
        /// <summary>The number of matches that must be found before ending the search.<para/>
        /// EX: If <see cref="AncestorLevel"/>=2 and <typeparamref name="T"/>=typeof(<see cref="MGBorder"/>), 
        /// this resolver will look for the 2nd <see cref="MGBorder"/> parent when traversing the visual tree upwards.</summary>
        public readonly int AncestorLevel;

        /// <param name="AncestorLevel">The number of matches that must be found before ending the search.<para/>
        /// EX: If <paramref name="AncestorLevel"/>=2 and <typeparamref name="T"/>=typeof(<see cref="MGBorder"/>), 
        /// this resolver will look for the 2nd <see cref="MGBorder"/> parent when traversing the visual tree upwards.</param>
        public SourceObjectResolverElementAncestor(int AncestorLevel = 1)
        {
            this.AncestorLevel = AncestorLevel;
        }

        public object ResolveSourceObject(object TargetObject)
        {
            if (AncestorLevel == 0)
                return TargetObject;

            if (TargetObject is MGElement Element)
            {
                int Count = AncestorLevel;
                MGElement Current = Element;

                while (Count > 0 && Current != null)
                {
                    Current = Current.Parent;
                    if (Current is T)
                    {
                        Count--;
                        if (Count == 0)
                            return Current;
                    }
                }
            }

            return null;
        }

        public override string ToString() => $"{nameof(SourceObjectResolverElementAncestor<T>)}: {typeof(T).Name} ({AncestorLevel})";
    }

    /// <summary>Indicates that the source object of the binding should be retrieved via <see cref="MGElement.GetDesktop"/><br/>
    /// (assuming the target object is of type <see cref="MGElement"/>)</summary>
    public class SourceObjectResolverDesktop : ISourceObjectResolver
    {
        public SourceObjectResolverDesktop() { }

        public object ResolveSourceObject(object TargetObject)
        {
            if (TargetObject is MGElement Element)
                return Element.GetDesktop();
            else
                return null;
        }

        public override string ToString() => $"{nameof(SourceObjectResolverDesktop)}";
    }

    //TODO: After you make a 'Resources' class, it could have a Dictionary<string, object> or something that DataBindings could reference by string key
    //public class SourceObjectResolverNamedResource : ISourceObjectResolver
    //		or could call it SourceObjectResolverStaticResource
    #endregion Resolvers

    public readonly record struct MGBinding(string TargetPropertyName, string SourcePath, DataBindingMode BindingMode = DataBindingMode.OneWay,
        ISourceObjectResolver SourceResolver = null, DataContextResolver DataContextResolver = DataContextResolver.DataContext)
    {
        public string TargetPropertyName { get; init; } = TargetPropertyName ?? throw new ArgumentNullException(nameof(TargetPropertyName));
        public string SourcePath { get; init; } = SourcePath ?? throw new ArgumentNullException(nameof(SourcePath));
        public ReadOnlyCollection<string> SourcePaths { get; } =
            SourcePath == string.Empty ? new List<string>() { "" }.AsReadOnly() :
            SourcePath.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly();
        public ISourceObjectResolver SourceResolver { get; init; } = SourceResolver ?? ISourceObjectResolver.FromSelf();
    }

    public interface IObservableDataContext
    {
        public const string DefaultDataContextPropertyName = "DataContext";
        public string DataContextPropertyName => DefaultDataContextPropertyName;
        event EventHandler<object> DataContextChanged;
    }

    /// <summary>To instantiate a binding, use <see cref="DataBindingManager.AddBinding"/></summary>
    public sealed class DataBinding : IDisposable
    {
        public readonly MGBinding Config;

        public readonly object TargetObject;
        public readonly string TargetPropertyName;
        public readonly PropertyInfo TargetProperty;
        public readonly Type TargetPropertyType;

        private object _SourceRoot;
        /// <summary>The root of the source object, as determined by <see cref="MGBinding.SourceResolver"/> and <see cref="MGBinding.DataContextResolver"/>.<para/>
        /// This might not be the same as <see cref="SourceObject"/> if the <see cref="MGBinding.SourcePath"/> must recurse nested objects.<br/>
        /// For example, if the <see cref="MGBinding.SourcePath"/> is "Foo.Bar", then the <see cref="SourceRoot"/> would be the object containing the Foo property, 
        /// and the <see cref="SourceObject"/> would be the Foo object itself. ("Bar" would be the Source Property's Name)</summary>
        public object SourceRoot
        {
            get => _SourceRoot;
            private set
            {
                if (_SourceRoot != value)
                {
                    _SourceRoot = value;
                    if (Config.SourcePaths.Count <= 1)
                        SourceObject = SourceRoot;
                    else
                        SourceObject = ResolvePath(SourceRoot, Config.SourcePaths, true);

                    //TODO need to listen for changes to all objects along the path.
                    //when any of them are changed, SourceObject must be re-calculated starting from the changed object's position along the path
                    //so if the path were "Foo.Bar.Baz", listen for changes to Foo's Value, or Bar's Value within the Foo object
                    //if Foo's value changes, recalculate Bar from the new Foo value. If Bar changes, just set SourceObject to the new Bar, don't need to recalculate from Foo
                }
            }
        }

        private object _SourceObject;
        public object SourceObject
        {
            get => _SourceObject;
            private set
            {
                if (_SourceObject != value)
                {
                    if (IsSubscribedToSourceObjectPropertyChanged && SourceObject is INotifyPropertyChanged PreviousObservableSourceObject)
                    {
                        PreviousObservableSourceObject.PropertyChanged -= ObservableSourceObject_PropertyChanged;
                    }

                    _SourceObject = value;
                    SourceProperty = GetPublicProperty(SourceObject, SourcePropertyName);

                    //  Update the target object's property value if the binding is directly on the source object (instead of a property on the source object)
                    if (string.IsNullOrEmpty(SourcePropertyName) && Config.BindingMode is DataBindingMode.OneTime or DataBindingMode.OneWay)
                    {
                        TrySetPropertyValue(SourceObject, TargetObject, TargetProperty, TargetPropertyType);
                    }

                    if (Config.BindingMode is DataBindingMode.OneTime)
                    {
                        TrySetPropertyValue(SourceObject, SourceProperty, SourcePropertyType, TargetObject, TargetProperty, TargetPropertyType);
                    }

                    //  Listen for changes to the source object's property value
                    if (SourceProperty != null && Config.BindingMode is DataBindingMode.OneWay or DataBindingMode.TwoWay &&
                        SourceObject is INotifyPropertyChanged ObservableSourceObject)
                    {
                        ObservableSourceObject.PropertyChanged += ObservableSourceObject_PropertyChanged;
                        IsSubscribedToSourceObjectPropertyChanged = true;
                    }
                    else
                        IsSubscribedToSourceObjectPropertyChanged = false;
                }
            }
        }

        private PropertyInfo _SourceProperty;
        public PropertyInfo SourceProperty
        {
            get => _SourceProperty;
            private set
            {
                if (_SourceProperty != value)
                {
                    _SourceProperty = value;
                    SourcePropertyType = GetUnderlyingType(SourceProperty);
                    SourcePropertyValueChanged();
                }
            }
        }

        public Type SourcePropertyType { get; private set; }

        public readonly string SourcePropertyName;

        public static object ResolvePath(object Root, IList<string> PropertyNames, bool ExcludeLast = false)
        {
            object Current = Root;
            for (int i = 0; i < PropertyNames.Count; i++)
            {
                if (Current == null || ExcludeLast && i == PropertyNames.Count - 1)
                    break;

                string PropertyName = PropertyNames[i];
                PropertyInfo Property = GetPublicProperty(Current, PropertyName);
                Current = Property?.GetValue(Current, null);
            }

            return Current;
        }

        private static readonly Dictionary<object, Dictionary<string, PropertyInfo>> CachedProperties = new();
        private static PropertyInfo GetPublicProperty(object Parent, string PropertyName)
        {
            if (Parent == null || string.IsNullOrEmpty(PropertyName))
                return null;

            if (!CachedProperties.TryGetValue(Parent, out Dictionary<string, PropertyInfo> PropertiesByName))
            {
                PropertiesByName = new();
                CachedProperties.Add(Parent, PropertiesByName);
            }

            if (!PropertiesByName.TryGetValue(PropertyName, out PropertyInfo PropInfo))
            {
                PropInfo = Parent.GetType().GetProperty(PropertyName);
                PropertiesByName.Add(PropertyName, PropInfo);
            }

            return PropInfo;
        }

        /// <summary>Retrieve the given <paramref name="PropInfo"/>'s <see cref="PropertyInfo.PropertyType"/>, 
        /// but prioritizes the underlying type if the type is wrapped in a Nullable&lt;T&gt;</summary>
        private static Type GetUnderlyingType(PropertyInfo PropInfo) =>
            PropInfo == null ? null : Nullable.GetUnderlyingType(PropInfo.PropertyType) ?? PropInfo.PropertyType;

        //TODO:
        //Call NotifyPropertyChanged when properties in MGElement subclasses change, such as ProgressBar.Minimum, CheckBox.IsChecked, Element.VerticalAlignment etc
        //figure out how to use the new MGBinding markup extension for things like MGImage.Texture which uses its own special 'SetTexture' method to change the value
        //      or for things like IFillBrushes where the XAML value is a slightly different type
        //Test 2-way bindings
        //In classes that use Template.GetContent, should implement logic that disposes of old bindings when the old item is removed
        //      such as in MGComboBox.ItemsSource's CollectionChanged, the removed items should be removed from DataBindingManager
        //      to unsubscribe from any propertychanged subscriptions

        internal DataBinding(MGBinding Config, object TargetObject)
        {
            this.Config = Config;
            this.TargetObject = TargetObject;
            TargetPropertyName = Config.TargetPropertyName;
            TargetProperty = GetPublicProperty(TargetObject, Config.TargetPropertyName);
            TargetPropertyType = GetUnderlyingType(TargetProperty);
            SourcePropertyName = Config.SourcePaths.Count > 0 ? Config.SourcePaths[^1] : null;

            //  The Source object is computed in 3 steps:
            //  1. Use the SourceObjectResolver to determine where to start
            //			If SourceObjectResolverType=Self, we start from the TargetObject
            //			If SourceObjectResolverType=ElementName, we start from a particular named element in the TargetObject's Window
            //			If SourceObjectResolverType=Desktop, we start from the TargetObject's Desktop
            //  2. Use the DataContextResolver to determine if we should be reading directly from
            //			the 1st step's result, or if we additionally have to read the value of a 'DataContext' property.
            //  3. Traverse the path given by SourcePaths
            //			EX: If SourcePaths="A.B.C" then we want to take the result of step #2, and look for a property named "A".
            //				Then from that property's value, look for a property named "B" and take it's value.
            //				since we only want the parent property of the innermost source property, we would stop at object "B".

            object InitialSourceRoot = Config.SourceResolver.ResolveSourceObject(TargetObject);
            switch (Config.DataContextResolver)
            {
                case DataContextResolver.DataContext:
                    //  Retrieve the value of the "DataContext" property
                    if (InitialSourceRoot is IObservableDataContext DataContextHost)
                    {
                        string DataContextPropertyName = DataContextHost.DataContextPropertyName;
                        PropertyInfo DataContextProperty = GetPublicProperty(InitialSourceRoot, DataContextPropertyName);
                        SourceRoot = DataContextProperty?.GetValue(InitialSourceRoot);
                        DataContextHost.DataContextChanged += (sender, e) => { SourceRoot = DataContextProperty.GetValue(InitialSourceRoot); };
                    }
                    else
                    {
                        string DataContextPropertyName = IObservableDataContext.DefaultDataContextPropertyName;
                        PropertyInfo DataContextProperty = GetPublicProperty(InitialSourceRoot, DataContextPropertyName);
                        SourceRoot = DataContextProperty?.GetValue(InitialSourceRoot);
                    }
                    break;
                case DataContextResolver.Self:
                    SourceRoot = InitialSourceRoot;
                    break;
                default: throw new NotImplementedException($"Unrecognized {nameof(DataContextResolver)}: {Config.DataContextResolver}");
            }

            //  Listen for changes to the target object's property value
            if (TargetProperty != null && Config.BindingMode is DataBindingMode.OneWayToSource or DataBindingMode.TwoWay &&
                TargetObject is INotifyPropertyChanged ObservableTargetObject)
            {
                ObservableTargetObject.PropertyChanged += ObservableTargetObject_PropertyChanged;
                IsSubscribedToTargetObjectPropertyChanged = true;
            }
            else
                IsSubscribedToTargetObjectPropertyChanged = false;
        }

        #region Set Property Value
        private bool IsSettingValue = false;

        private bool TrySetPropertyValue(object Value, object TargetObject, PropertyInfo TargetProperty, Type TargetPropertyType)
        {
            if (IsSettingValue)
                return false;

            try
            {
                IsSettingValue = true;
                return TrySetValue(Value, TargetObject, TargetProperty, TargetPropertyType);
            }
            finally { IsSettingValue = false; }
        }

        private bool TrySetPropertyValue(object SourceObject, PropertyInfo SourceProperty, Type SourcePropertyType,
            object TargetObject, PropertyInfo TargetProperty, Type TargetPropertyType)
        {
            if (IsSettingValue)
                return false;

            try
            {
                IsSettingValue = true;
                return TrySetValue(SourceObject, SourceProperty, SourcePropertyType, TargetObject, TargetProperty, TargetPropertyType);
            }
            finally { IsSettingValue = false; }
        }

        /// <summary>Attempts to copy the given <paramref name="Value"/> into the <paramref name="TargetObject"/>'s <paramref name="TargetProperty"/>.</summary>
        /// <param name="TargetPropertyType">If null, will be retrieved via <see cref="GetUnderlyingType(PropertyInfo)"/></param>
        private static bool TrySetValue(object Value, object TargetObject, PropertyInfo TargetProperty, Type TargetPropertyType)
        {
            if (TargetObject != null && TargetProperty != null)
            {
                Type SourceType = Value?.GetType();
                TargetPropertyType ??= GetUnderlyingType(TargetProperty);

                if ((Value == null && !TargetPropertyType.IsValueType) || (Value != null && TypeDescriptor.GetConverter(SourceType).CanConvertTo(TargetPropertyType)))
                {
                    object ActualValue = Value == null ? null : SourceType.IsAssignableTo(TargetPropertyType) ? Value : Convert.ChangeType(Value, TargetPropertyType);
                    TargetProperty.SetValue(TargetObject, ActualValue);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Attempts to copy the value of the <paramref name="SourceObject"/>'s <paramref name="SourceProperty"/> into the 
		/// <paramref name="TargetObject"/>'s <paramref name="TargetProperty"/>.</summary>
        /// <param name="SourcePropertyType">If null, will be retrieved via <see cref="GetUnderlyingType(PropertyInfo)"/></param>
        /// <param name="TargetPropertyType">If null, will be retrieved via <see cref="GetUnderlyingType(PropertyInfo)"/></param>
        private static bool TrySetValue(object SourceObject, PropertyInfo SourceProperty, Type SourcePropertyType,
            object TargetObject, PropertyInfo TargetProperty, Type TargetPropertyType)
        {
            if (SourceObject != null && SourceProperty != null && TargetObject != null && TargetProperty != null)
            {
                SourcePropertyType ??= GetUnderlyingType(SourceProperty);
                TargetPropertyType ??= GetUnderlyingType(TargetProperty);

                if (TypeDescriptor.GetConverter(SourcePropertyType).CanConvertTo(TargetPropertyType))
                {
                    object Value = SourceProperty.GetValue(SourceObject);
                    object ActualValue = Value == null ? null : SourcePropertyType.IsAssignableTo(TargetPropertyType) ? Value : Convert.ChangeType(Value, TargetPropertyType);
                    TargetProperty.SetValue(TargetObject, ActualValue);
                    return true;
                }
            }

            return false;
        }
#endregion Set Property Value

        /// <summary>True if this binding subscribed to the <see cref="SourceObject"/>'s PropertyChanged event the last time <see cref="SourceObject"/> was set.<para/>
        /// If true, the event must be unsubscribed from when changing <see cref="SourceObject"/> or when disposing this <see cref="DataBinding"/></summary>
        private bool IsSubscribedToSourceObjectPropertyChanged = false;

        private void ObservableSourceObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == SourcePropertyName)
                SourcePropertyValueChanged();
        }

        /// <summary>True if this binding subscribed to the <see cref="TargetObject"/>'s PropertyChanged event during initialization.<para/>
        /// If true, the event must be unsubscribed from when disposing this <see cref="DataBinding"/></summary>
        private readonly bool IsSubscribedToTargetObjectPropertyChanged;

        private void ObservableTargetObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Config.TargetPropertyName)
                TargetPropertyValueChanged();
        }

        private void SourcePropertyValueChanged()
        {
            //TODO Converters? Instead of just IsAssignableTo, need to invoke the Converter on the Value first

            if (!IsSettingValue && Config.BindingMode is DataBindingMode.OneWay or DataBindingMode.TwoWay)
            {
                TrySetPropertyValue(SourceObject, SourceProperty, SourcePropertyType, TargetObject, TargetProperty, TargetPropertyType);
            }
        }

        private void TargetPropertyValueChanged()
        {
            //TODO Converters? Instead of just IsAssignableTo, need to invoke the Converter on the Value first

            //  Propagate the new value to the SourceProperty
            if (!IsSettingValue && Config.BindingMode is DataBindingMode.OneWayToSource or DataBindingMode.TwoWay)
            {
                TrySetPropertyValue(TargetObject, TargetProperty, TargetPropertyType, SourceObject, SourceProperty, SourcePropertyType);
            }
        }

        public bool IsDisposed { get; private set; }
        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;

                //  Unsubscribe from PropertyChanged events
                if (IsSubscribedToTargetObjectPropertyChanged && TargetObject is INotifyPropertyChanged ObservableTargetObject)
                {
                    ObservableTargetObject.PropertyChanged -= ObservableTargetObject_PropertyChanged;
                }
                if (IsSubscribedToSourceObjectPropertyChanged && SourceObject is INotifyPropertyChanged ObservableSourceObject)
                {
                    ObservableSourceObject.PropertyChanged -= ObservableSourceObject_PropertyChanged;
                    IsSubscribedToSourceObjectPropertyChanged = false;
                }
            }
        }
    }

    /// <summary>Static class that keeps track of all <see cref="DataBinding"/>s for all objects.</summary>
    public static class DataBindingManager
    {
        private static readonly List<DataBinding> _Bindings = new();
        public static IReadOnlyList<DataBinding> Bindings => _Bindings;

        private static readonly Dictionary<object, List<DataBinding>> _BindingsByTargetObject = new();

        public static DataBinding AddBinding(MGBinding Config, object TargetObject)
        {
            if (TargetObject == null)
                throw new ArgumentNullException(nameof(TargetObject));

            DataBinding Binding = new(Config, TargetObject);
            _Bindings.Add(Binding);

            if (_BindingsByTargetObject.TryGetValue(TargetObject, out List<DataBinding> ObjectBindings))
            {
                //  Validate that there isn't already a binding for this TargetObject/TargetProperty tuple
                //  (TODO: I guess we could instead remove the existing binding to replace it with the new one)
                if (ObjectBindings.Any(x => x.TargetPropertyName == Config.TargetPropertyName))
                {
                    throw new InvalidOperationException($"Unable to bind to target property '{Config.TargetPropertyName}' of target object '{TargetObject}' " +
                        $"because a binding was already created for this target.");
                }
            }
            else
            {
                ObjectBindings = new();
                _BindingsByTargetObject.Add(TargetObject, ObjectBindings);
            }

            ObjectBindings.Add(Binding);

            return Binding;
        }

        public static bool RemoveBinding(DataBinding Binding)
        {
            if (Binding == null)
                throw new ArgumentNullException(nameof(Binding));

            bool Result = _Bindings.Remove(Binding);

            if (_BindingsByTargetObject.TryGetValue(Binding.TargetObject, out List<DataBinding> ObjectBindings))
            {
                if (ObjectBindings.Remove(Binding) && ObjectBindings.Count == 0)
                    _BindingsByTargetObject.Remove(Binding.TargetObject);
            }

            if (Result)
                Binding.Dispose();
            return Result;
        }

        /// <summary>Removes all <see cref="DataBinding"/>s that use <paramref name="TargetObject"/> as their <see cref="DataBinding.TargetObject"/></summary>
        /// <returns>The count of the removed bindings</returns>
        public static int RemoveBindings(object TargetObject)
        {
            if (TargetObject == null)
                throw new ArgumentNullException(nameof(TargetObject));

            if (_BindingsByTargetObject.TryGetValue(TargetObject, out List<DataBinding> ObjectBindings))
            {
                foreach (DataBinding Binding in ObjectBindings)
                    RemoveBinding(Binding);
                return ObjectBindings.Count;
            }
            else
                return 0;
        }
    }

#if UseWPF
    public class MGBindingExtension : MarkupExtension
    {
        public string Path { get; set; } = "";
        public string ElementName { get; set; } = null;
        public DataBindingMode Mode { get; set; } = DataBindingMode.OneWay;
        public DataContextResolver DataContextResolver { get; set; } = DataContextResolver.DataContext;
        public object FallbackValue { get; set; }

        public ISourceObjectResolver SourceObjectResolver =>
            string.IsNullOrEmpty(ElementName) ? ISourceObjectResolver.FromSelf() : ISourceObjectResolver.FromElementName(ElementName);

        public MGBindingExtension() { }

        private MGBinding ToBinding(string TargetPropertyName, object FallbackValue)
            => new(TargetPropertyName, Path, Mode, SourceObjectResolver, DataContextResolver);//, FallbackValue);

        public override object ProvideValue(IServiceProvider Provider)
        {
            IProvideValueTarget ProvideValueTarget = (IProvideValueTarget)Provider.GetService(typeof(IProvideValueTarget));
            if (ProvideValueTarget.TargetProperty is PropertyInfo TargetProperty && ProvideValueTarget.TargetObject is Element TargetObject)
            {
                //  Save the binding info onto the target object so it can be evaluated later on (once we've instantiated the MGElement instance from the Element object)
                TargetObject.Bindings.Add(ToBinding(TargetProperty.Name, FallbackValue));

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
