using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.XAML;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

#if UseWPF
using System.Windows.Data;
#else
using Portable.Xaml;
using MGUI.Core.UI.Data_Binding.Converters;
#endif

namespace MGUI.Core.UI.Data_Binding
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

    public interface IObservableDataContext
    {
        public const string DefaultDataContextPropertyName = "DataContext";
        public string DataContextPropertyName => DefaultDataContextPropertyName;
        event EventHandler<object> DataContextChanged;
    }

    /// <summary>To instantiate a binding, use <see cref="DataBindingManager.AddBinding"/></summary>
    public sealed class DataBinding : IDisposable, ITypeDescriptorContext
    {
        //"Weak event pattern" information to avoid memory leaks with the event listeners:
        //https://learn.microsoft.com/en-us/dotnet/desktop/wpf/events/weak-event-patterns?view=netdesktop-7.0&redirectedfrom=MSDN
        //https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.propertychangedeventmanager?redirectedfrom=MSDN&view=windowsdesktop-7.0

        public readonly BindingConfig Config;

        public readonly object TargetObject;
        public readonly string TargetPropertyName;
        public readonly PropertyInfo TargetProperty;
        public readonly Type TargetPropertyType;

        private readonly record struct ConverterConfig(IValueConverter Converter, object Parameter, bool IsConvertingBack)
        {
            public object Apply(object Value, Type TargetType) => IsConvertingBack ?
                Converter.ConvertBack(Value, TargetType, Parameter, CultureInfo.CurrentCulture) :
                Converter.Convert(Value, TargetType, Parameter, CultureInfo.CurrentCulture);
        }
        private readonly ConverterConfig? ConvertSettings;
        private readonly ConverterConfig? ConvertBackSettings;

        private object _SourceRoot;
        /// <summary>The root of the source object, as determined by <see cref="BindingConfig.SourceResolver"/> and <see cref="BindingConfig.DataContextResolver"/>.<para/>
        /// This might not be the same as <see cref="SourceObject"/> if the <see cref="BindingConfig.SourcePath"/> must recurse nested objects.<br/>
        /// For example, if the <see cref="BindingConfig.SourcePath"/> is "Foo.Bar", then the <see cref="SourceRoot"/> would be the object containing the Foo property, 
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
                        FindAndTrackSourceObject();
                }
            }
        }

        private void FindAndTrackSourceObject()
        {
            ClearPathChangeListeners();
            SourceObject = ResolvePath(SourceRoot, Config.SourcePaths, true);

            //  Refresh the SourceObject when any object along the source path changes its value
            //  EX: SourcePath="Foo.Bar.Baz"
            //  SourceObject must be updated when the Foo property value of the SourceRoot changes, or when the Bar property value of the Foo object changes.
            if (SourceRoot != null && Config.SourcePaths.Count > 1)
            {
                IList<string> PropertyNames = Config.SourcePaths;

                object Current = SourceRoot;
                for (int i = 0; i < PropertyNames.Count - 1; i++)
                {
                    string PropertyName = PropertyNames[i];
                    PropertyInfo Property = GetPublicProperty(Current, PropertyName);
                    if (Property == null)
                        break;

                    if (Current is INotifyPropertyChanged PropChangedObject)
                        UpdateSourceObjectWhenPropertyChanges(new(PropChangedObject, PropertyName));

                    Current = Property.GetValue(Current, null);
                    if (Current == null)
                        break;
                }
            }
        }

        private readonly record struct PropertyChangedSourceMetadata(INotifyPropertyChanged Object, string PropertyName);

#if UseWPF
        private readonly List<PropertyChangedSourceMetadata> PathChangeHandlers = new();
#else
        private readonly List<PropertyNameHandler> PathChangeHandlers = new();
#endif

        private void UpdateSourceObjectWhenPropertyChanges(PropertyChangedSourceMetadata Item)
        {
#if UseWPF
            PropertyChangedEventManager.AddHandler(Item.Object, UpdateSourceObject, Item.PropertyName);
            PathChangeHandlers.Add(Item);
#else
            //  Maybe use this: https://github.com/davidmilligan/WeakEventListener/blob/master/WeakEventListener/WeakEventManager.cs
            PropertyNameHandler Handler = new(Item.Object, Item.PropertyName, UpdateSourceObject);
            PathChangeHandlers.Add(Handler);
#endif
        }
        private void ClearPathChangeListeners()
        {
            if (PathChangeHandlers.Any())
            {
                try
                {
#if UseWPF
                    foreach (PropertyChangedSourceMetadata Item in PathChangeHandlers)
                        PropertyChangedEventManager.RemoveHandler(Item.Object, UpdateSourceObject, Item.PropertyName);
#else
                    foreach (PropertyNameHandler Handler in PathChangeHandlers)
                        Handler.Detach();
#endif
                }
                finally { PathChangeHandlers.Clear(); }
            }
        }

        private void UpdateSourceObject(object sender, PropertyChangedEventArgs e) => FindAndTrackSourceObject();

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
#if UseWPF
                        PropertyChangedEventManager.RemoveHandler(PreviousObservableSourceObject, ObservableSourceObject_PropertyChanged, SourcePropertyName);
#else
                        PreviousObservableSourceObject.PropertyChanged -= ObservableSourceObject_PropertyChanged;
#endif
                        IsSubscribedToSourceObjectPropertyChanged = false;
                    }

                    _SourceObject = value;
                    SourceProperty = GetPublicProperty(SourceObject, SourcePropertyName);
                    SourcePropertyValueChanged();

                    //  Apply the FallbackValue if we couldn't find a valid property to bind to
                    if (SourceProperty == null && Config.FallbackValue != null && !IsSettingValue &&
                        Config.BindingMode is DataBindingMode.OneTime or DataBindingMode.OneWay or DataBindingMode.TwoWay)
                    {
                        TrySetPropertyValue(Config.FallbackValue, TargetObject, TargetProperty, TargetPropertyType, ConvertSettings);
                    }

                    //  Update the target object's property value if the binding is directly on the source object (instead of a property on the source object)
                    if (string.IsNullOrEmpty(SourcePropertyName) && Config.BindingMode is DataBindingMode.OneTime or DataBindingMode.OneWay)
                    {
                        TrySetPropertyValue(SourceObject, TargetObject, TargetProperty, TargetPropertyType, ConvertSettings);
                    }
                    //  Apply OneTime bindings
                    else if (Config.BindingMode is DataBindingMode.OneTime)
                    {
                        TrySetPropertyValue(SourceObject, SourceProperty, SourcePropertyType, TargetObject, TargetProperty, TargetPropertyType, ConvertSettings);
                    }

                    //  Listen for changes to the source object's property value
                    if (SourceProperty != null && Config.BindingMode is DataBindingMode.OneWay or DataBindingMode.TwoWay &&
                        SourceObject is INotifyPropertyChanged ObservableSourceObject)
                    {
#if UseWPF
                        PropertyChangedEventManager.AddHandler(ObservableSourceObject, ObservableSourceObject_PropertyChanged, SourcePropertyName);
#else
                        ObservableSourceObject.PropertyChanged += ObservableSourceObject_PropertyChanged;
#endif
                        IsSubscribedToSourceObjectPropertyChanged = true;
                    }
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
                if (Current == null || (ExcludeLast && i == PropertyNames.Count - 1))
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

        /// <param name="Object">The object which the property paths (<see cref="BindingConfig.TargetPath"/>, <see cref="BindingConfig.SourcePath"/>) should be retrieved from.</param>
        internal DataBinding(BindingConfig Config, object Object)
        {
            this.Config = Config;
            ConvertSettings = Config.Converter == null ? null : new(Config.Converter, Config.ConverterParameter, false);
            ConvertBackSettings = Config.Converter == null ? null : new(Config.Converter, Config.ConverterParameter, true);

            this.TargetObject = ResolvePath(Object, Config.TargetPaths, true);
            TargetPropertyName = Config.TargetPaths[^1];
            TargetProperty = GetPublicProperty(TargetObject, TargetPropertyName);
            TargetPropertyType = GetUnderlyingType(TargetProperty);
            SourcePropertyName = Config.SourcePaths.Count > 0 ? Config.SourcePaths[^1] : null;

            //  The Source object is computed in 3 steps:
            //  1. Use the SourceObjectResolver to determine where to start
            //			If SourceObjectResolver=Self, we start from the Object constructor parameter
            //			If SourceObjectResolver=ElementName, we start from a particular named element in the Object's Window
            //			If SourceObjectResolver=Desktop, we start from the Object's Desktop
            //  2. Use the DataContextResolver to determine if we should be reading directly from
            //			the 1st step's result, or if we additionally have to read the value of a 'DataContext' property.
            //  3. Traverse the path given by SourcePaths
            //			EX: If SourcePaths="A.B.C" then we want to take the result of step #2, and look for a property named "A".
            //				Then from that property's value, look for a property named "B" and take it's value.
            //				Since we only want the parent property of the innermost source property, we would stop at object "B".

            object InitialSourceRoot = Config.SourceResolver.ResolveSourceObject(Object);
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
#if UseWPF
                PropertyChangedEventManager.AddHandler(ObservableTargetObject, ObservableTargetObject_PropertyChanged, TargetPropertyName);
#else
                ObservableTargetObject.PropertyChanged += ObservableTargetObject_PropertyChanged;
#endif
                IsSubscribedToTargetObjectPropertyChanged = true;
            }
            else
                IsSubscribedToTargetObjectPropertyChanged = false;
        }

        #region Set Property Value
        private bool IsSettingValue = false;

        private bool TrySetPropertyValue(object Value, object TargetObject, PropertyInfo TargetProperty, Type TargetPropertyType, ConverterConfig? ConverterSettings)
        {
            if (IsSettingValue)
                return false;

            try
            {
                IsSettingValue = true;
                return TrySetValue(this, Value, TargetObject, TargetProperty, TargetPropertyType, ConverterSettings, Config.StringFormat);
            }
            finally { IsSettingValue = false; }
        }

        private bool TrySetPropertyValue(object SourceObject, PropertyInfo SourceProperty, Type SourcePropertyType,
            object TargetObject, PropertyInfo TargetProperty, Type TargetPropertyType, ConverterConfig? ConverterSettings)
        {
            if (IsSettingValue)
                return false;

            try
            {
                IsSettingValue = true;
                return TrySetValue(this, SourceObject, SourceProperty, SourcePropertyType, 
                    TargetObject, TargetProperty, TargetPropertyType, 
                    ConverterSettings, Config.StringFormat);
            }
            finally { IsSettingValue = false; }
        }

        /// <summary>Attempts to copy the given <paramref name="Value"/> into the <paramref name="TargetObject"/>'s <paramref name="TargetProperty"/>.</summary>
        /// <param name="TargetPropertyType">If null, will be retrieved via <see cref="GetUnderlyingType(PropertyInfo)"/></param>
        private static bool TrySetValue(ITypeDescriptorContext Context, object Value, object TargetObject, PropertyInfo TargetProperty, Type TargetPropertyType, 
            ConverterConfig? ConverterSettings, string StringFormat)
        {
            if (TargetObject != null && TargetProperty != null)
            {
                Type SourceType = Value?.GetType();
                TargetPropertyType ??= GetUnderlyingType(TargetProperty);

                if (ConverterSettings.HasValue)
                {
                    Value = ConverterSettings.Value.Apply(Value, TargetPropertyType);
                    SourceType = Value?.GetType() ?? typeof(object);
                }

                if ((Value == null && !TargetPropertyType.IsValueType) ||
                    (Value != null && IsAssignableOrConvertible(SourceType, TargetPropertyType)))
                {
                    object ActualValue = ConvertValue(Context, SourceType, TargetPropertyType, Value, null, StringFormat);
                    try { TargetProperty.SetValue(TargetObject, ActualValue); }
                    catch (Exception ex) { Debug.WriteLine(ex); }
                    return true;
                }
            }

            return false;
        }

        /// <summary>Attempts to copy the value of the <paramref name="SourceObject"/>'s <paramref name="SourceProperty"/> into the 
		/// <paramref name="TargetObject"/>'s <paramref name="TargetProperty"/>.</summary>
        /// <param name="SourcePropertyType">If null, will be retrieved via <see cref="GetUnderlyingType(PropertyInfo)"/></param>
        /// <param name="TargetPropertyType">If null, will be retrieved via <see cref="GetUnderlyingType(PropertyInfo)"/></param>
        private static bool TrySetValue(ITypeDescriptorContext Context, object SourceObject, PropertyInfo SourceProperty, Type SourcePropertyType,
            object TargetObject, PropertyInfo TargetProperty, Type TargetPropertyType, ConverterConfig? ConverterSettings, string StringFormat)
        {
            if (SourceObject != null && SourceProperty != null && TargetObject != null && TargetProperty != null)
            {
                SourcePropertyType ??= GetUnderlyingType(SourceProperty);
                TargetPropertyType ??= GetUnderlyingType(TargetProperty);

                object Value = SourceProperty.GetValue(SourceObject);
                if (ConverterSettings.HasValue)
                {
                    Value = ConverterSettings.Value.Apply(Value, TargetPropertyType);
                    SourcePropertyType = Value?.GetType() ?? typeof(object);
                }

                if (ConverterSettings.HasValue || IsAssignableOrConvertible(SourcePropertyType, TargetPropertyType))
                {
                    object ActualValue = ConvertValue(Context, SourcePropertyType, TargetPropertyType, Value, null, StringFormat);
                    try { TargetProperty.SetValue(TargetObject, ActualValue); }
                    catch (Exception ex) { Debug.WriteLine(ex); }
                    return true;
                }
            }

            return false;
        }

        /// <param name="CanAssign">If null, will be computed via <see cref="IsAssignable(Type, Type)"/></param>
        private static object ConvertValue(ITypeDescriptorContext Context, Type SourceType, Type TargetType, object Value, bool? CanAssign, string StringFormat)
        {
            if (Value == null)
                return null;
            else if (StringFormat != null && TargetType == typeof(string))
            {
                try { return string.Format(StringFormat, Value); }
                catch (FormatException) { return Value; }
            }
            else if (CanAssign == true || (!CanAssign.HasValue && IsAssignable(SourceType, TargetType)))
                return Value;
            else if (TryConvertWithTypeConverter(Context, SourceType, TargetType, Value, out object TypeConvertedValue))
                return TypeConvertedValue;
            else if (Value is IConvertible)
            {
                try { return Convert.ChangeType(Value, TargetType); }
                catch (FormatException) { return Value; }
            }
            else if (TargetType == typeof(string))
                return Value.ToString();
            else
                throw new NotImplementedException($"Could not convert value from type='{SourceType.FullName}' to type='{TargetType.FullName}'.");
        }

        private static bool TryConvertWithTypeConverter(ITypeDescriptorContext Context, Type SourceType, Type TargetType, object Value, out object Result)
        {
            if (TryConvertFromWithTypeConverter(Context, SourceType, TargetType, Value, out Result))
                return true;
            else if (TryConvertToWithTypeConverter(Context, SourceType, TargetType, Value, out Result))
                return true;
            else
                return false;
        }

        private static bool TryConvertFromWithTypeConverter(ITypeDescriptorContext Context, Type SourceType, Type TargetType, object Value, out object Result)
        {
            TypeConverter Converter = GetConverter(TargetType);
            if (Converter.CanConvertFrom(SourceType))
            {
                try
                {
                    Result = Context == null ?
                        Converter.ConvertFrom(Value) :
                        Converter.ConvertFrom(Context, CultureInfo.CurrentCulture, Value);
                    return true;
                }
                catch (ArgumentException) { }
            }

            Result = null;
            return false;
        }

        private static bool TryConvertToWithTypeConverter(ITypeDescriptorContext Context, Type SourceType, Type TargetType, object Value, out object Result)
        {
            TypeConverter Converter = GetConverter(SourceType);
            if (Converter.CanConvertTo(TargetType))
            {
                Result = Context == null ?
                    Converter.ConvertTo(Value, TargetType) :
                    Converter.ConvertTo(Context, CultureInfo.CurrentCulture, Value, TargetType);
                return true;
            }
            else
            {
                Result = null;
                return false;
            }
        }

        private static readonly Dictionary<Type, Dictionary<Type, bool>> CachedIsAssignable = new();
        private static bool IsAssignable(Type From, Type To)
        {
            if (From == null || To == null)
                return false;

            if (!CachedIsAssignable.TryGetValue(From, out Dictionary<Type, bool> CanAssignByType))
            {
                CanAssignByType = new();
                CachedIsAssignable.Add(From, CanAssignByType);
            }

            if (!CanAssignByType.TryGetValue(To, out bool CanAssign))
            {
                CanAssign = From.IsAssignableTo(To);
                CanAssignByType.Add(To, CanAssign);
            }

            return CanAssign;
        }

        static DataBinding()
        {
            Dictionary<Type, Type> BuiltInTypeConverters = new()
            {
                { typeof(Microsoft.Xna.Framework.Color), typeof(XNAColorStringConverter) }
            };

            //  Apply some default TypeConverters such as being able to convert a string to a Microsoft.Xna.Framework.Color
            foreach (KeyValuePair<Type, Type> KVP in BuiltInTypeConverters)
            {
                RegisterDefaultTypeConverter(KVP.Key, KVP.Value);
            }
        }

        /// <summary>Registers the given <see cref="TypeConverter"/> as the default converter to use when converting to the given <paramref name="TargetType"/></summary>
        public static void RegisterDefaultTypeConverter(Type TargetType, Type TypeConverter)
            => RegisterDefaultTypeConverter(TargetType, new TypeConverterAttribute(TypeConverter));
        /// <summary>Registers the given <see cref="TypeConverter"/> as the default converter to use when converting to the given <paramref name="TargetType"/></summary>
        public static void RegisterDefaultTypeConverter(Type TargetType, TypeConverterAttribute TypeConverter)
            => TypeDescriptor.AddAttributes(TargetType, TypeConverter);

        private static readonly Dictionary<Type, TypeConverter> CachedConverters = new();
        private static TypeConverter GetConverter(Type Type)
        {
            if (Type == null)
                return null;

            if (!CachedConverters.TryGetValue(Type, out TypeConverter Converter))
            {
                Converter = TypeDescriptor.GetConverter(Type);
                CachedConverters.Add(Type, Converter);
            }

            return Converter;
        }

        private static readonly Dictionary<TypeConverter, Dictionary<Type, bool>> CachedCanConvertFrom = new();
        private static readonly Dictionary<TypeConverter, Dictionary<Type, bool>> CachedCanConvertTo = new();
        private static bool IsConvertible(Type From, Type To) => IsConvertibleFrom(From, To) || IsConvertibleTo(From, To);

        //TODO TypeConverters don't necessarily return the same value for the same input types.
        //CanConvertFrom/CanConvertTo might return different values depending on the ITypeDescriptorContext
        //so we can't actually cache these results... and we should be passing in the context...
        //I don't care to fix this right now since it's pretty rare that a TypeConverter's implementation
        //needs to access the ITypeDescriptorContext to determine if it can/cannot convert to/from.
        private static bool IsConvertibleFrom(Type From, Type To)
        {
            if (From == null || To == null)
                return false;

            TypeConverter Converter = GetConverter(To);

            if (!CachedCanConvertFrom.TryGetValue(Converter, out Dictionary<Type, bool> CanConvertByType))
            {
                CanConvertByType = new();
                CachedCanConvertFrom.Add(Converter, CanConvertByType);
            }

            if (!CanConvertByType.TryGetValue(To, out bool CanConvert))
            {
                CanConvert = Converter.CanConvertFrom(From);
                CanConvertByType.Add(To, CanConvert);
            }

            return CanConvert;
        }

        private static bool IsConvertibleTo(Type From, Type To)
        {
            if (From == null || To == null)
                return false;

            TypeConverter Converter = GetConverter(From);

            if (!CachedCanConvertTo.TryGetValue(Converter, out Dictionary<Type, bool> CanConvertByType))
            {
                CanConvertByType = new();
                CachedCanConvertTo.Add(Converter, CanConvertByType);
            }

            if (!CanConvertByType.TryGetValue(To, out bool CanConvert))
            {
                CanConvert = Converter.CanConvertTo(To);
                CanConvertByType.Add(To, CanConvert);
            }

            return CanConvert;
        }

        private static bool IsAssignableOrConvertible(Type From, Type To) => IsAssignable(From, To) || IsConvertible(From, To);
        #endregion Set Property Value

        /// <summary>True if this binding subscribed to the <see cref="SourceObject"/>'s PropertyChanged event the last time <see cref="SourceObject"/> was set.<para/>
        /// If true, the event must be unsubscribed from when changing <see cref="SourceObject"/> or when disposing this <see cref="DataBinding"/></summary>
        private bool IsSubscribedToSourceObjectPropertyChanged = false;

        private void ObservableSourceObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == SourcePropertyName)
                SourcePropertyValueChanged();
#if UseWPF
            else
                throw new InvalidOperationException($"{nameof(DataBinding)}.{nameof(ObservableSourceObject_PropertyChanged)}: Expected PropertyName={SourcePropertyName}. Actual PropertyName={e.PropertyName}");
#endif
        }

        /// <summary>True if this binding subscribed to the <see cref="TargetObject"/>'s PropertyChanged event during initialization.<para/>
        /// If true, the event must be unsubscribed from when disposing this <see cref="DataBinding"/></summary>
        private readonly bool IsSubscribedToTargetObjectPropertyChanged;

        private void ObservableTargetObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == TargetPropertyName)
                TargetPropertyValueChanged();
#if UseWPF
            else
                throw new InvalidOperationException($"{nameof(DataBinding)}.{nameof(ObservableTargetObject_PropertyChanged)}: Expected PropertyName={TargetPropertyName}. Actual PropertyName={e.PropertyName}");
#endif
        }

        private void SourcePropertyValueChanged()
        {
            //  Propagate the new value to the TargetProperty
            if (!IsSettingValue && Config.BindingMode is DataBindingMode.OneWay or DataBindingMode.TwoWay)
            {
                TrySetPropertyValue(SourceObject, SourceProperty, SourcePropertyType, TargetObject, TargetProperty, TargetPropertyType, ConvertSettings);
            }
        }

        private void TargetPropertyValueChanged()
        {
            //  Propagate the new value to the SourceProperty
            if (!IsSettingValue && Config.BindingMode is DataBindingMode.OneWayToSource or DataBindingMode.TwoWay)
            {
                TrySetPropertyValue(TargetObject, TargetProperty, TargetPropertyType, SourceObject, SourceProperty, SourcePropertyType, ConvertBackSettings);
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
#if UseWPF
                    PropertyChangedEventManager.RemoveHandler(ObservableTargetObject, ObservableTargetObject_PropertyChanged, TargetPropertyName);
#else
                    ObservableTargetObject.PropertyChanged -= ObservableTargetObject_PropertyChanged;
#endif
                }
                if (IsSubscribedToSourceObjectPropertyChanged && SourceObject is INotifyPropertyChanged ObservableSourceObject)
                {
#if UseWPF
                    PropertyChangedEventManager.RemoveHandler(ObservableSourceObject, ObservableSourceObject_PropertyChanged, SourcePropertyName);
#else
                    ObservableSourceObject.PropertyChanged -= ObservableSourceObject_PropertyChanged;
#endif
                    IsSubscribedToSourceObjectPropertyChanged = false;
                }
                ClearPathChangeListeners();
            }
        }

        public object Instance => TargetObject;

        public PropertyDescriptor PropertyDescriptor => null;

        public IContainer Container => null;
        public object GetService(Type serviceType) => null;
        public void OnComponentChanged() { }
        public bool OnComponentChanging() => true;
    }
}
